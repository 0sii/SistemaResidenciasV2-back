using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApiVinculacionProyectosV2.Migrations
{
    /// <inheritdoc />
    public partial class updateExpediente2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "TipoDocumento",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                collation: "utf8mb4_spanish_ci",
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 2,
                column: "Descripcion",
                value: "Solicitud de residencia sellada por la División de Estudios Profesionales");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 3,
                column: "Descripcion",
                value: "Cronograma requisitado al 100%");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 4,
                column: "Descripcion",
                value: "Carta de presentación, con sello de la empresa, institución u organización");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 5,
                column: "Descripcion",
                value: "Carta de aceptación sellada por la División de Estudios Profesionales");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 6,
                column: "Descripcion",
                value: "Reporte parcial No. 1 sellado por la Div. de Est. Profesionales acompañado de la hoja de revisores firmando asesor interno y REV1 (semana 6 después del inicio)");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 7,
                column: "Descripcion",
                value: "Reporte parcial No. 2 sellado por la Div. de Est. Profesionales acompañado de la hoja de revisores firmando asesor interno y REV1 (semana 12 después del inicio)");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 8,
                column: "Descripcion",
                value: "Reporte final sellado por la Div. de Est. Profesionales acompañado de la hoja de revisores firmando asesor interno y REV1 (al finalizar la residencia)");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 9,
                column: "Descripcion",
                value: "Carta de terminación sellada por la División de Estudios Profesionales");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 10,
                column: "Descripcion",
                value: "Portada con firma de autorización");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 11,
                column: "Descripcion",
                value: "Adjuntar en carpeta los proyectos en digital (software, manuales e informe técnico final)");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 12,
                column: "Descripcion",
                value: "Acta de calificación (asesor interno)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "TipoDocumento",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                collation: "utf8mb4_spanish_ci",
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 2,
                column: "Descripcion",
                value: "Horario");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 3,
                column: "Descripcion",
                value: "Cronograma 100%");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 4,
                column: "Descripcion",
                value: "Solicitud de residencia sellada por la Div. Est. Prof.");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 5,
                column: "Descripcion",
                value: "Carta de presentación");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 6,
                column: "Descripcion",
                value: "Carta compromiso");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 7,
                column: "Descripcion",
                value: "Carta de aceptación");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 8,
                column: "Descripcion",
                value: "Reporte parcial No. 1 sellado + hoja de revisores (asesor interno y REV1)");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 9,
                column: "Descripcion",
                value: "Reporte parcial No. 2 sellado + hoja de revisores (asesor interno y REV1)");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 10,
                column: "Descripcion",
                value: "Reporte final sellado + hoja de revisores (asesor interno, REV1, REV2 y REV3)");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 11,
                column: "Descripcion",
                value: "Carta de terminación sellada por Gest. Tec.");

            migrationBuilder.UpdateData(
                table: "TipoDocumento",
                keyColumn: "Id",
                keyValue: 12,
                column: "Descripcion",
                value: "CD: informe final + SW instalable + manual técnico + manual de usuario (portada en etiqueta)");

            migrationBuilder.InsertData(
                table: "TipoDocumento",
                columns: new[] { "Id", "Activo", "Descripcion" },
                values: new object[] { 13, true, "Acta de residencia profesional" });
        }
    }
}
