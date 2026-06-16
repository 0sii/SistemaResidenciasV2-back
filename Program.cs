using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WebApiVinculacionProyectosV2.Custom;
using WebApiVinculacionProyectosV2.Models;
using WebApiVinculacionProyectosV2.Security;
using WebApiVinculacionProyectosV2.Services;
using WebApiVinculacionProyectosV2.Servicios;
using WebApiVinculacionProyectosV2.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase);

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });
    c.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Format = "duration" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega: Bearer {tu_token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// DB
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = ServerVersion.AutoDetect(cs);

builder.Services.AddDbContext<ResidenciasDbContext>(options =>
{
    options.UseMySql(cs, serverVersion);
    options.AddInterceptors(new Utf8mb4ConnectionInterceptor());
});

builder.Services.AddSingleton<Utilidades>();

// JWT
builder.Services.AddAuthentication(config =>
{
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(config =>
{
    config.RequireHttpsMetadata = false;
    config.SaveToken = true;
    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:key"]!)
        )
    };
});

// CORS (tu front)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition"); // permite leer el nombre del PDF en Angular
    });
});

builder.Services.AddMemoryCache();

// HttpClient Dipomex externo (opcional / no rompe si ya no tienes config)
var dipomexSection = builder.Configuration.GetSection("Dipomex");
var dipomexBaseUrl = dipomexSection["BaseUrl"];
var dipomexApiKey = dipomexSection["ApiKey"];

if (!string.IsNullOrWhiteSpace(dipomexBaseUrl))
{
    builder.Services.AddHttpClient("DipomexClient", client =>
    {
        client.BaseAddress = new Uri(dipomexBaseUrl);

        if (!string.IsNullOrWhiteSpace(dipomexApiKey))
            client.DefaultRequestHeaders.Add("APIKEY", dipomexApiKey);
    });
}

// Authorization
builder.Services.AddAuthorization(options =>
{
    // Política para el POST /api/dipomex/import.
    // En DEV lo dejamos abierto para poder importar desde Swagger sin token.
    // En PROD sí exigimos rol Admin.
    options.AddPolicy("AdminOnly", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.RequireAssertion(_ => true); // permite anónimo en Development
        }
        else
        {
            policy.RequireAuthenticatedUser().RequireRole("Admin");
        }
    });
});

// Tu autorización por permisos (se mantiene)
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// Servicios
builder.Services.AddScoped<IConstanciasPdfService, ConstanciasPdfService>();
builder.Services.AddScoped<IServicioEmail, ServicioEmail>();
builder.Services.AddScoped<INotificacionesService, NotificacionesService>();

// Límites de subida (para el TXT grande de SEPOMEX)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 350L * 1024 * 1024; // 350MB
});

builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 350L * 1024 * 1024; // 350MB
});

// Importer SEPOMEX
builder.Services.AddScoped<SepomexImporter>();



var app = builder.Build();

// Migraciones + (opcional) import al arranque (YA NO SE CAE si falta el archivo)
// Migraciones + import SEPOMEX solo si la BD está vacía
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ResidenciasDbContext>();
    db.Database.Migrate();

    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var importer = scope.ServiceProvider.GetRequiredService<SepomexImporter>();

    var importOnStartup = cfg.GetValue<bool>("Sepomex:ImportOnStartup");
    var path = cfg["Sepomex:DataFilePath"];

    if (importOnStartup && !string.IsNullOrWhiteSpace(path))
    {
        var absPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(app.Environment.ContentRootPath, path);

        if (File.Exists(absPath))
        {
            try
            {
                // Verifica si ya hay datos cargados
                var sepomexYaTieneDatos = await db.SepomexColonias.AnyAsync();

                if (sepomexYaTieneDatos)
                {
                    app.Logger.LogInformation(
                        "SEPOMEX: ya existen datos en la base de datos. Se omite la importación automática."
                    );
                }
                else
                {
                    var result = await importer.ImportFromUploadOrPathAsync(
                        file: null,
                        replace: false,
                        validateOnly: false,
                        ct: default
                    );

                    app.Logger.LogInformation(
                        "SEPOMEX import -> Ok:{Ok}, Estados:{Estados}, Municipios:{Municipios}, Colonias:{Colonias}, Msg:{Msg}",
                        result.Ok, result.Estados, result.Municipios, result.Colonias, result.Message
                    );

                    if (result.Warnings.Any())
                        app.Logger.LogWarning("SEPOMEX warnings: {Warn}", string.Join(" | ", result.Warnings));

                    if (!result.Ok)
                        app.Logger.LogError("SEPOMEX errores: {Err}", string.Join(" | ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "SEPOMEX: error importando al arranque. La app seguirá arriba.");
            }
        }
        else
        {
            app.Logger.LogWarning("SEPOMEX: no se encontró el archivo en {Path}. La app seguirá arriba sin catálogo.", absPath);
        }
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.Use((ctx, next) =>
    {
        ctx.Response.Headers["Content-Security-Policy"] =
            "connect-src 'self' http://localhost:* ws://localhost:* http://127.0.0.1:* ws://127.0.0.1:*";
        return next();
    });
}

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }