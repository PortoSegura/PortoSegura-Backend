

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

    // Servicos descrições
    public List<string> Servicos { get; set; } = new List<string>();
}
