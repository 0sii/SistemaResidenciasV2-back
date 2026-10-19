# Bitácora de Instalación — Sistema de Vinculación de Residencias Profesionales

Documento de referencia para instalar el sistema (backend .NET 8, frontend Angular y base de datos MySQL) en un servidor nuevo, basado en la configuración real verificada en el servidor `residenciaosiris` (10.168.0.113).

> ⚠️ Las secciones marcadas con 🔍 son supuestos razonables basados en patrones comunes, pero **no se verificaron directamente en esta sesión** — confírmalas en tu servidor antes de darlas por ciertas.

---

## 0. Qué es cada pieza

| Componente | Tecnología | Dónde vive (en el servidor de referencia) |
|---|---|---|
| Backend | ASP.NET Core 8 (.NET 8) | `~/SistemaResidenciasV2-back` (código) → se publica a `/home/cds/publish-api` |
| Frontend | Angular | `~/SistemaResidenciasV2-front` → build estático en `/var/www/sistema-residencias` |
| Base de datos | MySQL | Base de datos `vinculacion` |
| Proceso backend | Servicio systemd `api-residencias.service` | Escucha en `127.0.0.1:5000` (interno, no público) |
| Acceso público | Nginx (proxy reverso) | Puerto **80** (HTTP estándar) → sirve el Angular estático y reenvía `/api/` a `127.0.0.1:5000` |

---

## 1. Requisitos previos (qué instalar en un servidor nuevo)

Sistema operativo de referencia: **Ubuntu Server 24.04**.

```bash
sudo apt update && sudo apt upgrade -y

# Git
sudo apt install -y git

# .NET 8 SDK (el proyecto apunta a net8.0)
# ⚠️ El servidor de referencia tiene VARIAS versiones de .NET instaladas
# (snap dotnet 10.0.10, snap dotnet-sdk 8.0.407, deb dotnet-host-8.0 8.0.28).
# El proyecto compila contra net8.0 — instala la 8, no la 10:
sudo apt install -y dotnet-sdk-8.0
dotnet --version        # debe mostrar 8.0.x — si tienes más de un SDK, usa:
dotnet --list-sdks      # para confirmar cuáles hay instalados

# MySQL Server
sudo apt install -y mysql-server
sudo systemctl enable --now mysql

# Node.js — versión EXACTA verificada funcionando: v22.23.2
# (instalar por nvm en vez de "LTS genérico" evita que el Node del
# servidor cambie de versión por su cuenta con el tiempo)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.1/install.sh | bash
source ~/.bashrc
nvm install 22.23.2
nvm alias default 22.23.2
node -v   # debe mostrar v22.23.2
npm -v    # debe mostrar 10.9.8

# Angular CLI — versión EXACTA verificada funcionando: 20.1.5
sudo npm install -g @angular/cli@20.1.5
ng version   # confirma "Angular CLI: 20.1.5"

# Nginx (proxy reverso + servir el frontend)
sudo apt install -y nginx
```

> 💡 El servidor de referencia tiene **varias versiones de .NET instaladas** (snap `dotnet 10.0.10`, snap `dotnet-sdk 8.0.407`, deb `8.0.28`). Si usas `dotnet build`/`dotnet publish` y hay ambigüedad, confirma con `dotnet --list-sdks` que usas la 8.x.

---

## 2. Base de datos (MySQL)

**Importante: no hace falta crear tablas a mano.** El backend corre `db.Database.Migrate()` automáticamente al arrancar (ver `Program.cs`), que crea todas las tablas y los datos semilla (catálogos de Roles, Carreras, y el usuario Administrador inicial) desde las migraciones de Entity Framework. Solo necesitas crear la base de datos **vacía** y un usuario con permisos.

```bash
sudo mysql
```
Dentro de la consola de MySQL:
```sql
CREATE DATABASE vinculacion CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'usuario_app'@'localhost' IDENTIFIED BY 'UNA_CONTRASEÑA_SEGURA';
GRANT ALL PRIVILEGES ON vinculacion.* TO 'usuario_app'@'localhost';
FLUSH PRIVILEGES;
EXIT;
```

El resto (tablas, catálogos, usuario admin) se crea solo la primera vez que arranca el backend (sección 3.5).

### Usuario administrador sembrado por defecto
Las migraciones incluyen un usuario Administrador (`Rol Id = 1`) con estos datos de fábrica — **cámbialos en el código antes de instalar en un servidor nuevo**, o actualízalos por SQL después del primer arranque:
- Correo: definido en `Migrations/20260306021355_Initial.cs` / `.Designer.cs` (bloque `InsertData` de la tabla `Usuarios`)
- Contraseña: queda guardada como hash SHA-256 (sin salt) — debes conocer la contraseña en texto plano por fuera de este documento.

---

## 3. Backend (ASP.NET Core)

### 3.1 Clonar el repositorio
```bash
cd ~
git clone https://github.com/0sii/SistemaResidenciasV2-back.git
cd SistemaResidenciasV2-back
```

### 3.2 Configurar `appsettings.Production.json`
Crea (o edita) este archivo en la raíz del proyecto — **no lo subas a git con datos reales**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=vinculacion;user=usuario_app;password=UNA_CONTRASEÑA_SEGURA;"
  },
  "Jwt": {
    "key": "UNA_CLAVE_LARGA_Y_ALEATORIA_DE_AL_MENOS_32_CARACTERES"
  },
  "Sepomex": {
    "ImportOnStartup": false,
    "DataFilePath": ""
  }
}
```
> ⚠️ **Revisa el CORS en `Program.cs`** — actualmente está fijo a `http://localhost:4200`:
> ```csharp
> policy.WithOrigins("http://localhost:4200")
> ```
> En este servidor, como Nginx sirve el frontend y reenvía `/api/` bajo el **mismo origen** (puerto 80), el navegador nunca hace una petición *cross-origin* real hacia el backend — así que de hecho CORS casi no entra en juego en producción. Aun así, conviene no dejar `localhost:4200` como único origen permitido (sirve solo para cuando desarrollas en tu máquina local con `ng serve`); considera agregar también el dominio/IP de producción por si en algún momento el frontend llega a llamar directo al puerto 5000/5154 sin pasar por Nginx.

### 3.3 Restaurar y compilar
```bash
dotnet restore
dotnet build
```
Si ves errores de sintaxis en algún archivo de `Migrations/*.cs`, revisa que no haya quedado una cadena de texto mal cerrada (nos pasó una vez por una edición manual de un seed).

### 3.4 Publicar
```bash
dotnet publish -c Release -o /home/cds/publish-api
```
(Ajusta la ruta de salida al usuario/carpeta real de tu servidor.)

### 3.5 Crear el servicio systemd
Archivo: `/etc/systemd/system/api-residencias.service`
```ini
[Unit]
Description=API Residencias Profesionales - ASP.NET Core
After=network.target mysql.service

[Service]
WorkingDirectory=/home/cds/publish-api
ExecStart=/usr/bin/dotnet /home/cds/publish-api/WebApiVinculacionProyectosV2.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=api-residencias
User=cds
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```
Cambia `cds` y las rutas `/home/cds/...` por tu usuario real.

```bash
sudo systemctl daemon-reload
sudo systemctl enable api-residencias.service
sudo systemctl start api-residencias.service
sudo systemctl status api-residencias.service
```

En el primer arranque, revisa el log para confirmar que las migraciones corrieron y creó las tablas:
```bash
sudo journalctl -u api-residencias.service -n 50 --no-pager
```
Debe verse algo como `Applying migration 'Initial'...` y después `Application started`.

---

## 4. Frontend (Angular) 🔍

### 4.1 Clonar el repositorio
```bash
cd ~
git clone https://github.com/0sii/SistemaResidenciasV2-front.git
cd SistemaResidenciasV2-front
```

### 4.2 Configurar la URL del backend
Edita el archivo de entorno de producción (normalmente `src/environments/environment.prod.ts` o similar). Como Nginx sirve el frontend y el backend bajo el **mismo origen** (puerto 80, con `/api/` reenviado internamente), lo más simple y portable es usar una ruta **relativa**, para no tener que hardcodear la IP del servidor:
```ts
export const environment = {
  production: true,
  apiUrl: '/api'
};
```
Así el mismo build funciona sin cambios sin importar si accedes por IP, por dominio, o detrás de otro proxy en el futuro.

### 4.3 Instalar dependencias y compilar
Usa `npm ci` en vez de `npm install`: instala **exactamente** las versiones fijadas en `package-lock.json` (las mismas que usaste cuando sí te compiló bien), en vez de dejar que npm resuelva versiones nuevas que puedan traer sorpresas.
```bash
npm ci
ng build --configuration production
```
> Si `npm ci` falla porque no existe `package-lock.json` en el repo, usa `npm install` una vez, confirma que compila, y luego **sube el `package-lock.json` generado a git** para que la próxima instalación ya sea reproducible con `npm ci`.
Esto genera una carpeta `dist/` con los archivos estáticos listos para servir.

### 4.4 Servir el build con Nginx
Copia el contenido de `dist/` a donde Nginx sirva archivos estáticos:
```bash
sudo mkdir -p /var/www/sistema-residencias
sudo cp -r dist/*/* /var/www/sistema-residencias/
```

Configuración real verificada en el servidor, `/etc/nginx/sites-available/sistema-residencias`:
```nginx
server {
    listen 80;
    listen [::]:80;

    server_name _;

    root /var/www/sistema-residencias;
    index index.html;

    # Angular SPA
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API ASP.NET Core
    location /api/ {
        proxy_pass http://127.0.0.1:5000/api/;

        proxy_http_version 1.1;

        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```
```bash
sudo ln -s /etc/nginx/sites-available/sistema-residencias /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

> El sistema se accede entonces simplemente por `http://IP_DEL_SERVIDOR` (puerto 80, sin especificarlo en la URL) — **no** por el puerto 5154 ni el 4200 (esos son solo los puertos de desarrollo local en la máquina del programador, `ng serve` en 4200 y el backend en modo Development en 5154 — no aplican a este servidor de producción).

---

## 5. Verificación final (checklist de instalación)

- [ ] `sudo systemctl status api-residencias.service` → `active (running)`
- [ ] `sudo journalctl -u api-residencias.service -n 50` sin errores de conexión a BD ni de migraciones
- [ ] `curl http://127.0.0.1:5000/swagger/v1/swagger.json` responde JSON
- [ ] Nginx activo: `sudo systemctl status nginx`
- [ ] Frontend carga en el navegador (`http://IP_DEL_SERVIDOR`, puerto 80 por defecto)
- [ ] Login funciona con el usuario administrador sembrado
- [ ] Revisar que `appsettings.Production.json` **no** esté commiteado a git con credenciales reales

---

## 6. Para actualizar el sistema después de la instalación (despliegue de cambios)

### Backend
```bash
cd ~/SistemaResidenciasV2-back
git pull origin main
dotnet publish -c Release -o /home/cds/publish-api
sudo systemctl restart api-residencias.service
```

### Frontend
```bash
cd ~/SistemaResidenciasV2-front
git pull origin main
npm install
ng build --configuration production
sudo cp -r dist/*/* /var/www/sistema-residencias/
```

### Si el `git pull` choca por `bin/`/`obj/` versionados
```bash
git checkout -- .
git pull origin main
```
**Arreglo permanente** (hacerlo una sola vez):
```bash
printf "bin/\nobj/\n" >> .gitignore
git rm -r --cached bin obj
git add .gitignore
git commit -m "Dejar de versionar bin/ y obj/"
git push origin main
```

---

## 7. Pendientes de seguridad/limpieza detectados (recomendado atender)

1. **`bin/` y `obj/` siguen versionados en git** — ver sección 6 para el arreglo.
2. **Un PDF de expediente de un estudiante quedó en el historial de git** (`uploads/Expediente/...pdf`). Si el repositorio no es privado, considerar limpiar el historial (`git filter-repo` o equivalente) y agregar `uploads/` al `.gitignore`.
3. **`POST /api/Usuarios` no requiere autenticación** — cualquiera que conozca la URL puede crear usuarios. Proteger con `[Authorize]` una vez que el primer administrador ya esté creado.
4. **CORS fijo a `http://localhost:4200`** en `Program.cs` — debe actualizarse al dominio/IP real de producción.
5. **Contraseña del usuario `cds` compartida en texto plano en algún momento** — rotarla si aún no se ha hecho.

---

## 8. Instalación local de desarrollo (PC con Windows)

Sirve para correr el sistema en una PC (por ejemplo la de jefa de vinculacion) sin Nginx ni systemd: el backend corre con `dotnet run` y el frontend con `ng serve`.

| Pieza | Puerto local |
|---|---|
| Frontend (`ng serve`) | `http://localhost:4200` |
| Backend (`dotnet run`, modo Development) | `http://localhost:5154` |
| MySQL | `localhost:3306` |

> El CORS de `Program.cs` ya permite `http://localhost:4200`, por eso en local funciona sin cambios.

### 8.1 Programas a instalar
Abre PowerShell. Si la PC no te da permisos de administrador, pide al encargado que instale al menos Git, .NET SDK y MySQL.

```powershell
winget install --id Git.Git -e
winget install --id Microsoft.DotNet.SDK.8 -e
winget install --id Oracle.MySQL -e        # o el instalador oficial MySQL Installer
winget install --id CoreyButler.NVMforWindows -e
```
Cierra y vuelve a abrir PowerShell, y luego:
```powershell
nvm install 22.23.2
nvm use 22.23.2
npm install -g @angular/cli@20.1.5
```
Verifica: `dotnet --list-sdks` (debe haber 8.0.x), `node -v` (v22.23.2), `ng version` (20.1.5).

### 8.2 Base de datos
Con MySQL instalado, abre `MySQL Command Line Client` (o `mysql -u root -p`):
```sql
CREATE DATABASE vinculacion CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```
Las tablas y el usuario administrador se crean solos al arrancar el backend (migraciones).

### 8.3 Backend
```powershell
git clone https://github.com/0sii/SistemaResidenciasV2-back.git
cd SistemaResidenciasV2-back
```
Edita (o crea) `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=vinculacion;user=root;password=TU_PASSWORD_MYSQL;"
  },
  "Jwt": { "key": "UNA_CLAVE_LARGA_DE_AL_MENOS_32_CARACTERES" }
}
```
Arranca:
```powershell
dotnet restore
dotnet run
```
Debe decir `Now listening on: http://localhost:5154` (si muestra otro puerto, revisa `Properties/launchSettings.json`). Swagger queda en `http://localhost:5154/swagger`.

### 8.4 Frontend
```powershell
git clone https://github.com/0sii/SistemaResidenciasV2-front.git
cd SistemaResidenciasV2-front
npm ci
ng serve
```
Revisa que `src/environments/environment.ts` apunte a `http://localhost:5154/api`. Abre `http://localhost:4200` e inicia sesión con el usuario administrador sembrado.

### 8.5 Problemas comunes
- **`ng` no se reconoce:** cierra y abre la terminal, o revisa que `nvm use 22.23.2` esté activo.
- **Error de conexión a MySQL:** confirma que el servicio MySQL esté corriendo (`services.msc`) y que usuario/contraseña del connection string sean correctos.
- **Puerto ocupado (5154 o 4200):** cierra el proceso que lo usa o cambia el puerto (`ng serve --port 4300`; en ese caso agrega ese origen al CORS de `Program.cs`).
- **No tienes permisos de administrador:** usa el ZIP portable de MySQL/MariaDB, o lleva el sistema en una laptop/VM propia.
