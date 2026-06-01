public class Documento
{
    public int UserId { get; set; }
    public required string Tipo { get; set; }
    public required string Url { get; set; }
    public required string NomeArquivo { get; set; }
    public string StatusUpload { get; set; } = "Pendente";
    public DateTime DataUpload { get; set; } = DateTime.UtcNow;
}