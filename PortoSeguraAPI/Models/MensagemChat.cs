using System;

namespace PortoSeguraAPI.Models;

public class MensagemChat
{
    public int Id { get; set; }
    public int SessaoChatId { get; set; }
    public int RemetenteId { get; set; }
    public required string Texto { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Propriedade de navegação
    public virtual SessaoChat SessaoChat { get; set; } = null!;
}
