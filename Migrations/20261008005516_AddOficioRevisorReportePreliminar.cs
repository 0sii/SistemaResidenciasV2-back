using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApiVinculacionProyectosV2.Migrations
{
    /// <inheritdoc />
    public partial class AddOficioRevisorReportePreliminar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OficioRevisorReportePreliminar",
                table: "Proyectos",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_spanish_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OficioRevisorReportePreliminar",
                table: "Proyectos");
        }
    }
}
