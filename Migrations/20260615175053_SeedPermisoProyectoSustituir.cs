using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApiVinculacionProyectosV2.Migrations
{
    /// <summary>
    /// Agrega el permiso "Proyecto-Sustituir" y lo asigna al rol
    /// Jefe de vinculación (Id=4) y Administrador (Id=1).
    /// Este permiso permite sustituir un docente (asesor o revisor)
    /// en un proyecto cuando hay una baja.
    /// </summary>
    public partial class SeedPermisoProyectoSustituir : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Insertar el permiso solo si no existe
            migrationBuilder.Sql(@"
                INSERT INTO permisos (Descripcion, Activo)
                SELECT 'Proyecto-Sustituir', 1
                WHERE NOT EXISTS (
                    SELECT 1 FROM permisos
                    WHERE Descripcion = 'Proyecto-Sustituir'
                );
            ");

            // 2) Asignarlo al rol Administrador (Id = 1)
            migrationBuilder.Sql(@"
                INSERT INTO rolpermiso (idRol, idPermiso)
                SELECT 1, Id FROM permisos
                WHERE Descripcion = 'Proyecto-Sustituir'
                AND NOT EXISTS (
                    SELECT 1 FROM rolpermiso
                    WHERE idRol = 1
                    AND idPermiso = (
                        SELECT Id FROM permisos WHERE Descripcion = 'Proyecto-Sustituir'
                    )
                );
            ");

            // 3) Asignarlo al rol Jefe de vinculación (Id = 4)
            migrationBuilder.Sql(@"
                INSERT INTO rolpermiso (idRol, idPermiso)
                SELECT 4, Id FROM permisos
                WHERE Descripcion = 'Proyecto-Sustituir'
                AND NOT EXISTS (
                    SELECT 1 FROM rolpermiso
                    WHERE idRol = 4
                    AND idPermiso = (
                        SELECT Id FROM permisos WHERE Descripcion = 'Proyecto-Sustituir'
                    )
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Quitar asignaciones primero
            migrationBuilder.Sql(@"
                DELETE FROM rolpermiso
                WHERE idPermiso = (
                    SELECT Id FROM permisos WHERE Descripcion = 'Proyecto-Sustituir'
                );
            ");

            // Luego eliminar el permiso
            migrationBuilder.Sql(@"
                DELETE FROM permisos WHERE Descripcion = 'Proyecto-Sustituir';
            ");
        }
    }
}
