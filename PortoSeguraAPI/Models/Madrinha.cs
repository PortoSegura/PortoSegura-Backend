using PortoSeguraAPI.Models;

public class Madrinha
{
    public int Id { get; private set; }
    public required int UsuarioID { get; set; }
    public required decimal PrecoDiaria { get; set; }
    public bool VerificadoIdentidade { get; set; } = false;
    public bool VerificadoResidencia { get; set; } = false;
    public bool TrilhaCursoCompleto { get; set; } = false;
    public required string Motivacao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public string? DocumentoIdentidadeUrl { get; set; }
    public string? ComprovanteResidenciaUrl { get; set; }
    
    // Propriedades de navegação
    public virtual Usuaria Usuario { get; set; } = null!;
    public virtual ICollection<Servico> Servicos { get; set; } = null!;
    public virtual ICollection<Solicitacao> Solicitacoes { get; set; } = null!;
    public virtual ICollection<Avaliacao> Avaliacoes { get; set; } = null!;
}