using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApiVinculacionProyectosV2.Migrations
{
    /// <inheritdoc />
    public partial class EstadoDocumentoxpediente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComentarioRevision",
                table: "Documentos",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_spanish_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "EstadoRevision",
                table: "Documentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRevision",
                table: "Documentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisadoPorUsuarioId",
                table: "Documentos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComentarioRevision",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "EstadoRevision",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "FechaRevision",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "RevisadoPorUsuarioId",
                table: "Documentos");
        }
    }
}
