namespace PortoSeguraAPI.Dtos;

public class AvaliacaoSummaryDto
{
    public int Id { get; set; }
    public int Nota { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public string NomeUsuaria { get; set; } = string.Empty;
    public string? ServicoTipo { get; set; }
}