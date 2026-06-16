using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebApiVinculacionProyectosV2.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Carreras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carreras", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Contactoemergencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Parentesco = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Domicilio = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contactoemergencia", x => x.Id);
                    table.CheckConstraint("CK_ContactoEmerg_EmailFormato", "email IS NULL OR email REGEXP '^[^[:space:]@]+@[^[:space:]@]+\\.[^[:space:]@]+$'");
                    table.CheckConstraint("CK_ContactoEmerg_TelFormato", "Telefono IS NULL OR Telefono REGEXP '^[0-9]{10}$'");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "DependenciaMedica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependenciaMedica", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RFC = table.Column<string>(type: "varchar(13)", maxLength: 13, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Giro = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mision = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Domicilio = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Colonia = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: true),
                    Municipio = table.Column<int>(type: "int", nullable: true),
                    Ciudad = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CP = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Titular = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PuestoTitular = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                    table.CheckConstraint("CK_Empresas_CP5", "CP IS NULL OR CP REGEXP '^[0-9]{5}$'");
                    table.CheckConstraint("CK_Empresas_RFC_Formato", "RFC REGEXP '^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$'");
                    table.CheckConstraint("CK_Empresas_Telefono10", "Telefono REGEXP '^[0-9]{10}$'");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Especializacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Especializacion", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Estado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Descripcion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estado", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "EstadoEntregable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Clave = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoEntregable", x => x.Id);
                    table.CheckConstraint("CK_EstadoEntregable_Clave", "Clave IN ('PENDIENTE','EN_REVISION','CAMBIOS','APROBADO','RECHAZADO','CANCELADO')");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Modalidad",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Descripcion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modalidad", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "PeriodosAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    JefeDepartamentoNombre = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrefijoOficio = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsecutivoOficio = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosAcademicos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Descripcion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Rol",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Descripcion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rol", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "SepomexColonias",
                columns: table => new
                {
                    ColoniaId = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstadoId = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MunicipioId = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cp = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cr = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaAct = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SepomexColonias", x => x.ColoniaId);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "SepomexEstados",
                columns: table => new
                {
                    EstadoId = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Abreviatura = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rango1 = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rango2 = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SepomexEstados", x => x.EstadoId);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "SepomexMunicipios",
                columns: table => new
                {
                    EstadoId = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MunicipioId = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rango1 = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rango2 = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SepomexMunicipios", x => new { x.EstadoId, x.MunicipioId });
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "TipoDocumento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Descripcion = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoDocumento", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "TipoEntregable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Descripcion = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxRevisiones = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoEntregable", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "TipoRelacionDocenteProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Clave = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoRelacionDocenteProyecto", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Correo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApellidoPaterno = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApellidoMaterno = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.CheckConstraint("CK_Usuarios_Correo_Formato", "Correo REGEXP '^[^[:space:]@]+@[^[:space:]@]+\\.[^[:space:]@]+$'");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Contacto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    correo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacto_Empresas_IdEmpresa",
                        column: x => x.IdEmpresa,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "PeriodosMembrentados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PeriodoAcademicoId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PdfBytes = table.Column<byte[]>(type: "longblob", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosMembrentados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodosMembrentados_PeriodosAcademicos_PeriodoAcademicoId",
                        column: x => x.PeriodoAcademicoId,
                        principalTable: "PeriodosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "RolPermiso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    idRol = table.Column<int>(type: "int", nullable: false),
                    idPermiso = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermiso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolPermiso_Permisos_idPermiso",
                        column: x => x.idPermiso,
                        principalTable: "Permisos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolPermiso_Rol_idRol",
                        column: x => x.idRol,
                        principalTable: "Rol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Docentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    idUsuario = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApellidoPaterno = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApellidoMaterno = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RFC = table.Column<string>(type: "varchar(13)", maxLength: 13, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Docentes", x => x.Id);
                    table.CheckConstraint("CK_Docentes_RFC_Formato", "RFC IS NULL OR RFC REGEXP '^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$'");
                    table.CheckConstraint("CK_Docentes_Telefono10", "Telefono IS NULL OR Telefono REGEXP '^[0-9]{10}$'");
                    table.ForeignKey(
                        name: "FK_Docentes_Usuarios_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "UsuarioRol",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    IdRol = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRol", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioRol_Rol_IdRol",
                        column: x => x.IdRol,
                        principalTable: "Rol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRol_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEstudiante = table.Column<int>(type: "int", nullable: false),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NombreOriginal = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreServidor = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    RutaFisica = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UrlExterna = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_TipoDocumento_TipoDocumento",
                        column: x => x.TipoDocumento,
                        principalTable: "TipoDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Entregables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdProyecto = table.Column<int>(type: "int", nullable: false),
                    IdTipoEntregable = table.Column<int>(type: "int", nullable: false),
                    IdEstudianteAutor = table.Column<int>(type: "int", nullable: false),
                    VersionActual = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IdEstadoEntregable = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entregables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entregables_EstadoEntregable_IdEstadoEntregable",
                        column: x => x.IdEstadoEntregable,
                        principalTable: "EstadoEntregable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entregables_TipoEntregable_IdTipoEntregable",
                        column: x => x.IdTipoEntregable,
                        principalTable: "TipoEntregable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "EntregableVersion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEntregable = table.Column<int>(type: "int", nullable: false),
                    NumeroVersion = table.Column<int>(type: "int", nullable: false),
                    IdEstudianteSubio = table.Column<int>(type: "int", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    NombreOriginal = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreServidor = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    RutaFisica = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregableVersion", x => x.Id);
                    table.CheckConstraint("CK_EntregableVersion_NumeroVersion", "NumeroVersion >= 1");
                    table.ForeignKey(
                        name: "FK_EntregableVersion_Entregables_IdEntregable",
                        column: x => x.IdEntregable,
                        principalTable: "Entregables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "RevisionEntregable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEntregableVersion = table.Column<int>(type: "int", nullable: false),
                    NumeroRevision = table.Column<int>(type: "int", nullable: false),
                    IdDocenteRevisor = table.Column<int>(type: "int", nullable: false),
                    Dictamen = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false, defaultValue: "CAMBIOS", collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Observaciones = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaRevision = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    NombreOriginal = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreServidor = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: true),
                    RutaFisica = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionEntregable", x => x.Id);
                    table.CheckConstraint("CK_RevisionEntregable_Dictamen", "Dictamen IN ('CAMBIOS','APROBADO','RECHAZADO')");
                    table.CheckConstraint("CK_RevisionEntregable_NumeroRevision", "NumeroRevision >= 1");
                    table.ForeignKey(
                        name: "FK_RevisionEntregable_Docentes_IdDocenteRevisor",
                        column: x => x.IdDocenteRevisor,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RevisionEntregable_EntregableVersion_IdEntregableVersion",
                        column: x => x.IdEntregableVersion,
                        principalTable: "EntregableVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Estudiantes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    idUsuario = table.Column<int>(type: "int", nullable: false),
                    idProyecto = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApellidoPaterno = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApellidoMaterno = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    idcarrera = table.Column<int>(type: "int", nullable: true),
                    domicilio = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ciudad = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cp = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    noControl = table.Column<string>(type: "varchar(9)", maxLength: 9, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    correoPersonal = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    noSeguroSocial = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    idDependenciaMedica = table.Column<int>(type: "int", nullable: true),
                    telefonoCelular = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    idContactoEmergencia = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estudiantes", x => x.id);
                    table.CheckConstraint("CK_Estudiantes_ApellidoMatSoloLetras", "ApellidoMaterno REGEXP '^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+( [A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+)*$'");
                    table.CheckConstraint("CK_Estudiantes_ApellidoPatSoloLetras", "ApellidoPaterno REGEXP '^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+( [A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+)*$'");
                    table.CheckConstraint("CK_Estudiantes_CorreoPersonalFormato", "correoPersonal IS NULL OR correoPersonal REGEXP '^[^[:space:]@]+@[^[:space:]@]+\\.[^[:space:]@]+$'");
                    table.CheckConstraint("CK_Estudiantes_NoControlFormato", "noControl IS NULL OR noControl REGEXP '^([0-9]{8}|[A-Za-z][0-9]{8})$'");
                    table.CheckConstraint("CK_Estudiantes_NombreSoloLetras", "Nombre REGEXP '^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+( [A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+)*$'");
                    table.CheckConstraint("CK_Estudiantes_TelCelFormato", "telefonoCelular IS NULL OR telefonoCelular REGEXP '^[0-9]{10}$'");
                    table.ForeignKey(
                        name: "FK_Estudiantes_Carreras_idcarrera",
                        column: x => x.idcarrera,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Estudiantes_Contactoemergencia_idContactoEmergencia",
                        column: x => x.idContactoEmergencia,
                        principalTable: "Contactoemergencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Estudiantes_DependenciaMedica_idDependenciaMedica",
                        column: x => x.idDependenciaMedica,
                        principalTable: "DependenciaMedica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Estudiantes_Usuarios_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "Proyectos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    idEspecializcion = table.Column<int>(type: "int", nullable: true),
                    Titulo = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Objetivo = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaRegistro = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    NoResidentes = table.Column<int>(type: "int", nullable: true),
                    HorarioInicio = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    HorarioFinal = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    IdPeriodoAcademico = table.Column<int>(type: "int", nullable: true),
                    idModalidad = table.Column<int>(type: "int", nullable: true),
                    idEstado = table.Column<int>(type: "int", nullable: true),
                    PropuestaAlumno = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IdEstudianteCreador = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proyectos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proyectos_Empresas_IdEmpresa",
                        column: x => x.IdEmpresa,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Proyectos_Especializacion_idEspecializcion",
                        column: x => x.idEspecializcion,
                        principalTable: "Especializacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Proyectos_Estado_idEstado",
                        column: x => x.idEstado,
                        principalTable: "Estado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Proyectos_Estudiantes_IdEstudianteCreador",
                        column: x => x.IdEstudianteCreador,
                        principalTable: "Estudiantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Proyectos_Modalidad_idModalidad",
                        column: x => x.idModalidad,
                        principalTable: "Modalidad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Proyectos_PeriodosAcademicos_IdPeriodoAcademico",
                        column: x => x.IdPeriodoAcademico,
                        principalTable: "PeriodosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "InvitacionProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdProyecto = table.Column<int>(type: "int", nullable: false),
                    IdEstudianteInvitado = table.Column<int>(type: "int", nullable: false),
                    IdEstudianteCreador = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDIENTE", collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    FechaRespuesta = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitacionProyecto", x => x.Id);
                    table.CheckConstraint("CK_InvitacionProyecto_Estado", "Estado IN ('PENDIENTE','ACEPTADA','RECHAZADA','CANCELADA')");
                    table.ForeignKey(
                        name: "FK_InvitacionProyecto_Estudiantes_IdEstudianteCreador",
                        column: x => x.IdEstudianteCreador,
                        principalTable: "Estudiantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvitacionProyecto_Estudiantes_IdEstudianteInvitado",
                        column: x => x.IdEstudianteInvitado,
                        principalTable: "Estudiantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvitacionProyecto_Proyectos_IdProyecto",
                        column: x => x.IdProyecto,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "ProyectoDocente",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    idProyecto = table.Column<int>(type: "int", nullable: false),
                    idDocente = table.Column<int>(type: "int", nullable: false),
                    IdTipoRelacion = table.Column<int>(type: "int", nullable: false),
                    FechaInscripcion = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoDocente", x => x.id);
                    table.ForeignKey(
                        name: "FK_ProyectoDocente_Docentes_idDocente",
                        column: x => x.idDocente,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoDocente_Proyectos_idProyecto",
                        column: x => x.idProyecto,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoDocente_TipoRelacionDocenteProyecto_IdTipoRelacion",
                        column: x => x.IdTipoRelacion,
                        principalTable: "TipoRelacionDocenteProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.CreateTable(
                name: "ProyectoDocumentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdProyecto = table.Column<int>(type: "int", nullable: false),
                    NombreOriginal = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreServidor = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    RutaFisica = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaSubida = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_spanish_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProyectoDocumentos_Proyectos_IdProyecto",
                        column: x => x.IdProyecto,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_spanish_ci");

            migrationBuilder.InsertData(
                table: "Carreras",
                columns: new[] { "Id", "Activo", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "Contador Público" },
                    { 2, true, "Licenciatura en Administración" },
                    { 3, true, "Ingeniería Química" },
                    { 4, true, "Ingeniería Mecánica" },
                    { 5, true, "Ingeniería Industrial" },
                    { 6, true, "Ingeniería en Sistemas Computacionales" },
                    { 7, true, "Ingeniería en Gestión Empresarial" },
                    { 8, true, "Ingeniería Electrónica" },
                    { 9, true, "Ingeniería Eléctrica" },
                    { 10, true, "Ingeniería Civil" }
                });

            migrationBuilder.InsertData(
                table: "DependenciaMedica",
                columns: new[] { "Id", "Activo", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "IMSS" },
                    { 2, true, "ISSSTE" },
                    { 3, true, "ISSFAM (Militar)" },
                    { 4, true, "PEMEX" },
                    { 5, true, "Seguro privado" },
                    { 6, true, "Servicios de Salud del Estado" },
                    { 7, true, "Sin seguridad social" }
                });

            migrationBuilder.InsertData(
                table: "Especializacion",
                columns: new[] { "id", "Activo", "descripcion" },
                values: new object[,]
                {
                    { 1, true, "Desarrollo de software" },
                    { 2, true, "Redes y telecomunicaciones" },
                    { 3, true, "Bases de datos" },
                    { 4, true, "Gestión de proyectos" }
                });

            migrationBuilder.InsertData(
                table: "Estado",
                columns: new[] { "Id", "Activo", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "Nuevo" },
                    { 2, true, "Disponible" },
                    { 3, true, "En Espera de Asignación de Revisor de Anteproyecto" },
                    { 4, true, "En Espera de Revisión de Anteproyecto" },
                    { 5, true, "Anteproyecto Revisado" },
                    { 6, true, "En Espera de Asignación de Asesor Interno" },
                    { 7, true, "En Curso" },
                    { 8, true, "Finalizado" },
                    { 9, true, "Cancelado" }
                });

            migrationBuilder.InsertData(
                table: "EstadoEntregable",
                columns: new[] { "Id", "Activo", "Clave", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "PENDIENTE", "Pendiente" },
                    { 2, true, "EN_REVISION", "En revisión" },
                    { 3, true, "CAMBIOS", "Con cambios" },
                    { 4, true, "APROBADO", "Aprobado" },
                    { 5, true, "RECHAZADO", "Rechazado" },
                    { 6, true, "CANCELADO", "Cancelado" }
                });

            migrationBuilder.InsertData(
                table: "Modalidad",
                columns: new[] { "id", "Activo", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "Presencial" },
                    { 2, true, "Mixta" },
                    { 3, true, "Virtual" }
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "id", "Activo", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "Estudiante-Create" },
                    { 2, true, "Estudiante-Update" },
                    { 3, true, "Estudiante-Read" },
                    { 4, true, "Docente-Create" },
                    { 5, true, "Docente-Update" },
                    { 6, true, "Docente-Read" },
                    { 7, true, "Empresa-Create" },
                    { 8, true, "Empresa-Update" },
                    { 9, true, "Empresa-Read" },
                    { 10, true, "Proyecto-Create" },
                    { 11, true, "Proyecto-Update" },
                    { 12, true, "Proyecto-Read" },
                    { 13, true, "Usuario-Create" },
                    { 14, true, "Usuario-Update" },
                    { 15, true, "Usuario-Read" },
                    { 16, true, "Repositorio-Read" },
                    { 17, true, "Repositorio-Select" },
                    { 18, true, "Repositorio-Create" },
                    { 19, true, "Seguimiento-Read" },
                    { 20, true, "Seguimiento-Edit" },
                    { 21, true, "Perfil-Read" },
                    { 22, true, "Perfil-Edit" },
                    { 23, true, "PeriodoAcademico-Read" },
                    { 24, true, "DocenteProyecto-Edit" },
                    { 25, true, "DocenteProyecto-Read" }
                });

            migrationBuilder.InsertData(
                table: "Rol",
                columns: new[] { "Id", "Activo", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "Administrador" },
                    { 2, true, "Estudiante" },
                    { 3, true, "Docente" },
                    { 4, true, "Jefe de vinculación" },
                    { 5, true, "Jefe de Departamento" }
                });

            migrationBuilder.InsertData(
                table: "TipoDocumento",
                columns: new[] { "Id", "Activo", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "Dictamen de autorización de comité académico (cuando sea necesario)" },
                    { 2, true, "Horario" },
                    { 3, true, "Cronograma 100%" },
                    { 4, true, "Solicitud de residencia sellada por la Div. Est. Prof." },
                    { 5, true, "Carta de presentación" },
                    { 6, true, "Carta compromiso" },
                    { 7, true, "Carta de aceptación" },
                    { 8, true, "Reporte parcial No. 1 sellado + hoja de revisores (asesor interno y REV1)" },
                    { 9, true, "Reporte parcial No. 2 sellado + hoja de revisores (asesor interno y REV1)" },
                    { 10, true, "Reporte final sellado + hoja de revisores (asesor interno, REV1, REV2 y REV3)" },
                    { 11, true, "Carta de terminación sellada por Gest. Tec." },
                    { 12, true, "CD: informe final + SW instalable + manual técnico + manual de usuario (portada en etiqueta)" },
                    { 13, true, "Acta de residencia profesional" }
                });

            migrationBuilder.InsertData(
                table: "TipoEntregable",
                columns: new[] { "Id", "Activo", "Descripcion", "MaxRevisiones" },
                values: new object[,]
                {
                    { 1, true, "Anteproyecto", 3 },
                    { 2, true, "Evaluación Parcial 1", null },
                    { 3, true, "Evaluación Parcial 2", null },
                    { 4, true, "Evaluación Final", null }
                });

            migrationBuilder.InsertData(
                table: "TipoRelacionDocenteProyecto",
                columns: new[] { "Id", "Activo", "Clave", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "REVISOR_ANTEPROYECTO", "Revisor de anteproyecto" },
                    { 2, true, "ASESOR_INTERNO", "Asesor interno" },
                    { 3, true, "REVISOR_RESIDENCIA", "Revisor de residencia" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Activo", "ApellidoMaterno", "ApellidoPaterno", "Correo", "Nombre", "PasswordHash" },
                values: new object[] { 1, true, "Trujillo", "Alvarez", "19161231@itoaxaca.edu.mx", "Luis Enrique", "8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92" });

            migrationBuilder.InsertData(
                table: "RolPermiso",
                columns: new[] { "Id", "idPermiso", "idRol" },
                values: new object[,]
                {
                    { 1, 1, 1 },
                    { 2, 2, 1 },
                    { 3, 3, 1 },
                    { 4, 4, 1 },
                    { 5, 5, 1 },
                    { 6, 6, 1 },
                    { 7, 16, 2 },
                    { 8, 17, 2 },
                    { 9, 18, 2 },
                    { 10, 19, 2 },
                    { 11, 20, 2 },
                    { 12, 21, 2 },
                    { 13, 22, 2 },
                    { 14, 7, 1 },
                    { 15, 8, 1 },
                    { 16, 9, 1 },
                    { 17, 10, 1 },
                    { 18, 11, 1 },
                    { 19, 12, 1 },
                    { 20, 13, 1 },
                    { 21, 14, 1 },
                    { 22, 15, 1 },
                    { 23, 16, 1 },
                    { 24, 17, 1 },
                    { 25, 18, 1 },
                    { 26, 19, 1 },
                    { 27, 20, 1 },
                    { 28, 21, 1 },
                    { 29, 22, 1 },
                    { 30, 23, 1 },
                    { 31, 24, 1 },
                    { 32, 24, 3 },
                    { 33, 1, 4 },
                    { 34, 2, 4 },
                    { 35, 3, 4 },
                    { 36, 4, 4 },
                    { 37, 5, 4 },
                    { 38, 6, 4 },
                    { 39, 7, 4 },
                    { 40, 8, 4 },
                    { 41, 9, 4 },
                    { 42, 10, 4 },
                    { 43, 11, 4 },
                    { 44, 12, 4 },
                    { 45, 13, 4 },
                    { 46, 14, 4 },
                    { 47, 15, 4 },
                    { 48, 19, 4 },
                    { 49, 20, 4 },
                    { 50, 21, 4 },
                    { 51, 22, 4 },
                    { 52, 23, 4 },
                    { 55, 25, 3 },
                    { 56, 21, 3 },
                    { 57, 22, 3 }
                });

            migrationBuilder.InsertData(
                table: "UsuarioRol",
                columns: new[] { "Id", "IdRol", "IdUsuario" },
                values: new object[] { 1, 1, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Contacto_IdEmpresa",
                table: "Contacto",
                column: "IdEmpresa");

            migrationBuilder.CreateIndex(
                name: "IX_Docentes_idUsuario",
                table: "Docentes",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_IdEstudiante_TipoDocumento",
                table: "Documentos",
                columns: new[] { "IdEstudiante", "TipoDocumento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_TipoDocumento",
                table: "Documentos",
                column: "TipoDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_Entregables_IdEstadoEntregable",
                table: "Entregables",
                column: "IdEstadoEntregable");

            migrationBuilder.CreateIndex(
                name: "IX_Entregables_IdEstudianteAutor",
                table: "Entregables",
                column: "IdEstudianteAutor");

            migrationBuilder.CreateIndex(
                name: "IX_Entregables_IdProyecto_IdTipoEntregable",
                table: "Entregables",
                columns: new[] { "IdProyecto", "IdTipoEntregable" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entregables_IdTipoEntregable",
                table: "Entregables",
                column: "IdTipoEntregable");

            migrationBuilder.CreateIndex(
                name: "IX_EntregableVersion_IdEntregable_NumeroVersion",
                table: "EntregableVersion",
                columns: new[] { "IdEntregable", "NumeroVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregableVersion_IdEstudianteSubio",
                table: "EntregableVersion",
                column: "IdEstudianteSubio");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoEntregable_Clave",
                table: "EstadoEntregable",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_idcarrera",
                table: "Estudiantes",
                column: "idcarrera");

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_idContactoEmergencia",
                table: "Estudiantes",
                column: "idContactoEmergencia");

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_idDependenciaMedica",
                table: "Estudiantes",
                column: "idDependenciaMedica");

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_idProyecto",
                table: "Estudiantes",
                column: "idProyecto");

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_idUsuario",
                table: "Estudiantes",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionProyecto_IdEstudianteCreador",
                table: "InvitacionProyecto",
                column: "IdEstudianteCreador");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionProyecto_IdEstudianteInvitado",
                table: "InvitacionProyecto",
                column: "IdEstudianteInvitado");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionProyecto_IdProyecto_IdEstudianteInvitado",
                table: "InvitacionProyecto",
                columns: new[] { "IdProyecto", "IdEstudianteInvitado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosMembrentados_PeriodoAcademicoId",
                table: "PeriodosMembrentados",
                column: "PeriodoAcademicoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoDocente_idDocente",
                table: "ProyectoDocente",
                column: "idDocente");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoDocente_idProyecto",
                table: "ProyectoDocente",
                column: "idProyecto");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoDocente_idProyecto_IdTipoRelacion",
                table: "ProyectoDocente",
                columns: new[] { "idProyecto", "IdTipoRelacion" });

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoDocente_idProyecto_IdTipoRelacion_idDocente",
                table: "ProyectoDocente",
                columns: new[] { "idProyecto", "IdTipoRelacion", "idDocente" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoDocente_IdTipoRelacion",
                table: "ProyectoDocente",
                column: "IdTipoRelacion");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoDocumentos_IdProyecto",
                table: "ProyectoDocumentos",
                column: "IdProyecto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_IdEmpresa",
                table: "Proyectos",
                column: "IdEmpresa");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_idEspecializcion",
                table: "Proyectos",
                column: "idEspecializcion");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_idEstado",
                table: "Proyectos",
                column: "idEstado");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_IdEstudianteCreador",
                table: "Proyectos",
                column: "IdEstudianteCreador");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_idModalidad",
                table: "Proyectos",
                column: "idModalidad");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_IdPeriodoAcademico",
                table: "Proyectos",
                column: "IdPeriodoAcademico");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionEntregable_IdDocenteRevisor",
                table: "RevisionEntregable",
                column: "IdDocenteRevisor");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionEntregable_IdEntregableVersion_NumeroRevision",
                table: "RevisionEntregable",
                columns: new[] { "IdEntregableVersion", "NumeroRevision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermiso_idPermiso",
                table: "RolPermiso",
                column: "idPermiso");

            migrationBuilder.CreateIndex(
                name: "IX_RolPermiso_idRol",
                table: "RolPermiso",
                column: "idRol");

            migrationBuilder.CreateIndex(
                name: "IX_SepomexColonias_Cp",
                table: "SepomexColonias",
                column: "Cp");

            migrationBuilder.CreateIndex(
                name: "IX_SepomexColonias_EstadoId_MunicipioId",
                table: "SepomexColonias",
                columns: new[] { "EstadoId", "MunicipioId" });

            migrationBuilder.CreateIndex(
                name: "IX_SepomexMunicipios_EstadoId",
                table: "SepomexMunicipios",
                column: "EstadoId");

            migrationBuilder.CreateIndex(
                name: "IX_TipoRelacionDocenteProyecto_Clave",
                table: "TipoRelacionDocenteProyecto",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRol_IdRol",
                table: "UsuarioRol",
                column: "IdRol");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRol_IdUsuario",
                table: "UsuarioRol",
                column: "IdUsuario");

            migrationBuilder.AddForeignKey(
                name: "FK_Documentos_Estudiantes_IdEstudiante",
                table: "Documentos",
                column: "IdEstudiante",
                principalTable: "Estudiantes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Entregables_Estudiantes_IdEstudianteAutor",
                table: "Entregables",
                column: "IdEstudianteAutor",
                principalTable: "Estudiantes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Entregables_Proyectos_IdProyecto",
                table: "Entregables",
                column: "IdProyecto",
                principalTable: "Proyectos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EntregableVersion_Estudiantes_IdEstudianteSubio",
                table: "EntregableVersion",
                column: "IdEstudianteSubio",
                principalTable: "Estudiantes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Estudiantes_Proyectos_idProyecto",
                table: "Estudiantes",
                column: "idProyecto",
                principalTable: "Proyectos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_Empresas_IdEmpresa",
                table: "Proyectos");

            migrationBuilder.DropForeignKey(
                name: "FK_Estudiantes_Usuarios_idUsuario",
                table: "Estudiantes");

            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_Estudiantes_IdEstudianteCreador",
                table: "Proyectos");

            migrationBuilder.DropTable(
                name: "Contacto");

            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "InvitacionProyecto");

            migrationBuilder.DropTable(
                name: "PeriodosMembrentados");

            migrationBuilder.DropTable(
                name: "ProyectoDocente");

            migrationBuilder.DropTable(
                name: "ProyectoDocumentos");

            migrationBuilder.DropTable(
                name: "RevisionEntregable");

            migrationBuilder.DropTable(
                name: "RolPermiso");

            migrationBuilder.DropTable(
                name: "SepomexColonias");

            migrationBuilder.DropTable(
                name: "SepomexEstados");

            migrationBuilder.DropTable(
                name: "SepomexMunicipios");

            migrationBuilder.DropTable(
                name: "UsuarioRol");

            migrationBuilder.DropTable(
                name: "TipoDocumento");

            migrationBuilder.DropTable(
                name: "TipoRelacionDocenteProyecto");

            migrationBuilder.DropTable(
                name: "Docentes");

            migrationBuilder.DropTable(
                name: "EntregableVersion");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Rol");

            migrationBuilder.DropTable(
                name: "Entregables");

            migrationBuilder.DropTable(
                name: "EstadoEntregable");

            migrationBuilder.DropTable(
                name: "TipoEntregable");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Estudiantes");

            migrationBuilder.DropTable(
                name: "Carreras");

            migrationBuilder.DropTable(
                name: "Contactoemergencia");

            migrationBuilder.DropTable(
                name: "DependenciaMedica");

            migrationBuilder.DropTable(
                name: "Proyectos");

            migrationBuilder.DropTable(
                name: "Especializacion");

            migrationBuilder.DropTable(
                name: "Estado");

            migrationBuilder.DropTable(
                name: "Modalidad");

            migrationBuilder.DropTable(
                name: "PeriodosAcademicos");
        }
    }
}
