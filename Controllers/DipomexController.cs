// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using System.Threading.Tasks;

// namespace WebApiVinculacionProyectosV2.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class DipomexController : ControllerBase
//     {
//             private readonly HttpClient _http;

//             public DipomexController(IHttpClientFactory httpClientFactory)
//             {
//                 _http = httpClientFactory.CreateClient("DipomexClient");
//             }

//             // GET api/dipomex/codigo-postal/09000
//             [HttpGet("codigo-postal/{cp}")]
//             public async Task<IActionResult> GetCodigoPostal(string cp)
//             {
//                 var response = await _http.GetAsync($"codigo_postal?cp={cp}");

//                 var body = await response.Content.ReadAsStringAsync();

//                 if (!response.IsSuccessStatusCode)
//                     return StatusCode((int)response.StatusCode, body);

//                 return Content(body, "application/json");
//             }

//             // GET api/dipomex/estado/09
//             [HttpGet("estado/{id}")]
//             public async Task<IActionResult> GetEstado(string id)
//             {
//                 var response = await _http.GetAsync($"estado?id={id}");

//                 var body = await response.Content.ReadAsStringAsync();

//                 if (!response.IsSuccessStatusCode)
//                     return StatusCode((int)response.StatusCode, body);

//                 return Content(body, "application/json");
//             }

//             // GET api/dipomex/estados
//             [HttpGet("estados")]
//             public async Task<IActionResult> GetEstados()
//             {
//                 var response = await _http.GetAsync("estados");

//                 var body = await response.Content.ReadAsStringAsync();

//                 if (!response.IsSuccessStatusCode)
//                     return StatusCode((int)response.StatusCode, body);

//                 return Content(body, "application/json");
//             }

//             // GET api/dipomex/municipios/09
//             [HttpGet("municipios/{estadoId}")]
//             public async Task<IActionResult> GetMunicipios(string estadoId)
//             {
//                 var response = await _http.GetAsync($"municipios?id_estado={estadoId}");

//                 var body = await response.Content.ReadAsStringAsync();

//                 if (!response.IsSuccessStatusCode)
//                     return StatusCode((int)response.StatusCode, body);

//                 return Content(body, "application/json");
//             }

//             // GET api/dipomex/colonias/09/014
//             [HttpGet("colonias/{estadoId}/{municipioId}")]
//             public async Task<IActionResult> GetColonias(string estadoId, string municipioId)
//             {
//                 var response = await _http.GetAsync($"colonias?id_estado={estadoId}&id_mun={municipioId}");

//                 var body = await response.Content.ReadAsStringAsync();

//                 if (!response.IsSuccessStatusCode)
//                     return StatusCode((int)response.StatusCode, body);

//                 return Content(body, "application/json");
//             }
//         }
//     }


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Dtos;
using WebApiVinculacionProyectosV2.Models;
using WebApiVinculacionProyectosV2.Models.Requests;
using WebApiVinculacionProyectosV2.Services;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DipomexController : ControllerBase
    {
        private readonly SepomexImporter _importer;
        private readonly ResidenciasDbContext _db;

        public DipomexController(ResidenciasDbContext db, SepomexImporter importer)
        {
            _importer = importer;
          _db = db;  
        } 

        [HttpGet("estados")]
        public async Task<IActionResult> GetEstados()
        {
            var estados = await _db.SepomexEstados.AsNoTracking()
                .OrderBy(e => e.EstadoId)
                .Select(e => new EstadoItemDto
                {
                    ESTADO_ID = e.EstadoId,
                    ESTADO = e.Nombre,
                    EDO1 = e.Abreviatura ?? "",
                    RANGO1 = e.Rango1,
                    RANGO2 = e.Rango2
                })
                .ToListAsync();

            return Ok(new EstadosResponseDto
            {
                Error = false,
                Message = $"Estados cargados: {estados.Count}",
                Estados = estados
            });
        }

        [HttpGet("estado/{id}")]
        public async Task<IActionResult> GetEstado(string id)
        {
            id = (id ?? "").Trim().PadLeft(2, '0');

            var e = await _db.SepomexEstados.AsNoTracking().FirstOrDefaultAsync(x => x.EstadoId == id);

            var list = new List<EstadoItemDto>();
            if (e != null)
            {
                list.Add(new EstadoItemDto
                {
                    ESTADO_ID = e.EstadoId,
                    ESTADO = e.Nombre,
                    EDO1 = e.Abreviatura ?? "",
                    RANGO1 = e.Rango1,
                    RANGO2 = e.Rango2
                });
            }

            return Ok(new EstadoResponseDto
            {
                Error = false,
                Message = $"Estado cargado: {list.Count}",
                Estado = list
            });
        }

        [HttpGet("municipios/{estadoId}")]
        public async Task<IActionResult> GetMunicipios(string estadoId)
        {
            estadoId = (estadoId ?? "").Trim().PadLeft(2, '0');

            var municipios = await _db.SepomexMunicipios.AsNoTracking()
                .Where(m => m.EstadoId == estadoId)
                .OrderBy(m => m.MunicipioId)
                .Select(m => new MunicipioItemDto
                {
                    ESTADO_ID = m.EstadoId,
                    MUNICIPIO_ID = m.MunicipioId,
                    MUNICIPIO = m.Nombre,
                    RANGO1 = m.Rango1,
                    RANGO2 = m.Rango2
                })
                .ToListAsync();

            return Ok(new MunicipiosResponseDto
            {
                Error = false,
                Message = $"Municipios cargados: {municipios.Count}",
                Municipios = municipios
            });
        }

        [HttpGet("colonias/{estadoId}/{municipioId}")]
        public async Task<IActionResult> GetColonias(string estadoId, string municipioId)
        {
            estadoId = (estadoId ?? "").Trim().PadLeft(2, '0');
            municipioId = (municipioId ?? "").Trim().PadLeft(3, '0');

            var colonias = await _db.SepomexColonias.AsNoTracking()
                .Where(c => c.EstadoId == estadoId && c.MunicipioId == municipioId)
                .OrderBy(c => c.Nombre)
                .Select(c => new ColoniaItemDto
                {
                    COLONIA_ID = c.ColoniaId,
                    ESTADO_ID = c.EstadoId,
                    MUNICIPIO_ID = c.MunicipioId,
                    COLONIA = c.Nombre,
                    CP = c.Cp,
                    CR = c.Cr ?? "",
                    FECHA_ACT = c.FechaAct.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToListAsync();

            return Ok(new ColoniasResponseDto
            {
                Error = false,
                Message = $"Colonias cargadas: {colonias.Count}",
                Colonias = colonias
            });
        }

        [HttpGet("codigo-postal/{cp}")]
        public async Task<IActionResult> GetCodigoPostal(string cp)
        {
            cp = (cp ?? "").Trim().PadLeft(5, '0');

            var rows = await _db.SepomexColonias.AsNoTracking()
                .Where(c => c.Cp == cp)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            if (rows.Count == 0)
            {
                // OJO: tu TS interface espera que exista "codigo_postal", así que lo mando vacío.
                return Ok(new CodigoPostalResponseDto
                {
                    Error = true,
                    Message = "Código Postal no encontrado.",
                    CodigoPostal = new CodigoPostalDto
                    {
                        EstadoId = "",
                        MunicipioId = "",
                        Estado = "",
                        EstadoAbreviatura = "",
                        Municipio = "",
                        CentroReparto = "",
                        CodigoPostal = cp,
                        Colonias = new()
                    }
                });
            }

            var first = rows[0];

            var edo = await _db.SepomexEstados.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EstadoId == first.EstadoId);

            var mun = await _db.SepomexMunicipios.AsNoTracking()
                .FirstOrDefaultAsync(m => m.EstadoId == first.EstadoId && m.MunicipioId == first.MunicipioId);

            return Ok(new CodigoPostalResponseDto
            {
                Error = false,
                Message = "Procesamiento correcto.",
                CodigoPostal = new CodigoPostalDto
                {
                    EstadoId = first.EstadoId,
                    MunicipioId = first.MunicipioId,
                    Estado = edo?.Nombre ?? "",
                    EstadoAbreviatura = edo?.Abreviatura ?? "",
                    Municipio = mun?.Nombre ?? "",
                    CentroReparto = first.Cr ?? "",
                    CodigoPostal = cp,
                    Colonias = rows.Select(r => new CpColoniaDto
                    {
                        ColoniaId = r.ColoniaId,
                        Colonia = r.Nombre
                    }).ToList()
                }
            });
        }

        // POST /api/dipomex/import
    // multipart/form-data: file=<cpdescarga.txt>


[HttpPost("import")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> Import(
    [FromForm] SepomexImportForm form,
    [FromQuery] bool replace = true,
    [FromQuery] bool validateOnly = false,
    CancellationToken ct = default)
{
    var result = await _importer.ImportFromUploadOrPathAsync(form.File, replace, validateOnly, ct);

    if (!result.Ok)
        return BadRequest(result);

    return Ok(result);
}
    }
}