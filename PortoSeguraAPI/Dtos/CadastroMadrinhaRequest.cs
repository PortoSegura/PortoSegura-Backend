namespace PortoSeguraAPI.Dtos;

public class CadastroMadrinhaRequest
{
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public required string Telefone { get; set; }
    public required string Senha { get; set; }
    public required string Bio { get; set; }
    public required decimal PrecoDiaria { get; set; }
    public required string Motivacao { get; set; }
    public required string Estado { get; set; }
    public required string Cidade { get; set; }
    public required string VideoVerificacao { get; set; }
    public string? Linkedin { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public string? FotoPerfilUrl { get; set; }
}