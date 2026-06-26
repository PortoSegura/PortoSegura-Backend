using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortoSeguraAPI.Data;
using PortoSeguraAPI.Models;

namespace PortoSeguraAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<Usuaria> _userManager;
    private readonly BlobStorageService _blobStorageService;

    public ChatController(AppDbContext context, UserManager<Usuaria> userManager, BlobStorageService blobStorageService)
    {
        _context = context;
        _userManager = userManager;
        _blobStorageService = blobStorageService;
    }

    [HttpGet("sessoes")]
    public async Task<IActionResult> ObterSessoes()
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var roles = await _userManager.GetRolesAsync(usuario);
        bool isMadrinha = roles.Contains("Madrinha");

        IQueryable<SessaoChat> query = _context.Set<SessaoChat>()
            .Include(s => s.Usuaria)
            .Include(s => s.Madrinha)
                .ThenInclude(m => m!.Usuario);

        if (isMadrinha)
        {
            var madrinha = await _context.Set<Madrinha>().FirstOrDefaultAsync(m => m.UsuarioID == usuario.Id);
            if (madrinha == null)
                return NotFound(new { mensagem = "Cadastro de madrinha não encontrado." });

            query = query.Where(s => s.MadrinhaId == madrinha.Id);
        }
        else
        {
            query = query.Where(s => s.UsuariaId == usuario.Id);
        }

        var sessoes = await query
            .OrderByDescending(s => s.DataCriacao)
            .Select(s => new
            {
                s.Id,
                s.UsuariaId,
                s.MadrinhaId,
                s.ServicoTipo,
                s.Status,
                s.DataInicio,
                s.DataCriacao,
                s.TempoLimite,
                s.SlaLimite,
                s.Respondida,
                s.CreditosConsumidos,
                s.HorarioDesembarque,
                s.Aeroporto,
                s.LocaisVisitados,
                s.QuantidadeHoras,
                s.AcompanhamentoDataInicio,
                s.AcompanhamentoDataFim,
                s.AcompanhamentoHoraInicio,
                s.AcompanhamentoHoraFim,
                s.Avaliada,
                s.PontoEncontro,
                s.DuvidaInicial,
                ViajanteNome = s.Usuaria.Nome,
                ViajanteFotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(s.Usuaria.FotoPerfilUrl),
                ViajanteCidade = s.Usuaria.Cidade,
                ViajanteEstado = s.Usuaria.Estado,
                MadrinhaNome = s.Madrinha != null ? s.Madrinha.Usuario.Nome : "Aguardando Madrinha...",
                MadrinhaFotoPerfilUrl = s.Madrinha != null ? _blobStorageService.GerarUrlDeLeitura(s.Madrinha.Usuario.FotoPerfilUrl) : null,
                MadrinhaMediaAvaliacao = s.Madrinha != null ? (s.Madrinha.Avaliacoes.Any() ? s.Madrinha.Avaliacoes.Average(a => a.Nota) : 5.0) : 0.0,
                ViagemDestino = _context.Set<Solicitacao>()
                    .Where(sol => sol.UsuariaId == s.UsuariaId && (sol.Status == "Aberta" || sol.Status == "Aceita"))
                    .OrderByDescending(sol => sol.DataCriacao)
                    .Select(sol => sol.Destino)
                    .FirstOrDefault(),
                ViagemInicio = _context.Set<Solicitacao>()
                    .Where(sol => sol.UsuariaId == s.UsuariaId && (sol.Status == "Aberta" || sol.Status == "Aceita"))
                    .OrderByDescending(sol => sol.DataCriacao)
                    .Select(sol => (DateTime?)sol.DataInicio)
                    .FirstOrDefault(),
                ViagemFim = _context.Set<Solicitacao>()
                    .Where(sol => sol.UsuariaId == s.UsuariaId && (sol.Status == "Aberta" || sol.Status == "Aceita"))
                    .OrderByDescending(sol => sol.DataCriacao)
                    .Select(sol => (DateTime?)sol.DataFim)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(sessoes);
    }

    [HttpGet("sessoes/{id}/mensagens")]
    public async Task<IActionResult> ObterMensagens(int id)
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var sessao = await _context.Set<SessaoChat>().FindAsync(id);
        if (sessao == null)
            return NotFound(new { mensagem = "Sessão de chat não encontrada." });

        // Verifica permissão
        var roles = await _userManager.GetRolesAsync(usuario);
        bool isMadrinha = roles.Contains("Madrinha");

        if (isMadrinha)
        {
            var madrinha = await _context.Set<Madrinha>().FirstOrDefaultAsync(m => m.UsuarioID == usuario.Id);
            if (madrinha == null || (sessao.MadrinhaId.HasValue && sessao.MadrinhaId.Value != madrinha.Id))
                return Forbid();
        }
        else
        {
            if (sessao.UsuariaId != usuario.Id)
                return Forbid();
        }

        // Se a sessão de dicas estourou o tempo de 30 minutos, vamos fechar ela aqui se ainda estiver ativa
        if (sessao.Status == "Ativa" && sessao.TempoLimite.HasValue && DateTime.UtcNow > sessao.TempoLimite.Value)
        {
            sessao.Status = "Finalizada";
            _context.Update(sessao);
            await _context.SaveChangesAsync();
        }

        var mensagens = await _context.Set<MensagemChat>()
            .Where(m => m.SessaoChatId == id)
            .OrderBy(m => m.DataCriacao)
            .Select(m => new
            {
                m.Id,
                m.SessaoChatId,
                m.RemetenteId,
                m.Texto,
                m.DataCriacao
            })
            .ToListAsync();

        string? madrinhaNome = null;
        string? madrinhaFotoPerfilUrl = null;
        double madrinhaMediaAvaliacao = 0.0;

        if (sessao.MadrinhaId.HasValue)
        {
            var madrinha = await _context.Set<Madrinha>()
                .Include(m => m.Usuario)
                .Include(m => m.Avaliacoes)
                .FirstOrDefaultAsync(m => m.Id == sessao.MadrinhaId.Value);

            if (madrinha != null)
            {
                madrinhaNome = madrinha.Usuario.Nome;
                madrinhaFotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(madrinha.Usuario.FotoPerfilUrl);
                madrinhaMediaAvaliacao = madrinha.Avaliacoes.Any() ? madrinha.Avaliacoes.Average(a => a.Nota) : 5.0;
            }
        }

        return Ok(new
        {
            sessaoStatus = sessao.Status,
            tempoLimite = sessao.TempoLimite,
            slaLimite = sessao.SlaLimite,
            respondida = sessao.Respondida,
            madrinhaNome,
            madrinhaFotoPerfilUrl,
            madrinhaMediaAvaliacao,
            avaliada = sessao.Avaliada,
            madrinhaId = sessao.MadrinhaId,
            mensagens
        });
    }

    [HttpPost("sessoes/enviar-mensagem")]
    public async Task<IActionResult> EnviarMensagem([FromBody] EnviarMensagemRequest request)
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var sessao = await _context.Set<SessaoChat>().FindAsync(request.SessaoId);
        if (sessao == null)
            return NotFound(new { mensagem = "Sessão de chat não encontrada." });

        if (sessao.Status == "Finalizada" || sessao.Status == "Expirada")
            return BadRequest(new { mensagem = "Esta sessão de chat já está finalizada." });

        var roles = await _userManager.GetRolesAsync(usuario);
        bool isMadrinha = roles.Contains("Madrinha");

        if (isMadrinha)
        {
            var madrinha = await _context.Set<Madrinha>().FirstOrDefaultAsync(m => m.UsuarioID == usuario.Id);
            if (madrinha == null || sessao.MadrinhaId != madrinha.Id)
                return Forbid();

            // Madrinha enviou a mensagem -> Resposta efetuada, cancela o SLA pendente!
            if (!sessao.Respondida)
            {
                sessao.Respondida = true;
                if (sessao.Status == "Pendente")
                {
                    sessao.Status = "Ativa";
                }
                _context.Update(sessao);
            }
        }
        else
        {
            if (sessao.UsuariaId != usuario.Id)
                return Forbid();
        }

        var novaMensagem = new MensagemChat
        {
            SessaoChatId = sessao.Id,
            RemetenteId = usuario.Id,
            Texto = request.Texto.Trim(),
            DataCriacao = DateTime.UtcNow
        };

        _context.Add(novaMensagem);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            novaMensagem.Id,
            novaMensagem.SessaoChatId,
            novaMensagem.RemetenteId,
            novaMensagem.Texto,
            novaMensagem.DataCriacao,
            sessaoStatus = sessao.Status,
            respondida = sessao.Respondida
        });
    }

    [HttpPost("sessoes/iniciar-servico")]
    public async Task<IActionResult> IniciarServico([FromBody] IniciarServicoRequest request)
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        // 1. Verificar se a viajante tem um match/pareamento ou viagem ativa
        var solicitacaoAtiva = await _context.Set<Solicitacao>()
            .Where(s => s.UsuariaId == usuario.Id && (s.Status == "Aberta" || s.Status == "Aceita"))
            .OrderByDescending(s => s.DataCriacao)
            .FirstOrDefaultAsync();

        if (solicitacaoAtiva == null)
            return BadRequest(new { mensagem = "Você precisa selecionar um destino e adquirir créditos antes de acionar serviços." });

        // 2. Extrair TimeLocalId com base no destino da solicitação
        int? timeLocalId = null;
        if (!string.IsNullOrWhiteSpace(solicitacaoAtiva.Destino))
        {
            var cidadeDestino = solicitacaoAtiva.Destino.Split(',')[0].Trim().ToLower();
            var timeLocal = await _context.Set<TimeLocal>()
                .FirstOrDefaultAsync(t => t.Cidade.ToLower().Contains(cidadeDestino));
            timeLocalId = timeLocal?.Id;
        }

        int custo = 0;
        switch (request.ServicoTipo.Trim().ToLower())
        {
            case "dicas locais":
            case "dicas locais (chat)":
                custo = 1;
                break;
            case "ligacao/suporte":
            case "ligação/suporte":
            case "ligação suporte":
            case "ligacao suporte":
                custo = 3;
                break;
            case "busca no aeroporto":
            case "busca aeroporto":
            case "apoio aeroporto":
                custo = 12; // Aligned with mobile UI (12 credits) and web UI (10/12 credits)
                break;
            case "acompanhamento presencial":
            case "acompanhamento":
                if (request.AcompanhamentoDataInicio.HasValue && request.AcompanhamentoDataFim.HasValue)
                {
                    var dataInicio = request.AcompanhamentoDataInicio.Value;
                    var dataFim = request.AcompanhamentoDataFim.Value;

                    if (!string.IsNullOrWhiteSpace(request.AcompanhamentoHoraInicio))
                    {
                        var parts = request.AcompanhamentoHoraInicio.Split(':');
                        if (parts.Length >= 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
                        {
                            dataInicio = new DateTime(dataInicio.Year, dataInicio.Month, dataInicio.Day, h, m, 0);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(request.AcompanhamentoHoraFim))
                    {
                        var parts = request.AcompanhamentoHoraFim.Split(':');
                        if (parts.Length >= 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
                        {
                            dataFim = new DateTime(dataFim.Year, dataFim.Month, dataFim.Day, h, m, 0);
                        }
                    }

                    double totalHoras = (dataFim - dataInicio).TotalHours;
                    int horasCalculadas = (int)Math.Max(1, Math.Ceiling(totalHoras));
                    custo = horasCalculadas * 5;
                    request.QuantidadeHoras = horasCalculadas;
                }
                else
                {
                    int horas = request.QuantidadeHoras ?? 4;
                    custo = horas * 5;
                }
                break;
            default:
                return BadRequest(new { mensagem = $"Tipo de serviço inválido: '{request.ServicoTipo}'." });
        }

        // 3. Verificar se a viajante possui créditos suficientes
        if (usuario.SaldoCreditos < custo)
            return BadRequest(new { mensagem = $"Saldo de créditos insuficiente para este serviço (Custo: {custo} cr, Saldo: {usuario.SaldoCreditos} cr). Por favor, adquira mais créditos." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 4. Debitar créditos e registrar transação
            usuario.SaldoCreditos -= custo;
            await _userManager.UpdateAsync(usuario);

            var transacao = new TransacaoCredito
            {
                UsuariaId = usuario.Id,
                Quantidade = -custo,
                Tipo = "Consumo",
                Descricao = $"Consumo: {request.ServicoTipo}",
                PrecoPago = 0,
                DataCriacao = DateTime.UtcNow
            };
            _context.Add(transacao);

            // 5. Criar sessão de chat nativo em status pendente de aceitação de alguma Madrinha
            var sessao = new SessaoChat
            {
                UsuariaId = usuario.Id,
                MadrinhaId = null, // Inicialmente aberto sem match
                TimeLocalId = timeLocalId,
                ServicoTipo = request.ServicoTipo,
                Status = "Pendente",
                DataInicio = DateTime.UtcNow,
                DataCriacao = DateTime.UtcNow,
                CreditosConsumidos = custo,
                SlaLimite = DateTime.UtcNow.AddMinutes(15), // SLA limite de 15 min de resposta
                Respondida = false,

                // Parâmetros específicos
                HorarioDesembarque = string.IsNullOrWhiteSpace(request.HorarioDesembarque) ? (DateTime?)null : DateTime.TryParse(request.HorarioDesembarque, out var hd) ? hd : (DateTime?)null,
                Aeroporto = request.Aeroporto,
                LocaisVisitados = request.LocaisVisitados,
                QuantidadeHoras = request.QuantidadeHoras,
                AcompanhamentoDataInicio = request.AcompanhamentoDataInicio,
                AcompanhamentoDataFim = request.AcompanhamentoDataFim,
                AcompanhamentoHoraInicio = request.AcompanhamentoHoraInicio,
                AcompanhamentoHoraFim = request.AcompanhamentoHoraFim,
                Avaliada = false,
                PontoEncontro = request.PontoEncontro,
                DuvidaInicial = request.DuvidaInicial
            };

            _context.Add(sessao);
            await _context.SaveChangesAsync();

            // Formatação da mensagem do sistema
            string txtSistema = $"[Sistema] Serviço '{request.ServicoTipo}' solicitado. Custo: {custo} créditos.";
            if (request.ServicoTipo.ToLower().Contains("busca"))
            {
                txtSistema += $" | Aeroporto: {request.Aeroporto} | Desembarque: {request.HorarioDesembarque}";
            }
            else if (request.ServicoTipo.ToLower().Contains("acompanhamento"))
            {
                txtSistema += $" | Duração: {request.QuantidadeHoras} horas | Roteiro: {request.LocaisVisitados}";
            }
            txtSistema += " | Aguardando aceitação por uma Madrinha do time local (SLA máximo de 15 minutos).";

            var msgSistema = new MensagemChat
            {
                SessaoChatId = sessao.Id,
                RemetenteId = 0,
                Texto = txtSistema,
                DataCriacao = DateTime.UtcNow
            };
            _context.Add(msgSistema);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(request.DuvidaInicial))
            {
                var msgDuvida = new MensagemChat
                {
                    SessaoChatId = sessao.Id,
                    RemetenteId = usuario.Id,
                    Texto = request.DuvidaInicial,
                    DataCriacao = DateTime.UtcNow
                };
                _context.Add(msgDuvida);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                sessaoId = sessao.Id,
                status = sessao.Status,
                saldoRestante = usuario.SaldoCreditos,
                tempoLimite = sessao.TempoLimite,
                slaLimite = sessao.SlaLimite
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mensagem = "Erro ao iniciar serviço.", detalhe = ex.Message });
        }
    }

    [HttpGet("sessoes/demandas-disponiveis")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> ObterDemandasDisponiveis()
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var madrinha = await _context.Set<Madrinha>()
            .FirstOrDefaultAsync(m => m.UsuarioID == usuario.Id);

        if (madrinha == null)
            return NotFound(new { mensagem = "Perfil de Madrinha não encontrado." });

        if (!madrinha.TimeLocalId.HasValue)
            return Ok(new System.Collections.Generic.List<object>());

        var sessoes = await _context.Set<SessaoChat>()
            .Include(s => s.Usuaria)
            .Where(s => s.MadrinhaId == null && s.Status == "Pendente" && s.TimeLocalId == madrinha.TimeLocalId.Value)
            .OrderByDescending(s => s.DataCriacao)
            .Select(s => new
            {
                s.Id,
                s.UsuariaId,
                s.ServicoTipo,
                s.Status,
                s.DataCriacao,
                s.CreditosConsumidos,
                s.HorarioDesembarque,
                s.Aeroporto,
                s.LocaisVisitados,
                s.QuantidadeHoras,
                s.AcompanhamentoDataInicio,
                s.AcompanhamentoDataFim,
                s.AcompanhamentoHoraInicio,
                s.AcompanhamentoHoraFim,
                s.PontoEncontro,
                s.DuvidaInicial,
                ViajanteNome = s.Usuaria.Nome,
                ViajanteFotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(s.Usuaria.FotoPerfilUrl),
                ViajanteEmail = s.Usuaria.Email,
                ViajanteTelefone = s.Usuaria.Telefone,
                ViajanteBio = s.Usuaria.Bio,
                ViajanteCidade = s.Usuaria.Cidade,
                ViajanteEstado = s.Usuaria.Estado,
                ViagemDestino = _context.Set<Solicitacao>()
                    .Where(sol => sol.UsuariaId == s.UsuariaId && (sol.Status == "Aberta" || sol.Status == "Aceita"))
                    .OrderByDescending(sol => sol.DataCriacao)
                    .Select(sol => sol.Destino)
                    .FirstOrDefault(),
                ViagemInicio = _context.Set<Solicitacao>()
                    .Where(sol => sol.UsuariaId == s.UsuariaId && (sol.Status == "Aberta" || sol.Status == "Aceita"))
                    .OrderByDescending(sol => sol.DataCriacao)
                    .Select(sol => (DateTime?)sol.DataInicio)
                    .FirstOrDefault(),
                ViagemFim = _context.Set<Solicitacao>()
                    .Where(sol => sol.UsuariaId == s.UsuariaId && (sol.Status == "Aberta" || sol.Status == "Aceita"))
                    .OrderByDescending(sol => sol.DataCriacao)
                    .Select(sol => (DateTime?)sol.DataFim)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(sessoes);
    }

    [HttpPost("sessoes/{id}/aceitar")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> AceitarDemanda(int id)
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var madrinha = await _context.Set<Madrinha>()
            .Include(m => m.Usuario)
            .FirstOrDefaultAsync(m => m.UsuarioID == usuario.Id);

        if (madrinha == null)
            return NotFound(new { mensagem = "Perfil de Madrinha não encontrado." });

        var sessao = await _context.Set<SessaoChat>()
            .Include(s => s.Usuaria)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (sessao == null)
            return NotFound(new { mensagem = "Demanda/Sessão não encontrada." });

        if (sessao.MadrinhaId != null || sessao.Status != "Pendente")
            return BadRequest(new { mensagem = "Esta demanda já foi aceita por outra especialista ou já foi encerrada." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Vincular Madrinha ao chat
            sessao.MadrinhaId = madrinha.Id;
            sessao.Status = "Ativa";
            sessao.SlaLimite = DateTime.UtcNow.AddMinutes(15);
            sessao.Respondida = true; // Auto-limpa pendência de resposta

            if (sessao.ServicoTipo.ToLower().StartsWith("dicas"))
            {
                sessao.TempoLimite = DateTime.UtcNow.AddMinutes(30);
            }

            _context.Update(sessao);

            // 2. Adicionar os créditos ganhos à Madrinha que aceitou
            madrinha.Usuario.SaldoCreditos += sessao.CreditosConsumidos;
            _context.Update(madrinha.Usuario);

            // Registrar a transação de ganho para a Madrinha
            var transacaoGanho = new TransacaoCredito
            {
                UsuariaId = madrinha.Usuario.Id,
                Quantidade = sessao.CreditosConsumidos,
                Tipo = "Ganho",
                Descricao = $"Ganho pelo serviço: {sessao.ServicoTipo} (Viajante: {sessao.Usuaria.Nome})",
                PrecoPago = 0,
                DataCriacao = DateTime.UtcNow
            };
            _context.Add(transacaoGanho);

            // 3. Atualizar carga de atendimentos
            madrinha.CargaAtendimentosAtivos += 1;
            _context.Update(madrinha);

            // 4. Mensagem do sistema
            var msgAceito = new MensagemChat
            {
                SessaoChatId = sessao.Id,
                RemetenteId = 0,
                Texto = $"[Sistema] A Madrinha {madrinha.Usuario.Nome} aceitou sua solicitação. O canal está aberto para contato!",
                DataCriacao = DateTime.UtcNow
            };
            _context.Add(msgAceito);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { mensagem = "Demanda aceita com sucesso!", sessaoId = sessao.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mensagem = "Erro ao aceitar atendimento.", detalhe = ex.Message });
        }
    }

    [HttpPost("sessoes/{id}/encerrar")]
    public async Task<IActionResult> EncerrarSessao(int id)
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var sessao = await _context.Set<SessaoChat>()
            .Include(s => s.Usuaria)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        if (sessao == null)
            return NotFound(new { mensagem = "Sessão não encontrada." });

        var roles = await _userManager.GetRolesAsync(usuario);
        bool isMadrinha = roles.Contains("Madrinha");

        if (isMadrinha)
        {
            var madrinha = await _context.Set<Madrinha>().FirstOrDefaultAsync(m => m.UsuarioID == usuario.Id);
            if (madrinha == null || sessao.MadrinhaId != madrinha.Id)
                return Forbid();
        }
        else
        {
            if (sessao.UsuariaId != usuario.Id)
                return Forbid();
        }

        if (sessao.Status == "Finalizada")
            return BadRequest(new { mensagem = "Esta sessão já está encerrada." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            sessao.Status = "Finalizada";
            _context.Update(sessao);

            var msgEncerrado = new MensagemChat
            {
                SessaoChatId = sessao.Id,
                RemetenteId = 0,
                Texto = $"[Sistema] O atendimento do serviço ({sessao.ServicoTipo}) foi encerrado por {(isMadrinha ? "Madrinha" : "Viajante")}.",
                DataCriacao = DateTime.UtcNow
            };
            _context.Add(msgEncerrado);

            if (sessao.MadrinhaId.HasValue)
            {
                var madrinhaVinculada = await _context.Set<Madrinha>().FindAsync(sessao.MadrinhaId.Value);
                if (madrinhaVinculada != null && madrinhaVinculada.CargaAtendimentosAtivos > 0)
                {
                    madrinhaVinculada.CargaAtendimentosAtivos -= 1;
                    _context.Update(madrinhaVinculada);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { mensagem = "Atendimento encerrado com sucesso!", status = sessao.Status });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mensagem = "Erro ao encerrar atendimento.", detalhe = ex.Message });
        }
    }

    [HttpPost("verificar-sla")]
    [AllowAnonymous]
    public async Task<IActionResult> VerificarSla()
    {
        var sessoesPendentes = await _context.Set<SessaoChat>()
            .Include(s => s.Usuaria)
            .Include(s => s.Madrinha)
                .ThenInclude(m => m!.Usuario)
            .Where(s => s.Status == "Pendente" && !s.Respondida && DateTime.UtcNow > s.SlaLimite)
            .ToListAsync();

        int redistribuicoes = 0;
        var log = new System.Collections.Generic.List<string>();

        foreach (var sessao in sessoesPendentes)
        {
            if (sessao.MadrinhaId.HasValue)
            {
                // Madrinha assinalada mas inativa (atrasou para responder a primeira vez)
                var madrinhaAntiga = sessao.Madrinha!;
                madrinhaAntiga.AtivaFilaAlocacao = false;
                madrinhaAntiga.CargaAtendimentosAtivos = Math.Max(0, madrinhaAntiga.CargaAtendimentosAtivos - 1);
                _context.Update(madrinhaAntiga);

                var msgFalhaSla = new MensagemChat
                {
                    SessaoChatId = sessao.Id,
                    RemetenteId = 0,
                    Texto = $"[Sistema] A Madrinha {madrinhaAntiga.Usuario.Nome} não respondeu dentro do SLA limite de 15 minutos e foi removida temporariamente da fila.",
                    DataCriacao = DateTime.UtcNow
                };
                _context.Add(msgFalhaSla);

                if (madrinhaAntiga.TimeLocalId.HasValue)
                {
                    var novaMadrinha = await _context.Set<Madrinha>()
                        .Include(m => m.Usuario)
                        .Where(m => m.TimeLocalId == madrinhaAntiga.TimeLocalId && m.Disponivel && m.AtivaFilaAlocacao && m.Id != madrinhaAntiga.Id)
                        .OrderBy(m => m.CargaAtendimentosAtivos)
                        .FirstOrDefaultAsync();

                    if (novaMadrinha != null)
                    {
                        novaMadrinha.CargaAtendimentosAtivos += 1;
                        _context.Update(novaMadrinha);

                        sessao.MadrinhaId = novaMadrinha.Id;
                        sessao.SlaLimite = DateTime.UtcNow.AddMinutes(15);
                        _context.Update(sessao);

                        var solicitacao = await _context.Set<Solicitacao>()
                            .Where(s => s.UsuariaId == sessao.UsuariaId && s.MadrinhaId == madrinhaAntiga.Id && (s.Status == "Aceita" || s.Status == "Aberta"))
                            .FirstOrDefaultAsync();

                        if (solicitacao != null)
                        {
                            solicitacao.MadrinhaId = novaMadrinha.Id;
                            _context.Update(solicitacao);
                        }

                        var msgRedistribuida = new MensagemChat
                        {
                            SessaoChatId = sessao.Id,
                            RemetenteId = 0,
                            Texto = $"[Sistema] Atendimento redistribuído. Sua nova Madrinha é {novaMadrinha.Usuario.Nome}.",
                            DataCriacao = DateTime.UtcNow
                        };
                        _context.Add(msgRedistribuida);

                        redistribuicoes++;
                        log.Add($"Sessão {sessao.Id} redistribuída de {madrinhaAntiga.Usuario.Nome} para {novaMadrinha.Usuario.Nome}.");
                    }
                    else
                    {
                        sessao.Status = "Finalizada";
                        _context.Update(sessao);

                        var msgFalhaGeral = new MensagemChat
                        {
                            SessaoChatId = sessao.Id,
                            RemetenteId = 0,
                            Texto = "[Sistema] Sem outras Madrinhas locais disponíveis. O suporte concierge PortoSegura central foi notificado.",
                            DataCriacao = DateTime.UtcNow
                        };
                        _context.Add(msgFalhaGeral);

                        log.Add($"Sessão {sessao.Id} com falha de SLA, sem outras madrinhas disponíveis.");
                    }
                }
            }
            else
            {
                // Nenhuma Madrinha do Time Local aceitou a demanda em 15 minutos
                var msgExcedido = new MensagemChat
                {
                    SessaoChatId = sessao.Id,
                    RemetenteId = 0,
                    Texto = "[Sistema] SLA excedido. Nenhuma Madrinha do Time Local aceitou este chamado em 15 minutos. O concierge PortoSegura central assumirá o atendimento manual.",
                    DataCriacao = DateTime.UtcNow
                };
                _context.Add(msgExcedido);

                sessao.Status = "Finalizada";
                _context.Update(sessao);

                log.Add($"Sessão {sessao.Id} pendente sem aceite por nenhuma Madrinha do time local no prazo de 15 minutos.");
            }
        }

        if (redistribuicoes > 0 || log.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return Ok(new { mensagem = "Varredura de SLA concluída.", sessoesVerificadas = sessoesPendentes.Count, redistribuicoes, logs = log });
    }

    private async Task<Usuaria?> ObterUsuarioAutenticadoAsync()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(usuarioId) || !int.TryParse(usuarioId, out var id))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(id.ToString());
    }

    [HttpPost("sessoes/{sessaoId}/webrtc/signal")]
    public async Task<IActionResult> EnviarSinalWebRtc(int sessaoId, [FromBody] WebRtcSignalRequest request)
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var signal = new WebRtcSignal
        {
            Type = request.Type,
            Sdp = request.Sdp,
            Candidate = request.Candidate,
            SdpMid = request.SdpMid,
            SdpMLineIndex = request.SdpMLineIndex,
            SenderId = usuario.Id
        };

        WebRtcSignalingManager.SendSignal(sessaoId, signal);
        return Ok(new { mensagem = "Sinal enviado com sucesso!" });
    }

    [HttpGet("sessoes/{sessaoId}/webrtc/signals")]
    public async Task<IActionResult> ObterSinaisWebRtc(int sessaoId, [FromQuery] string sinceUtc)
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        DateTime since = DateTime.MinValue;
        if (!string.IsNullOrEmpty(sinceUtc) && DateTime.TryParse(sinceUtc, out var dt))
        {
            since = dt.ToUniversalTime();
        }

        var signals = WebRtcSignalingManager.GetSignals(sessaoId, since);
        return Ok(signals);
    }
}

public class EnviarMensagemRequest
{
    public int SessaoId { get; set; }
    public required string Texto { get; set; }
}

public class IniciarServicoRequest
{
    public required string ServicoTipo { get; set; }
    public string? HorarioDesembarque { get; set; }
    public string? Aeroporto { get; set; }
    public string? LocaisVisitados { get; set; }
    public int? QuantidadeHoras { get; set; }
    public DateTime? AcompanhamentoDataInicio { get; set; }
    public DateTime? AcompanhamentoDataFim { get; set; }
    public string? AcompanhamentoHoraInicio { get; set; }
    public string? AcompanhamentoHoraFim { get; set; }
    public string? PontoEncontro { get; set; }
    public string? DuvidaInicial { get; set; }
}

public class WebRtcSignalRequest
{
    public required string Type { get; set; } // "offer", "answer", "candidate", "hangup"
    public string? Sdp { get; set; }
    public string? Candidate { get; set; }
    public string? SdpMid { get; set; }
    public int? SdpMLineIndex { get; set; }
}

public class WebRtcSignal
{
    public int Id { get; set; }
    public required string Type { get; set; }
    public string? Sdp { get; set; }
    public string? Candidate { get; set; }
    public string? SdpMid { get; set; }
    public int? SdpMLineIndex { get; set; }
    public int SenderId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class WebRtcSignalingManager
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, List<WebRtcSignal>> _signals = new();
    private static int _nextSignalId = 1;
    private static readonly object _lock = new();

    public static void SendSignal(int sessaoId, WebRtcSignal signal)
    {
        lock (_lock)
        {
            signal.Id = _nextSignalId++;
            signal.Timestamp = DateTime.UtcNow;
            
            var list = _signals.GetOrAdd(sessaoId, _ => new List<WebRtcSignal>());
            list.Add(signal);
            
            if (list.Count > 100)
            {
                list.RemoveRange(0, list.Count - 100);
            }
        }
    }

    public static List<WebRtcSignal> GetSignals(int sessaoId, DateTime since)
    {
        var result = new List<WebRtcSignal>();
        if (_signals.TryGetValue(sessaoId, out var list))
        {
            lock (_lock)
            {
                foreach (var signal in list)
                {
                    if (signal.Timestamp > since)
                    {
                        result.Add(signal);
                    }
                }
            }
        }
        return result;
    }

    public static void ClearSession(int sessaoId)
    {
        _signals.TryRemove(sessaoId, out _);
    }
}
