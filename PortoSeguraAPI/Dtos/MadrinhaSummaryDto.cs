

namespace PortoSeguraAPI.Dtos;

public class MadrinhaSummaryDto
{
    public int Id { get; set; }
    public decimal PrecoDiaria { get; set; }
    public string? FotoPerfilUrl { get; set; }
    public string Motivacao { get; set; } = string.Empty;

    // Usuária resumida
    public int UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int QtdSolicitacoes { get; set; }
    public double MediaAvaliacao { get; set; }
    public ICollection<AvaliacaoSummaryDto> Avaliacoes { get; set; } = new List<AvaliacaoSummaryDto>();
    // Servicos descrições
    public List<string> Servicos { get; set; } = new List<string>();
    public System.Collections.Generic.Dictionary<string, double>? MediaPorServico { get; set; }
    public System.Collections.Generic.Dictionary<string, int>? QtdPorServico { get; set; }
}
