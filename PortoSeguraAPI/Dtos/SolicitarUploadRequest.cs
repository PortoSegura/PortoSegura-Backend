namespace PortoSeguraAPI.Dtos;
public class SolicitarUploadRequest
{
    public required string TipoDocumento { get; set; } // Ex: "Identidade", "ComprovanteResidencia"
    public required string TipoMime { get; set; }      // Ex: "application/pdf", "image/jpeg"
    public required long TamanhoEmBytes { get; set; }  // Ex: 2048500 (2.04 MB)
}