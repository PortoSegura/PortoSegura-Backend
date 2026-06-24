namespace PortoSeguraAPI.Models;

public class Avaliacao
{
    public int Id { get; set; }
    public int? SolicitacaoId { get; set; }
    public int? SessaoChatId { get; set; }
    public string? ServicoTipo { get; set; }
    public int MadrinhaId { get; set; }
    public int UsuariaId { get; set; }
    public int Nota { get; set; }
    public string? Comentario { get; set; } = null!;
    public bool IsAvaliacaoMadrinha { get; set; } = false;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public virtual Solicitacao? Solicitacao { get; set; }
    public virtual SessaoChat? SessaoChat { get; set; }
    public virtual Madrinha Madrinha { get; set; } = null!;
    public virtual Usuaria Usuaria { get; set; } = null!;
}