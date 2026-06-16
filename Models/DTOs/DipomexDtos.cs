using System.Text.Json.Serialization;

namespace WebApiVinculacionProyectosV2.Dtos
{
    public class CodigoPostalResponseDto
    {
        [JsonPropertyName("error")] public bool Error { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = "";
        [JsonPropertyName("codigo_postal")] public CodigoPostalDto CodigoPostal { get; set; } = new();
    }

    public class CodigoPostalDto
    {
        [JsonPropertyName("estado_id")] public string EstadoId { get; set; } = "";
        [JsonPropertyName("municipio_id")] public string MunicipioId { get; set; } = "";
        [JsonPropertyName("estado")] public string Estado { get; set; } = "";
        [JsonPropertyName("estado_abreviatura")] public string EstadoAbreviatura { get; set; } = "";
        [JsonPropertyName("municipio")] public string Municipio { get; set; } = "";
        [JsonPropertyName("centro_reparto")] public string CentroReparto { get; set; } = "";
        [JsonPropertyName("codigo_postal")] public string CodigoPostal { get; set; } = "";
        [JsonPropertyName("colonias")] public List<CpColoniaDto> Colonias { get; set; } = new();
    }

    public class CpColoniaDto
    {
        [JsonPropertyName("colonia_id")] public string ColoniaId { get; set; } = "";
        [JsonPropertyName("colonia")] public string Colonia { get; set; } = "";
    }

    public class EstadoItemDto
    {
        [JsonPropertyName("ESTADO_ID")] public string ESTADO_ID { get; set; } = "";
        [JsonPropertyName("ESTADO")] public string ESTADO { get; set; } = "";
        [JsonPropertyName("EDO1")] public string EDO1 { get; set; } = "";
        [JsonPropertyName("RANGO1")] public string RANGO1 { get; set; } = "";
        [JsonPropertyName("RANGO2")] public string RANGO2 { get; set; } = "";
    }

    public class EstadosResponseDto
    {
        [JsonPropertyName("error")] public bool Error { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = "";
        [JsonPropertyName("estados")] public List<EstadoItemDto> Estados { get; set; } = new();
    }

    public class EstadoResponseDto
    {
        [JsonPropertyName("error")] public bool Error { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = "";
        [JsonPropertyName("estado")] public List<EstadoItemDto> Estado { get; set; } = new();
    }

    public class MunicipioItemDto
    {
        [JsonPropertyName("ESTADO_ID")] public string ESTADO_ID { get; set; } = "";
        [JsonPropertyName("MUNICIPIO_ID")] public string MUNICIPIO_ID { get; set; } = "";
        [JsonPropertyName("MUNICIPIO")] public string MUNICIPIO { get; set; } = "";
        [JsonPropertyName("RANGO1")] public string RANGO1 { get; set; } = "";
        [JsonPropertyName("RANGO2")] public string RANGO2 { get; set; } = "";
    }

    public class MunicipiosResponseDto
    {
        [JsonPropertyName("error")] public bool Error { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = "";
        [JsonPropertyName("municipios")] public List<MunicipioItemDto> Municipios { get; set; } = new();
    }

    public class ColoniaItemDto
    {
        [JsonPropertyName("COLONIA_ID")] public string COLONIA_ID { get; set; } = "";
        [JsonPropertyName("ESTADO_ID")] public string ESTADO_ID { get; set; } = "";
        [JsonPropertyName("MUNICIPIO_ID")] public string MUNICIPIO_ID { get; set; } = "";
        [JsonPropertyName("COLONIA")] public string COLONIA { get; set; } = "";
        [JsonPropertyName("CP")] public string CP { get; set; } = "";
        [JsonPropertyName("CR")] public string CR { get; set; } = "";
        [JsonPropertyName("FECHA_ACT")] public string FECHA_ACT { get; set; } = "";
    }

    public class ColoniasResponseDto
    {
        [JsonPropertyName("error")] public bool Error { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = "";
        [JsonPropertyName("colonias")] public List<ColoniaItemDto> Colonias { get; set; } = new();
    }
}