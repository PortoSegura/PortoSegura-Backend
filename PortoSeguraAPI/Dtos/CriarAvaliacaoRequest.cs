namespace PortoSeguraAPI.Dtos;

public class CriarAvaliacaoRequest
{
    public int? SolicitacaoId { get; set; }
    public int? SessaoChatId { get; set; }
    public int MadrinhaId { get; set; }
    public int Nota { get; set; }
    public string? Comentario { get; set; } = null!;
}