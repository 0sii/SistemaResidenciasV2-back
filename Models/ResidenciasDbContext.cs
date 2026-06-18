using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace WebApiVinculacionProyectosV2.Models
{
    public partial class ResidenciasDbContext : DbContext
    {
        public ResidenciasDbContext() { }

        public ResidenciasDbContext(DbContextOptions<ResidenciasDbContext> options)
            : base(options) { }

        // DbSets (entidades en singular, tablas en plural)
        public virtual DbSet<Usuarios> Usuarios { get; set; } = null!;
        public virtual DbSet<Empresas> Empresas { get; set; } = null!;
        public virtual DbSet<Estudiantes> Estudiantes { get; set; } = null!;
        public virtual DbSet<Docentes> Docentes { get; set; } = null!;
        public virtual DbSet<Proyectos> Proyectos { get; set; } = null!;
        public virtual DbSet<Modalidad> Modalidad { get; set; } = null!;
        public virtual DbSet<Estado> Estado { get; set; } = null!;
        public virtual DbSet<Contactoemergencia> Contactoemergencia { get; set; } = null!;
        public virtual DbSet<Contacto> Contacto { get; set; } = null!;
        public virtual DbSet<DependenciaMedica> DependenciasMedica { get; set; } = null!;
        public virtual DbSet<Permisos> Permisos { get; set; } = null!;
        public virtual DbSet<ProyectoDocente> ProyectoDocente { get; set; } = null!;
        public virtual DbSet<Rol> Rol { get; set; } = null!;
        public virtual DbSet<RolPermiso> RolPermiso { get; set; } = null!;
        public virtual DbSet<UsuarioRol> UsuarioRol { get; set; } = null!;
        public virtual DbSet<Carreras> Carreras { get; set; } = null!;
        public virtual DbSet<Especializacion> Especializacion { get; set; } = null!;

        // ✅ Documentos y TipoDocumento
        public virtual DbSet<Documento> Documentos { get; set; } = null!;
        public virtual DbSet<TipoDocumento> TipoDocumentos { get; set; } = null!;
        public virtual DbSet<InvitacionProyecto> InvitacionProyectos { get; set; } = null!;

        public virtual DbSet<TipoEntregable> TipoEntregables { get; set; } = null!;
        public virtual DbSet<Entregable> Entregables { get; set; } = null!;
        public virtual DbSet<EntregableVersion> EntregableVersiones { get; set; } = null!;
        public virtual DbSet<RevisionEntregable> RevisionEntregables { get; set; } = null!;
        public virtual DbSet<TipoRelacionDocenteProyecto> TipoRelacionDocenteProyecto { get; set; } = null!;
        public DbSet<PeriodoAcademico> PeriodosAcademicos { get; set; }
        public virtual DbSet<EstadoEntregable> EstadoEntregables { get; set; } = null!;
        public DbSet<PeriodoMembrentado> PeriodosMembrentados { get; set; } = null!;
        public DbSet<ProyectoDocumento> ProyectoDocumentos { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // aquí normalmente va el connection string si no lo pones en Program.cs
                // optionsBuilder.UseMySql("...", ServerVersion.AutoDetect("..."));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureUsuario(modelBuilder);
            ConfigureEmpresa(modelBuilder);
            ConfigureCarreras(modelBuilder);
            ConfigureDependenciaMedica(modelBuilder);
            ConfigureContactoEmergencia(modelBuilder);
            ConfigureContacto(modelBuilder);
            ConfigureEspecializacion(modelBuilder);
            ConfigureEstado(modelBuilder);
            ConfigureModalidad(modelBuilder);
            ConfigureProyectos(modelBuilder);
            ConfigureEstudiante(modelBuilder);
            ConfigureDocente(modelBuilder);
            ConfigurePermisos(modelBuilder);
            ConfigureRol(modelBuilder);
            ConfigureUsuarioRol(modelBuilder);
            ConfigureRolPermiso(modelBuilder);
            ConfigureProyectoDocente(modelBuilder);

            // ✅ NUEVO: TipoDocumento y Documento con relaciones
            ConfigureTipoDocumento(modelBuilder);
            ConfigureDocumento(modelBuilder);
            ConfigureTipoEntregable(modelBuilder);
            ConfigureEntregable(modelBuilder);
            ConfigureEntregableVersion(modelBuilder);
            ConfigureRevisionEntregable(modelBuilder);
            ConfigureTipoRelacionDocenteProyecto(modelBuilder);
            ConfigureInvitacionProyecto(modelBuilder);
            ConfigureEstadoEntregable(modelBuilder);



            OnModelCreatingPartial(modelBuilder);
            modelBuilder.Entity<Proyectos>()
                .HasOne<PeriodoAcademico>()
                .WithMany()
                .HasForeignKey(p => p.IdPeriodoAcademico)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Proyectos>()
        .Property(p => p.FechaRegistro)
        .HasColumnType("datetime(6)")
        .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
        .ValueGeneratedOnAdd()
        .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore); // 🔒 no se actualiza jamás


            modelBuilder.Entity<Proyectos>()
    .Property(p => p.FechaRegistro)
    .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);


            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PeriodoMembrentado>()
        .HasIndex(x => x.PeriodoAcademicoId)
        .IsUnique();

            // FK sin navigation properties
            modelBuilder.Entity<PeriodoMembrentado>()
                .HasOne<PeriodoAcademico>()
                .WithMany()
                .HasForeignKey(x => x.PeriodoAcademicoId)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<ProyectoDocumento>(e =>
            {
                e.ToTable("ProyectoDocumentos");
                e.HasKey(x => x.Id);

                e.Property(x => x.NombreOriginal).HasMaxLength(255).IsRequired();
                e.Property(x => x.NombreServidor).HasMaxLength(255).IsRequired();
                e.Property(x => x.RutaFisica).HasMaxLength(500).IsRequired();
                e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();

                // ✅ 1 documento por proyecto (si esa es tu regla)
                e.HasIndex(x => x.IdProyecto).IsUnique();

                e.Property(x => x.TamanoBytes).IsRequired();
                e.Property(x => x.FechaSubida).IsRequired();

                // FK (ajusta el nombre real de tu tabla Proyectos)
                e.HasOne<Proyectos>()              // o la entidad real
                 .WithMany()                      // o .WithOne() si tienes navegación
                 .HasForeignKey(x => x.IdProyecto)
                 .OnDelete(DeleteBehavior.Cascade);
            });

        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        // ========================= USUARIO =========================

        private static void ConfigureEstadoEntregable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EstadoEntregable>(entity =>
            {
                entity.ToTable("EstadoEntregable");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Clave)
                      .IsRequired()
                      .HasMaxLength(30);

                entity.Property(x => x.Descripcion)
                      .IsRequired()
                      .HasMaxLength(120);

                entity.Property(x => x.Activo)
                      .HasDefaultValue(true);

                entity.HasIndex(x => x.Clave).IsUnique();

                entity.HasData(
    new EstadoEntregable { Id = 1, Activo = true, Clave = "PENDIENTE", Descripcion = "Pendiente" },
    new EstadoEntregable { Id = 2, Activo = true, Clave = "EN_REVISION", Descripcion = "En revisión" },
    new EstadoEntregable { Id = 3, Activo = true, Clave = "CAMBIOS", Descripcion = "Con cambios" },
    new EstadoEntregable { Id = 4, Activo = true, Clave = "APROBADO", Descripcion = "Aprobado" },
    new EstadoEntregable { Id = 5, Activo = true, Clave = "RECHAZADO", Descripcion = "Rechazado" },
    new EstadoEntregable { Id = 6, Activo = true, Clave = "CANCELADO", Descripcion = "Cancelado" }
);


                entity.HasCheckConstraint(
                    "CK_EstadoEntregable_Clave",
                    "Clave IN ('PENDIENTE','EN_REVISION','CAMBIOS','APROBADO','RECHAZADO','CANCELADO')"
                );
            });
        }

        private static void ConfigureUsuario(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuarios>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Correo)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                      .IsRequired();

                entity.Property(e => e.Nombre)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.ApellidoPaterno)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.ApellidoMaterno)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Activo)
                      .HasDefaultValue(true);

                entity.HasCheckConstraint(
                    "CK_Usuarios_Correo_Formato",
                    "Correo REGEXP '^[^[:space:]@]+@[^[:space:]@]+\\.[^[:space:]@]+$'"
                );

                const int ADMIN_USER_ID = 1;
                const string ADMIN_EMAIL = "19161231@itoaxaca.edu.mx";
                const string ADMIN_PASSWORD = "123456";

                var adminHash = SecurityUtils.Sha256(ADMIN_PASSWORD);

                entity.HasData(new Usuarios
                {
                    Id = ADMIN_USER_ID,
                    Correo = ADMIN_EMAIL,
                    PasswordHash = adminHash,
                    Nombre = "Luis Enrique",
                    ApellidoPaterno = "Alvarez",
                    ApellidoMaterno = "Trujillo",
                    Activo = true
                });
            });
        }

        // ========================= EMPRESA =========================
        private static void ConfigureEmpresa(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Empresas>(entity =>
            {
                entity.ToTable("Empresas");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nombre)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.RFC)
                      .IsRequired()
                      .HasMaxLength(13);

                entity.Property(e => e.Telefono)
                      .IsRequired()
                      .HasMaxLength(10);

                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.CP)
                      .HasMaxLength(5);

                // RFC: 3–4 letras + 6 dígitos + 3 homoclave
                entity.HasCheckConstraint(
                    "CK_Empresas_RFC_Formato",
                    "RFC REGEXP '^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$'"
                );

                // Teléfono: 10 dígitos
                entity.HasCheckConstraint(
                    "CK_Empresas_Telefono10",
                    "Telefono REGEXP '^[0-9]{10}$'"
                );

                // CP opcional pero si viene 5 dígitos
                entity.HasCheckConstraint(
                    "CK_Empresas_CP5",
                    "CP IS NULL OR CP REGEXP '^[0-9]{5}$'"
                );
            });
        }

        // ========================= CARRERAS =========================
        private static void ConfigureCarreras(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Carreras>(entity =>
            {
                entity.ToTable("Carreras");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Descripcion)
                      .IsRequired()
                      .HasMaxLength(200);

                // Ejemplo de datos iniciales
                entity.HasData(
                    new Carreras { Id = 1, Descripcion = "Contador Público", Activo = true },
                    new Carreras { Id = 2, Descripcion = "Licenciatura en Administración", Activo = true },
                    new Carreras { Id = 3, Descripcion = "Ingeniería Química", Activo = true },
                    new Carreras { Id = 4, Descripcion = "Ingeniería Mecánica", Activo = true },
                    new Carreras { Id = 5, Descripcion = "Ingeniería Industrial", Activo = true },
                    new Carreras { Id = 6, Descripcion = "Ingeniería en Sistemas Computacionales", Activo = true },
                    new Carreras { Id = 7, Descripcion = "Ingeniería en Gestión Empresarial", Activo = true },
                    new Carreras { Id = 8, Descripcion = "Ingeniería Electrónica", Activo = true },
                    new Carreras { Id = 9, Descripcion = "Ingeniería Eléctrica", Activo = true },
                    new Carreras { Id = 10, Descripcion = "Ingeniería Civil", Activo = true }
                );
            });
        }

        // ==================== DEPENDENCIA MÉDICA ====================
        private static void ConfigureDependenciaMedica(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DependenciaMedica>(entity =>
            {
                entity.ToTable("DependenciaMedica");
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Descripcion)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.HasData(
                    new DependenciaMedica { Id = 1, Descripcion = "IMSS", Activo = true },
                    new DependenciaMedica { Id = 2, Descripcion = "ISSSTE", Activo = true },
                    new DependenciaMedica { Id = 3, Descripcion = "ISSFAM (Militar)", Activo = true },
                    new DependenciaMedica { Id = 4, Descripcion = "PEMEX", Activo = true },
                    new DependenciaMedica { Id = 5, Descripcion = "Seguro privado", Activo = true },
                    new DependenciaMedica { Id = 6, Descripcion = "Servicios de Salud del Estado", Activo = true },
                    new DependenciaMedica { Id = 7, Descripcion = "Sin seguridad social", Activo = true }
                );
            });
        }

        // ================= CONTACTO EMERGENCIA =================
        private static void ConfigureContactoEmergencia(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contactoemergencia>(entity =>
            {
                entity.ToTable("Contactoemergencia");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Telefono)
                      .HasMaxLength(10);

                entity.Property(c => c.email)
                      .HasMaxLength(255);

                // Teléfono 10 dígitos (opcional)
                entity.HasCheckConstraint(
                    "CK_ContactoEmerg_TelFormato",
                    "Telefono IS NULL OR Telefono REGEXP '^[0-9]{10}$'"
                );

                // Email (opcional)
                entity.HasCheckConstraint(
                    "CK_ContactoEmerg_EmailFormato",
                    "email IS NULL OR email REGEXP '^[^[:space:]@]+@[^[:space:]@]+\\.[^[:space:]@]+$'"
                );
            });
        }

        // ========================= CONTACTO =========================
        private static void ConfigureContacto(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contacto>(entity =>
            {
                entity.ToTable("Contacto");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.correo)
                      .HasMaxLength(255);

                entity.Property(c => c.Telefono)
                      .HasMaxLength(10);

                // FK → Empresa
                entity.HasOne<Empresas>()
                      .WithMany()
                      .HasForeignKey(c => c.IdEmpresa)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // ====================== ESPECIALIZACION ======================
        private static void ConfigureEspecializacion(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Especializacion>(entity =>
            {
                entity.ToTable("Especializacion");
                entity.HasKey(e => e.id);

                entity.Property(e => e.descripcion)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.HasData(
                    new Especializacion { id = 1, descripcion = "Desarrollo de software", Activo = true },
                    new Especializacion { id = 2, descripcion = "Redes y telecomunicaciones", Activo = true },
                    new Especializacion { id = 3, descripcion = "Bases de datos", Activo = true },
                    new Especializacion { id = 4, descripcion = "Gestión de proyectos", Activo = true }
                );
            });
        }

        // =========================== ESTADO ==========================
        private static void ConfigureEstado(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Estado>(entity =>
            {
                entity.ToTable("Estado");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Descripcion)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasData(
                    new Estado { Id = 1, Descripcion = "Nuevo", Activo = true },
                    new Estado { Id = 2, Descripcion = "Disponible", Activo = true },
                    new Estado { Id = 3, Descripcion = "En Espera de Asignación de Revisor de Anteproyecto", Activo = true },
                    new Estado { Id = 4, Descripcion = "En Espera de Revisión de Anteproyecto", Activo = true },
                    new Estado { Id = 5, Descripcion = "Anteproyecto Revisado", Activo = true},
                    new Estado { Id = 6, Descripcion = "En Espera de Asignación de Asesor Interno", Activo = true },
                    new Estado { Id = 7, Descripcion = "En Curso", Activo = true },
                    new Estado { Id = 8, Descripcion = "Finalizado", Activo = true },
                    new Estado { Id = 9, Descripcion = "Cancelado", Activo = true }
                    
                );
            });
        }

        // ========================== MODALIDAD =========================
        private static void ConfigureModalidad(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Modalidad>(entity =>
            {
                entity.ToTable("Modalidad");
                entity.HasKey(m => m.id);

                entity.Property(m => m.Descripcion)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasData(
                    new Modalidad { id = 1, Descripcion = "Presencial", Activo = true },
                    new Modalidad { id = 2, Descripcion = "Mixta", Activo = true },
                    new Modalidad { id = 3, Descripcion = "Virtual", Activo = true }
                );
            });
        }

        // ========================== PROYECTOS =========================
        private static void ConfigureProyectos(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Proyectos>(entity =>
            {
                entity.ToTable("Proyectos");

                entity.HasKey(p => p.Id);

                // Empresa (obligatoria)
                entity.HasOne<Empresas>()
                      .WithMany(e => e.Proyectos)
                      .HasForeignKey(p => p.IdEmpresa)
                      .OnDelete(DeleteBehavior.Restrict);

                // Especialización (obligatoria)
                entity.HasOne<Especializacion>()
                      .WithMany(e => e.Proyectos)
                      .HasForeignKey(p => p.idEspecializcion)
                      .OnDelete(DeleteBehavior.Restrict);

                // Modalidad (opcional)
                entity.HasOne<Modalidad>()
                      .WithMany(m => m.Proyectos)
                      .HasForeignKey(p => p.idModalidad)
                      .OnDelete(DeleteBehavior.SetNull);

                // Estado (opcional)
                entity.HasOne<Estado>()
                      .WithMany(e => e.Proyectos)
                      .HasForeignKey(p => p.idEstado)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(p => p.PropuestaAlumno)
                      .HasDefaultValue(false);

                // ✅ NUEVO: FK a Estudiantes (líder)
                entity.HasOne<Estudiantes>()
                      .WithMany()
                      .HasForeignKey(p => p.IdEstudianteCreador)
                      .OnDelete(DeleteBehavior.SetNull);

                

                // ✅ IMPORTANTÍSIMO: que EF ignore cambios después de crear
                entity.Property(p => p.FechaRegistro)
                      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

                // (Opcional) index para búsquedas rápidas
                entity.HasIndex(p => p.IdEstudianteCreador);
            });
        }

        // ========================= ESTUDIANTE =========================
        private static void ConfigureEstudiante(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Estudiantes>(entity =>
            {
                entity.ToTable("Estudiantes");

                entity.HasKey(e => e.id);

                entity.Property(e => e.Nombre)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.ApellidoPaterno)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.ApellidoMaterno)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.noControl)
                      .HasMaxLength(9);

                entity.Property(e => e.telefonoCelular)
                      .HasMaxLength(10);

                entity.Property(e => e.correoPersonal)
                      .HasMaxLength(255);

                const string soloLetrasRegex =
                    "^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+( [A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+)*$";

                entity.HasCheckConstraint(
                    "CK_Estudiantes_NombreSoloLetras",
                    $"Nombre REGEXP '{soloLetrasRegex}'"
                );

                entity.HasCheckConstraint(
                    "CK_Estudiantes_ApellidoPatSoloLetras",
                    $"ApellidoPaterno REGEXP '{soloLetrasRegex}'"
                );

                entity.HasCheckConstraint(
                    "CK_Estudiantes_ApellidoMatSoloLetras",
                    $"ApellidoMaterno REGEXP '{soloLetrasRegex}'"
                );

                entity.HasCheckConstraint(
                    "CK_Estudiantes_NoControlFormato",
                    "noControl IS NULL OR noControl REGEXP '^([0-9]{8}|[A-Za-z][0-9]{8})$'"
                );

                entity.HasCheckConstraint(
                    "CK_Estudiantes_TelCelFormato",
                    "telefonoCelular IS NULL OR telefonoCelular REGEXP '^[0-9]{10}$'"
                );

                entity.HasCheckConstraint(
                    "CK_Estudiantes_CorreoPersonalFormato",
                    "correoPersonal IS NULL OR correoPersonal REGEXP '^[^[:space:]@]+@[^[:space:]@]+\\.[^[:space:]@]+$'"
                );

                // Relaciones
                entity.HasOne<Usuarios>()
                      .WithMany()
                      .HasForeignKey(e => e.idUsuario)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Proyectos>()
                      .WithMany()
                      .HasForeignKey(e => e.idProyecto)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Carreras>()
                      .WithMany()
                      .HasForeignKey(e => e.idcarrera)
                      .OnDelete(DeleteBehavior.SetNull);


                entity.HasOne<DependenciaMedica>()
                      .WithMany()
                      .HasForeignKey(e => e.idDependenciaMedica)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Contactoemergencia>()
                      .WithMany()
                      .HasForeignKey(e => e.idContactoEmergencia)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }

        // =========================== DOCENTE ==========================
        private static void ConfigureDocente(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Docentes>(entity =>
            {
                entity.ToTable("Docentes");

                entity.HasKey(d => d.Id);

                entity.Property(d => d.Nombre)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(d => d.ApellidoPaterno)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(d => d.ApellidoMaterno)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(d => d.RFC)
                      .HasMaxLength(13);

                entity.Property(d => d.Telefono)
                      .HasMaxLength(10);

                entity.HasCheckConstraint(
                    "CK_Docentes_RFC_Formato",
                    "RFC IS NULL OR RFC REGEXP '^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$'"
                );

                entity.HasCheckConstraint(
                    "CK_Docentes_Telefono10",
                    "Telefono IS NULL OR Telefono REGEXP '^[0-9]{10}$'"
                );

                entity.HasOne<Usuarios>()
                      .WithMany()
                      .HasForeignKey(d => d.idUsuario)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureInvitacionProyecto(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvitacionProyecto>(entity =>
            {
                entity.ToTable("InvitacionProyecto");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Estado)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue("PENDIENTE");

                entity.Property(x => x.FechaCreacion)
                  .HasColumnType("timestamp(6)")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                  .ValueGeneratedOnAdd();


                // FK Proyecto
                entity.HasOne<Proyectos>()
                      .WithMany()
                      .HasForeignKey(x => x.IdProyecto)
                      .OnDelete(DeleteBehavior.Cascade);

                // FK Estudiante invitado
                entity.HasOne<Estudiantes>()
                      .WithMany()
                      .HasForeignKey(x => x.IdEstudianteInvitado)
                      .OnDelete(DeleteBehavior.Cascade);

                // FK Estudiante creador
                entity.HasOne<Estudiantes>()
                      .WithMany()
                      .HasForeignKey(x => x.IdEstudianteCreador)
                      .OnDelete(DeleteBehavior.Restrict);

                // Evita duplicar invitaciones para el mismo proyecto+estudiante invitado
                entity.HasIndex(x => new { x.IdProyecto, x.IdEstudianteInvitado })
                      .IsUnique();

                // (Opcional) Restricción de estados válidos (si tu MySQL lo soporta con CHECK)
                entity.HasCheckConstraint(
                    "CK_InvitacionProyecto_Estado",
                    "Estado IN ('PENDIENTE','ACEPTADA','RECHAZADA','CANCELADA')"
                );
            });
        }


        // =========================== PERMISOS =========================
        private static void ConfigurePermisos(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Permisos>(entity =>
            {
                entity.ToTable("Permisos");

                entity.HasKey(p => p.id);

                entity.Property(p => p.Descripcion)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(p => p.Activo)
                      .HasDefaultValue(true);

                entity.HasData(
                    new Permisos { id = 1, Descripcion = "Estudiante-Create", Activo = true },
                    new Permisos { id = 2, Descripcion = "Estudiante-Update", Activo = true },
                    new Permisos { id = 3, Descripcion = "Estudiante-Read", Activo = true },

                    new Permisos { id = 4, Descripcion = "Docente-Create", Activo = true },
                    new Permisos { id = 5, Descripcion = "Docente-Update", Activo = true },
                    new Permisos { id = 6, Descripcion = "Docente-Read", Activo = true },

                    new Permisos { id = 7, Descripcion = "Empresa-Create", Activo = true },
                    new Permisos { id = 8, Descripcion = "Empresa-Update", Activo = true },
                    new Permisos { id = 9, Descripcion = "Empresa-Read", Activo = true },

                    new Permisos { id = 10, Descripcion = "Proyecto-Create", Activo = true },
                    new Permisos { id = 11, Descripcion = "Proyecto-Update", Activo = true },
                    new Permisos { id = 12, Descripcion = "Proyecto-Read", Activo = true },

                    new Permisos { id = 13, Descripcion = "Usuario-Create", Activo = true },
                    new Permisos { id = 14, Descripcion = "Usuario-Update", Activo = true },
                    new Permisos { id = 15, Descripcion = "Usuario-Read", Activo = true },

                    new Permisos { id = 16, Descripcion = "Repositorio-Read", Activo = true },
                    new Permisos { id = 17, Descripcion = "Repositorio-Select", Activo = true },
                    new Permisos { id = 18, Descripcion = "Repositorio-Create", Activo = true },

                    new Permisos { id = 19, Descripcion = "Seguimiento-Read", Activo = true },
                    new Permisos { id = 20, Descripcion = "Seguimiento-Edit", Activo = true },

                    new Permisos { id = 21, Descripcion = "Perfil-Read", Activo = true },
                    new Permisos { id = 22, Descripcion = "Perfil-Edit", Activo = true },
                    new Permisos { id = 23, Descripcion = "PeriodoAcademico-Read", Activo = true },
                    new Permisos { id = 24, Descripcion = "DocenteProyecto-Edit", Activo = true },
                    new Permisos { id = 25, Descripcion = "DocenteProyecto-Read", Activo = true },
                    new Permisos { id = 26, Descripcion = "Proyecto-Sustituir", Activo = true }
                );
            });
        }

        // ============================== ROL ===========================
        private static void ConfigureRol(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.ToTable("Rol");
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Descripcion)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasData(
                    new Rol { Id = 1, Descripcion = "Administrador", Activo = true },
                    new Rol { Id = 2, Descripcion = "Estudiante", Activo = true },
                    new Rol { Id = 3, Descripcion = "Docente", Activo = true },
                    new Rol { Id = 4, Descripcion = "Jefe de vinculación", Activo = true },
                    new Rol { Id = 5, Descripcion = "Jefe de Departamento", Activo = true }
                );
            });
        }

        // ========================= USUARIO-ROL ========================
        private static void ConfigureUsuarioRol(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioRol>(entity =>
            {
                entity.ToTable("UsuarioRol");
                entity.HasKey(ur => ur.Id);

                entity.HasOne<Usuarios>()
                      .WithMany()
                      .HasForeignKey(ur => ur.IdUsuario)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Rol>()
                      .WithMany()
                      .HasForeignKey(ur => ur.IdRol)
                      .OnDelete(DeleteBehavior.Cascade);

                // ✅ Seed: el usuario 1 es Administrador (rol 1)
                entity.HasData(
                    new UsuarioRol { Id = 1, IdUsuario = 1, IdRol = 1 }
                );
            });
        }

        // ========================= ROL-PERMISO ========================
        private static void ConfigureRolPermiso(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RolPermiso>(entity =>
            {
                entity.ToTable("RolPermiso");
                entity.HasKey(rp => rp.Id);

                entity.HasOne<Rol>()
                      .WithMany()
                      .HasForeignKey(rp => rp.idRol)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Permisos>()
                      .WithMany()
                      .HasForeignKey(rp => rp.idPermiso)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasData(
                    // ✅ Administrador: TODOS (1..22)
                    new RolPermiso { Id = 1, idRol = 1, idPermiso = 1 },
                    new RolPermiso { Id = 2, idRol = 1, idPermiso = 2 },
                    new RolPermiso { Id = 3, idRol = 1, idPermiso = 3 },
                    new RolPermiso { Id = 4, idRol = 1, idPermiso = 4 },
                    new RolPermiso { Id = 5, idRol = 1, idPermiso = 5 },
                    new RolPermiso { Id = 6, idRol = 1, idPermiso = 6 },
                    new RolPermiso { Id = 14, idRol = 1, idPermiso = 7 },
                    new RolPermiso { Id = 15, idRol = 1, idPermiso = 8 },
                    new RolPermiso { Id = 16, idRol = 1, idPermiso = 9 },
                    new RolPermiso { Id = 17, idRol = 1, idPermiso = 10 },
                    new RolPermiso { Id = 18, idRol = 1, idPermiso = 11 },
                    new RolPermiso { Id = 19, idRol = 1, idPermiso = 12 },
                    new RolPermiso { Id = 20, idRol = 1, idPermiso = 13 },
                    new RolPermiso { Id = 21, idRol = 1, idPermiso = 14 },
                    new RolPermiso { Id = 22, idRol = 1, idPermiso = 15 },
                    new RolPermiso { Id = 23, idRol = 1, idPermiso = 16 },
                    new RolPermiso { Id = 24, idRol = 1, idPermiso = 17 },
                    new RolPermiso { Id = 25, idRol = 1, idPermiso = 18 },
                    new RolPermiso { Id = 26, idRol = 1, idPermiso = 19 },
                    new RolPermiso { Id = 27, idRol = 1, idPermiso = 20 },
                    new RolPermiso { Id = 28, idRol = 1, idPermiso = 21 },
                    new RolPermiso { Id = 29, idRol = 1, idPermiso = 22 },
                    new RolPermiso { Id = 30, idRol = 1, idPermiso = 23 },
                    new RolPermiso { Id = 31, idRol = 1, idPermiso = 24 },

                    // Estudiante (tus seeds existentes)
                    new RolPermiso { Id = 7, idRol = 2, idPermiso = 16 },
                    new RolPermiso { Id = 8, idRol = 2, idPermiso = 17 },
                    new RolPermiso { Id = 9, idRol = 2, idPermiso = 18 },
                    new RolPermiso { Id = 10, idRol = 2, idPermiso = 19 },
                    new RolPermiso { Id = 11, idRol = 2, idPermiso = 20 },
                    new RolPermiso { Id = 12, idRol = 2, idPermiso = 21 },
                    new RolPermiso { Id = 13, idRol = 2, idPermiso = 22 },

                    //Docente
                    new RolPermiso { Id = 32, idRol = 3, idPermiso = 24 },
                    new RolPermiso { Id = 55, idRol = 3, idPermiso = 25 }, 
                    new RolPermiso { Id = 56, idRol = 3, idPermiso = 21 }, 
                    new RolPermiso { Id = 57, idRol = 3, idPermiso = 22 }, 


                    //Jefe de Vinculacion: TODOS (1..22)
                    new RolPermiso { Id = 33, idRol = 4, idPermiso = 1 },
                    new RolPermiso { Id = 34, idRol = 4, idPermiso = 2 },
                    new RolPermiso { Id = 35, idRol = 4, idPermiso = 3 },
                    new RolPermiso { Id = 36, idRol = 4, idPermiso = 4 },
                    new RolPermiso { Id = 37, idRol = 4, idPermiso = 5 },
                    new RolPermiso { Id = 38, idRol = 4, idPermiso = 6 },
                    new RolPermiso { Id = 39, idRol = 4, idPermiso = 7 },
                    new RolPermiso { Id = 40, idRol = 4, idPermiso = 8 },
                    new RolPermiso { Id = 41, idRol = 4, idPermiso = 9 },
                    new RolPermiso { Id = 42, idRol = 4, idPermiso = 10 },
                    new RolPermiso { Id = 43, idRol = 4, idPermiso = 11 },
                    new RolPermiso { Id = 44, idRol = 4, idPermiso = 12 },
                    new RolPermiso { Id = 45, idRol = 4, idPermiso = 13 },
                    new RolPermiso { Id = 46, idRol = 4, idPermiso = 14 },
                    new RolPermiso { Id = 47, idRol = 4, idPermiso = 15 },
                    new RolPermiso { Id = 48, idRol = 4, idPermiso = 19 },
                    new RolPermiso { Id = 49, idRol = 4, idPermiso = 20 },
                    new RolPermiso { Id = 50, idRol = 4, idPermiso = 21 },
                    new RolPermiso { Id = 51, idRol = 4, idPermiso = 22 },
                    new RolPermiso { Id = 52, idRol = 4, idPermiso = 23 },

                    // Proyecto-Sustituir (id=26): solo Admin y Jefe de Vinculación
                    new RolPermiso { Id = 58, idRol = 1, idPermiso = 26 },
                    new RolPermiso { Id = 59, idRol = 4, idPermiso = 26 }
                );
            });
        }

        private static void ConfigureTipoRelacionDocenteProyecto(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TipoRelacionDocenteProyecto>(entity =>
            {
                entity.ToTable("TipoRelacionDocenteProyecto");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Clave)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(x => x.Descripcion)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(x => x.Activo)
                      .HasDefaultValue(true);

                entity.HasIndex(x => x.Clave).IsUnique();

                entity.HasData(
                    new TipoRelacionDocenteProyecto { Id = 1, Clave = "REVISOR_ANTEPROYECTO", Descripcion = "Revisor de anteproyecto", Activo = true },
                    new TipoRelacionDocenteProyecto { Id = 2, Clave = "ASESOR_INTERNO", Descripcion = "Asesor interno", Activo = true },
                    new TipoRelacionDocenteProyecto { Id = 3, Clave = "REVISOR_RESIDENCIA", Descripcion = "Revisor de residencia", Activo = true }
                );
            });
        }

        private static void ConfigureProyectoDocente(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProyectoDocente>(entity =>
            {
                entity.ToTable("proyectodocente");
                entity.HasKey(pd => pd.id);

                entity.HasOne<Proyectos>()
                      .WithMany()
                      .HasForeignKey(pd => pd.idProyecto)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Docentes>()
                      .WithMany()
                      .HasForeignKey(pd => pd.idDocente)
                      .OnDelete(DeleteBehavior.Cascade);

                // FK al tipo
                entity.HasOne<TipoRelacionDocenteProyecto>()
                      .WithMany()
                      .HasForeignKey(pd => pd.IdTipoRelacion)
                      .OnDelete(DeleteBehavior.Restrict);

// Índice para soportar FK y consultas por proyecto
    entity.HasIndex(e => e.idProyecto)
          .HasDatabaseName("IX_ProyectoDocente_idProyecto");

    // Índice NO-UNIQUE para consultas por proyecto+tipo
    entity.HasIndex(e => new { e.idProyecto, e.IdTipoRelacion })
          .HasDatabaseName("IX_ProyectoDocente_idProyecto_IdTipoRelacion");

    // UNIQUE para evitar duplicar el mismo docente en el mismo proyecto+tipo
    entity.HasIndex(e => new { e.idProyecto, e.IdTipoRelacion, e.idDocente })
          .IsUnique()
          .HasDatabaseName("IX_ProyectoDocente_idProyecto_IdTipoRelacion_idDocente");
            });

            
        }

        // ====================== TIPO DOCUMENTO ======================
        private static void ConfigureTipoDocumento(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<TipoDocumento>(entity =>
    {
        entity.ToTable("TipoDocumento");

        entity.HasKey(t => t.Id);

        entity.Property(t => t.Descripcion)
              .IsRequired()
              .HasMaxLength(150);

        entity.Property(t => t.Activo)
              .HasDefaultValue(true);

              entity.Property(x => x.Descripcion)
    .IsRequired()
    .HasMaxLength(500);

        entity.HasData(
    new TipoDocumento { Id = 1, Descripcion = "Dictamen de autorización de comité académico (cuando sea necesario)", Activo = true },
    new TipoDocumento { Id = 2, Descripcion = "Solicitud de residencia sellada por la División de Estudios Profesionales", Activo = true },
    new TipoDocumento { Id = 3, Descripcion = "Cronograma requisitado al 100%", Activo = true },
    new TipoDocumento { Id = 4, Descripcion = "Carta de presentación, con sello de la empresa, institución u organización", Activo = true },
    new TipoDocumento { Id = 5, Descripcion = "Carta de aceptación sellada por la División de Estudios Profesionales", Activo = true },
    new TipoDocumento { Id = 6, Descripcion = "Reporte parcial No. 1 sellado por la Div. de Est. Profesionales acompañado de la hoja de revisores firmando asesor interno y REV1 (semana 6 después del inicio)", Activo = true },
    new TipoDocumento { Id = 7, Descripcion = "Reporte parcial No. 2 sellado por la Div. de Est. Profesionales acompañado de la hoja de revisores firmando asesor interno y REV1 (semana 12 después del inicio)", Activo = true },
    new TipoDocumento { Id = 8, Descripcion = "Reporte final sellado por la Div. de Est. Profesionales acompañado de la hoja de revisores firmando asesor interno y REV1 (al finalizar la residencia)", Activo = true },
    new TipoDocumento { Id = 9, Descripcion = "Carta de terminación sellada por la División de Estudios Profesionales", Activo = true },
    new TipoDocumento { Id = 10, Descripcion = "Portada con firma de autorización", Activo = true },
    new TipoDocumento { Id = 11, Descripcion = "Adjuntar en carpeta los proyectos en digital (software, manuales e informe técnico final)", Activo = true },
    new TipoDocumento { Id = 12, Descripcion = "Acta de calificación (asesor interno)", Activo = true }
);
    });
}
        // ========================== DOCUMENTO ==========================
        private static void ConfigureDocumento(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Documento>(entity =>
            {
                entity.ToTable("Documentos");
                entity.HasKey(d => d.Id);

                entity.Property(d => d.NombreOriginal).HasMaxLength(255).IsRequired();
                entity.Property(d => d.NombreServidor).HasMaxLength(255).IsRequired();
                entity.Property(d => d.ContentType).HasMaxLength(150).IsRequired();
                entity.Property(d => d.RutaFisica).HasMaxLength(500).IsRequired();

                // Estudiante (1) -> Documento (N)
                entity.HasOne<Estudiantes>()
                      .WithMany()
                      .HasForeignKey(d => d.IdEstudiante)
                      .OnDelete(DeleteBehavior.Restrict);

                // TipoDocumento (1) -> Documento (N)
                // OJO: tu FK se llama "TipoDocumento" (int). Se mapea así.
                entity.HasOne<TipoDocumento>()
                      .WithMany()
                      .HasForeignKey(d => d.TipoDocumento)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índice útil para listar rápido por estudiante y tipo
                // ✅ 1 documento por estudiante + tipo (1–7)
                entity.HasIndex(d => new { d.IdEstudiante, d.TipoDocumento })
                      .IsUnique();

                entity.Property(d => d.UrlExterna).HasMaxLength(1024);

            });
        }

        private static void ConfigureTipoEntregable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TipoEntregable>(entity =>
            {
                entity.ToTable("TipoEntregable");
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Descripcion)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(t => t.Activo)
                      .HasDefaultValue(true);

                entity.Property(t => t.MaxRevisiones)
                      .IsRequired(false);

                // Seeds mínimos (ajusta los nombres a tu gusto)
                entity.HasData(
                    new TipoEntregable { Id = 1, Descripcion = "Anteproyecto", MaxRevisiones = 3, Activo = true },
                    new TipoEntregable { Id = 2, Descripcion = "Evaluación Parcial 1", MaxRevisiones = null, Activo = true },
                    new TipoEntregable { Id = 3, Descripcion = "Evaluación Parcial 2", MaxRevisiones = null, Activo = true },
                    new TipoEntregable { Id = 4, Descripcion = "Evaluación Final", MaxRevisiones = null, Activo = true }
                );
            });
        }
        private static void ConfigureEntregable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entregable>(entity =>
            {
                entity.ToTable("Entregables");
                entity.HasKey(e => e.Id);

                // IdEstadoEntregable es INT, default debe ser INT
                entity.Property(e => e.IdEstadoEntregable)
                      .HasDefaultValue(1); // 1 = PENDIENTE (según tu tabla EstadoEntregables)


                entity.Property(e => e.FechaCreacion)
                      .HasColumnType("datetime(6)")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.VersionActual)
                      .HasDefaultValue(0);

                // FK a Proyecto
                entity.HasOne<Proyectos>()
                      .WithMany()
                      .HasForeignKey(e => e.IdProyecto)
                      .OnDelete(DeleteBehavior.Cascade);

                // FK a TipoEntregable
                entity.HasOne<TipoEntregable>()
                      .WithMany()
                      .HasForeignKey(e => e.IdTipoEntregable)
                      .OnDelete(DeleteBehavior.Restrict);

                // FK a Estudiante autor (quién “lo creó”)
                entity.HasOne<Estudiantes>()
                      .WithMany()
                      .HasForeignKey(e => e.IdEstudianteAutor)
                      .OnDelete(DeleteBehavior.Restrict);

                // Un entregable por tipo por proyecto (una cabecera)
                entity.HasIndex(e => new { e.IdProyecto, e.IdTipoEntregable })
                      .IsUnique();

                // ✅ FK a EstadoEntregable (faltante en BD)
                entity.HasOne<EstadoEntregable>()
                      .WithMany()
                      .HasForeignKey(e => e.IdEstadoEntregable)
                      .OnDelete(DeleteBehavior.Restrict);

                // (Opcional) índice si vas a filtrar por estado seguido
                entity.HasIndex(e => e.IdEstadoEntregable);


            });
        }
        private static void ConfigureEntregableVersion(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EntregableVersion>(entity =>
            {
                entity.ToTable("EntregableVersion");
                entity.HasKey(v => v.Id);

                entity.Property(v => v.FechaSubida)
                      .HasColumnType("datetime(6)")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                      .ValueGeneratedOnAdd();

                entity.Property(v => v.NombreOriginal).HasMaxLength(255).IsRequired();
                entity.Property(v => v.NombreServidor).HasMaxLength(255).IsRequired();
                entity.Property(v => v.ContentType).HasMaxLength(150).IsRequired();
                entity.Property(v => v.RutaFisica).HasMaxLength(500).IsRequired();

                // FK → Entregable (cabecera)
                entity.HasOne<Entregable>()
                      .WithMany()
                      .HasForeignKey(v => v.IdEntregable)
                      .OnDelete(DeleteBehavior.Cascade);

                // FK → Estudiante que subió ESA versión
                entity.HasOne<Estudiantes>()
                      .WithMany()
                      .HasForeignKey(v => v.IdEstudianteSubio)
                      .OnDelete(DeleteBehavior.Restrict);

                // Versiones únicas dentro del mismo entregable
                entity.HasIndex(v => new { v.IdEntregable, v.NumeroVersion })
                      .IsUnique();

                entity.HasCheckConstraint(
                    "CK_EntregableVersion_NumeroVersion",
                    "NumeroVersion >= 1"
                );
            });
        }
        private static void ConfigureRevisionEntregable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RevisionEntregable>(entity =>
            {
                entity.ToTable("RevisionEntregable", t =>
                {
                    t.HasCheckConstraint(
                        "CK_RevisionEntregable_Dictamen",
                        "Dictamen IN ('CAMBIOS','APROBADO','RECHAZADO')"
                    );

                    t.HasCheckConstraint(
                        "CK_RevisionEntregable_NumeroRevision",
                        "NumeroRevision >= 1"
                    );
                });
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Dictamen)
                      .IsRequired()
                      .HasMaxLength(15)
                      .HasDefaultValue("CAMBIOS");

                entity.Property(r => r.Observaciones)
                      .IsRequired()
                      .HasColumnType("longtext");

                entity.Property(r => r.FechaRevision)
                      .HasColumnType("datetime(6)")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                      .ValueGeneratedOnAdd();

                // FK → EntregableVersion
                entity.HasOne<EntregableVersion>()
                      .WithMany()
                      .HasForeignKey(r => r.IdEntregableVersion)
                      .OnDelete(DeleteBehavior.Cascade);

                // FK → Docente revisor
                entity.HasOne<Docentes>()
                      .WithMany()
                      .HasForeignKey(r => r.IdDocenteRevisor)
                      .OnDelete(DeleteBehavior.Restrict);

                // Revisiones únicas por versión
                entity.HasIndex(r => new { r.IdEntregableVersion, r.NumeroRevision })
                      .IsUnique();
            });
        }

    }
}
