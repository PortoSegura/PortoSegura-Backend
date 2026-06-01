namespace PortoSeguraAPI.Models;

public class Servico
{
    public int Id { get; private set; }
    public required int MadrinhaId { get; set; }
    public required string Descricao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}