namespace PortoSeguraAPI.Dtos;

public class UploadDocumentosMadrinhaRequest
{
    public IFormFile? FotoPerfil { get; set; }
    public IFormFile? DocumentoIdentidade { get; set; }
    public IFormFile? ComprovanteResidencia { get; set; }
}