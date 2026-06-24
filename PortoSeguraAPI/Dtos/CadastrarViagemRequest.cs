using System;

namespace PortoSeguraAPI.Dtos;

public class CadastrarViagemRequest
{
    public required string Destino { get; set; }
    public required DateTime DataInicio { get; set; }
    public required DateTime DataFim { get; set; }
}
