using Microsoft.AspNetCore.Identity;
using PortoSeguraAPI.Enums;

namespace PortoSeguraAPI.Models;

public class Usuaria: IdentityUser<int>
{
    public required string Nome { get; set; }
    public int SaldoCreditos { get; set; } = 0;
    public required string Telefone { get; set; }
    public required UserStatus Status { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public required string Bio { get; set; }
    public required string Estado { get; set; }
    public required string Cidade { get; set; }
    public required string VideoVerificacao { get; set; } 
    public string urlLinkedin { get; set; } = string.Empty;
    public string urlInstagram { get; set; } = string.Empty;
    public string urlFacebook { get; set; } = string.Empty;
    public string? FotoPerfilUrl { get; set; }

    // Propriedades de navegação
    public virtual ICollection<Solicitacao> Solicitacoes { get; set; } = null!;
    public virtual ICollection<Documento> Documentos { get; set; } = null!;
    public virtual ICollection<Avaliacao> Avaliacoes { get; set; } = null!;

}