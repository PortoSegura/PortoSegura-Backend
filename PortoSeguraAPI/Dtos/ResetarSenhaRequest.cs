namespace PortoSeguraAPI.Dtos;

public class ResetarSenhaRequest
{
    public string Email { get; set; } = null!;
    public string NovaSenha { get; set; } = null!;
    public string Token { get; set; } = null!;
}