using WebApiVinculacionProyectosV2.Models;
using Microsoft.EntityFrameworkCore;

public interface INotificacionesService
{
    Task AvisarRevisionEntregableSubidoAsync(int idEntregable, int idEstudianteSubio, int numeroVersion);

}



namespace WebApiVinculacionProyectosV2.Servicios
{
    public class NotificacionesService : INotificacionesService
    {
        private readonly ResidenciasDbContext _db;
        private readonly IServicioEmail _email;

        public NotificacionesService(ResidenciasDbContext db, IServicioEmail email)
        {
            _db = db;
            _email = email;
        }

        public async Task AvisarRevisionEntregableSubidoAsync(int idEntregable, int idEstudianteSubio, int numeroVersion)
        {
            // 1) Entregable básico
            var ent = await _db.Entregables.AsNoTracking()
                .Where(x => x.Id == idEntregable)
                .Select(x => new { x.Id, x.IdProyecto, x.IdTipoEntregable })
                .FirstOrDefaultAsync();

            if (ent == null) return;

            // 2) Resolver tipo entregable por catálogo (sin hardcodear IDs)
            var tipo = await _db.TipoEntregables.AsNoTracking()
                .Where(t => t.Id == ent.IdTipoEntregable && t.Activo)
                .Select(t => t.Descripcion)
                .FirstOrDefaultAsync();

            var tipoNorm = (tipo ?? "").Trim().ToUpperInvariant();

            // 3) Destinatarios según tipo
            List<string> clavesRol;
            bool soloUno;

            if (tipoNorm == "ANTEPROYECTO")
            {
                clavesRol = new() { "REVISOR_ANTEPROYECTO" };
                soloUno = true;
            }
            else if (tipoNorm is "EVALUACIÓN PARCIAL 1" or "EVALUACION PARCIAL 1"
                          or "EVALUACIÓN PARCIAL 2" or "EVALUACION PARCIAL 2"
                          or "EVALUACIÓN FINAL" or "EVALUACION FINAL")
            {
                // 👇 Ajusta "REVISOR_REPORTE" si tu clave real se llama distinto
                clavesRol = new() { "ASESOR_INTERNO", "REVISOR_REPORTE" };
                soloUno = false;
            }
            else
            {
                return; // si es otro tipo, no avisamos
            }

            // 4) Correos de destinatarios (1 o muchos)
            var correos = await (
                from pd in _db.ProyectoDocente.AsNoTracking()
                join tr in _db.TipoRelacionDocenteProyecto.AsNoTracking() on pd.IdTipoRelacion equals tr.Id
                join d in _db.Docentes.AsNoTracking() on pd.idDocente equals d.Id
                join u in _db.Usuarios.AsNoTracking() on d.idUsuario equals u.Id
                where pd.idProyecto == ent.IdProyecto && clavesRol.Contains(tr.Clave)
                select u.Correo
            ).ToListAsync();

            correos = correos
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (soloUno && correos.Count > 1)
            {
                // si hay más de 1 revisor anteproyecto asignado (no debería),
                // avisa solo al primero para evitar spam.
                correos = new List<string> { correos[0] };
            }

            if (!correos.Any()) return;

            // 5) Info alumno
            var alumno = await _db.Estudiantes.AsNoTracking()
                .Where(e => e.id == idEstudianteSubio)
                .Select(e => new { e.noControl, e.Nombre, e.ApellidoPaterno, e.ApellidoMaterno })
                .FirstOrDefaultAsync();

            var alumnoNombre = alumno == null
                ? $"Estudiante #{idEstudianteSubio}"
                : $"{alumno.Nombre} {alumno.ApellidoPaterno} {alumno.ApellidoMaterno}".Trim();

            // 6) Proyecto
            var proyectoTitulo = await _db.Proyectos.AsNoTracking()
                .Where(p => p.Id == ent.IdProyecto)
                .Select(p => p.Titulo)
                .FirstOrDefaultAsync();

            proyectoTitulo ??= $"Proyecto #{ent.IdProyecto}";

            // 7) Email
            var subject = $"{tipo?.Trim() ?? "Entregable"} listo para revisión · {proyectoTitulo}";
            var body =
$@"
<p>Se subió una nueva versión de <b>{tipo?.Trim()}</b>.</p>
<ul>
  <li><b>Proyecto:</b> {proyectoTitulo}</li>
  <li><b>Versión:</b> v{numeroVersion}</li>
  <li><b>Subido por:</b> {alumnoNombre} ({alumno?.noControl ?? "s/n"})</li>
</ul>
<p>Ingresa a la plataforma para revisarlo.</p>
";

            // Nota: si un correo falla, idealmente NO tumbar el flujo
            foreach (var c in correos)
            {
                try
                {
                    await _email.EnviarEmail(c, subject, body);
                }
                catch
                {
                    // aquí ideal: loggear (pero sin romper UploadVersion)
                }
            }
        }
    }
}
