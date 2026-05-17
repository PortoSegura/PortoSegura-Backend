using Microsoft.AspNetCore.Identity;
using PortoSeguraAPI.Enums;

namespace PortoSeguraAPI.Models;

public class Usuario: IdentityUser<int>
{
    public required string Nome { get; set; }
    public required string Telefone { get; set; }
    public required UserStatus Status { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}