using PortoSeguraAPI.Models;

public class Solicitacao
{
    public int Id { get; private set; }
    public required int UsuariaId { get; set; }
    public required int MadrinhaId { get; set; }
    public required string Descricao { get; set; }
    public required string Destino { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public required DateTime DataInicio { get; set; }
    public required DateTime DataFim { get; set; }

    public required int QtdDiarias { get; set; }
    public required string Status { get; set; }
    public required decimal Valor { get; set; }

    // Propriedades de navegação
    public virtual Usuaria Usuaria { get; set; } = null!;
    public virtual Madrinha Madrinha { get; set; } = null!;
    public virtual ICollection<Avaliacao> Avaliacoes { get; set; } = null!;

}