public class SustituirDocenteDto
{
    /// <summary>"ASESOR_INTERNO" | "REVISOR_RESIDENCIA"</summary>
    public string TipoClave      { get; set; } = string.Empty;
    public int    IdDocenteSale  { get; set; }
    public int    IdDocenteEntra { get; set; }
    /// <summary>Motivo de la sustitución (opcional, para auditoría futura).</summary>
    public string? Motivo        { get; set; }
}
