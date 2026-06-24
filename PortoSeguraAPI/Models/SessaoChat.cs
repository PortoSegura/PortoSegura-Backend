using System;

namespace PortoSeguraAPI.Models;

public class SessaoChat
{
    public int Id { get; set; }
    public int UsuariaId { get; set; }
    public int? MadrinhaId { get; set; }
    public int? TimeLocalId { get; set; }
    
    public required string ServicoTipo { get; set; } // "Dicas Locais", "Ligação/Suporte", etc.
    public required string Status { get; set; } // "Pendente", "Ativa", "Finalizada", "Expirada", "Redistribuida"
    public DateTime DataInicio { get; set; } = DateTime.UtcNow;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? TempoLimite { get; set; } // Fim da sessão de dicas (ex: +30min)
    public DateTime SlaLimite { get; set; } // DataCriacao + 15 min
    public bool Respondida { get; set; } = false; // Fica true quando a madrinha envia mensagem
    public int CreditosConsumidos { get; set; }

    // Campos adicionais de especificidade de serviços
    public DateTime? HorarioDesembarque { get; set; }
    public string? Aeroporto { get; set; }
    public string? LocaisVisitados { get; set; }
    public int? QuantidadeHoras { get; set; }

    public DateTime? AcompanhamentoDataInicio { get; set; }
    public DateTime? AcompanhamentoDataFim { get; set; }
    public string? AcompanhamentoHoraInicio { get; set; }
    public string? AcompanhamentoHoraFim { get; set; }
    public bool Avaliada { get; set; } = false;

    // Propriedades de navegação
    public virtual Usuaria Usuaria { get; set; } = null!;
    public virtual Madrinha? Madrinha { get; set; } = null!;
    public virtual TimeLocal? TimeLocal { get; set; }
}
