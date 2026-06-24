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
    
    // Governança, times e SLA
    public int? TimeLocalId { get; set; }
    public virtual TimeLocal? TimeLocal { get; set; }
    public bool AtivaFilaAlocacao { get; set; } = true;
    public int SlaMinutos { get; set; } = 15;
    public bool Disponivel { get; set; } = true;
    public int CargaAtendimentosAtivos { get; set; } = 0;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public string? DocumentoIdentidadeUrl { get; set; }
    public string? ComprovanteResidenciaUrl { get; set; }
    
    // Propriedades de navegação
    public virtual Usuaria Usuario { get; set; } = null!;
    public virtual ICollection<Servico> Servicos { get; set; } = null!;
    public virtual ICollection<Solicitacao> Solicitacoes { get; set; } = null!;
    public virtual ICollection<Avaliacao> Avaliacoes { get; set; } = null!;
}