using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApiVinculacionProyectosV2.Migrations
{
    /// <inheritdoc />
    public partial class AddOficiosEmitidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oficiosemitidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdPeriodoAcademico = table.Column<int>(type: "int", nullable: false),
                    TipoClave = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdDocente = table.Column<int>(type: "int", nullable: false),
                    IdProyecto = table.Column<int>(type: "int", nullable: false),
                    IdEstudiante = table.Column<int>(type: "int", nullable: false),
                    NumeroOficio = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaEmision = table.Column<DateTime>(type: "date", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oficiosemitidos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateIndex(
                name: "UX_OficiosEmitidos_Llave",
                table: "oficiosemitidos",
                columns: new[] { "IdPeriodoAcademico", "TipoClave", "IdDocente", "IdProyecto", "IdEstudiante" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_OficiosEmitidos_Numero",
                table: "oficiosemitidos",
                columns: new[] { "IdPeriodoAcademico", "NumeroOficio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oficiosemitidos");
        }
    }
}
