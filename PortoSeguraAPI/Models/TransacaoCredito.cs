using System;

namespace PortoSeguraAPI.Models;

public class TransacaoCredito
{
    public int Id { get; set; }
    public int UsuariaId { get; set; }
    public int Quantidade { get; set; } // positivo para compra/estorno, negativo para consumo
    public required string Tipo { get; set; } // "Compra", "Consumo", "Estorno"
    public required string Descricao { get; set; }
    public decimal? PrecoPago { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Propriedade de navegação
    public virtual Usuaria Usuaria { get; set; } = null!;
}
