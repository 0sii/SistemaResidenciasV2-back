using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApiVinculacionProyectosV2.Migrations
{
    public partial class SeedPermisoEgresadoRead : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO permisos (Descripcion)
                SELECT 'Egresado-Read'
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM permisos
                    WHERE Descripcion = 'Egresado-Read'
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO rolpermiso (idRol, idPermiso)
                SELECT 1, Id
                FROM permisos
                WHERE Descripcion = 'Egresado-Read'
                AND NOT EXISTS (
                    SELECT 1
                    FROM rolpermiso
                    WHERE idRol = 1
                    AND idPermiso = (
                        SELECT Id
                        FROM permisos
                        WHERE Descripcion = 'Egresado-Read'
                    )
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO rolpermiso (idRol, idPermiso)
                SELECT 4, Id
                FROM permisos
                WHERE Descripcion = 'Egresado-Read'
                AND NOT EXISTS (
                    SELECT 1
                    FROM rolpermiso
                    WHERE idRol = 4
                    AND idPermiso = (
                        SELECT Id
                        FROM permisos
                        WHERE Descripcion = 'Egresado-Read'
                    )
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO rolpermiso (idRol, idPermiso)
                SELECT 5, Id
                FROM permisos
                WHERE Descripcion = 'Egresado-Read'
                AND NOT EXISTS (
                    SELECT 1
                    FROM rolpermiso
                    WHERE idRol = 5
                    AND idPermiso = (
                        SELECT Id
                        FROM permisos
                        WHERE Descripcion = 'Egresado-Read'
                    )
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM rolpermiso
                WHERE idPermiso = (
                    SELECT Id
                    FROM permisos
                    WHERE Descripcion = 'Egresado-Read'
                );
            ");

            migrationBuilder.Sql(@"
                DELETE FROM permisos
                WHERE Descripcion = 'Egresado-Read';
            ");
        }
    }
}