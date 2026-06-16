using System.Collections.Generic;

namespace WebApiVinculacionProyectosV2.Dto
{
    public class DocumentoUploadResultDto
    {
        public int TotalRegistrosCreados { get; set; }
        public List<int> IdsDocumentosCreados { get; set; } = new();
    }
}
