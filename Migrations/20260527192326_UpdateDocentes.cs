using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApiVinculacionProyectosV2.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        migrationBuilder.AddColumn<string>(
            name: "NivelAcademico",
            table: "Docentes",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EsJefeDepartamento",
            table: "Docentes",
            nullable: false,
            defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
