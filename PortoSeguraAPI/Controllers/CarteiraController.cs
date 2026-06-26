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
[Authorize(Roles = "Usuaria")]
public class CarteiraController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<Usuaria> _userManager;
    private readonly BlobStorageService _blobStorageService;

    public CarteiraController(AppDbContext context, UserManager<Usuaria> userManager, BlobStorageService blobStorageService)
    {
        _context = context;
        _userManager = userManager;
        _blobStorageService = blobStorageService;
    }

    [HttpGet("saldo")]
    public async Task<IActionResult> ObterSaldo()
    {
        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        return Ok(new { saldo = usuaria.SaldoCreditos });
    }

    [HttpGet("historico")]
    public async Task<IActionResult> ObterHistorico()
    {
        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var historico = await _context.Set<TransacaoCredito>()
            .Where(t => t.UsuariaId == usuaria.Id)
            .OrderByDescending(t => t.DataCriacao)
            .Select(t => new
            {
                t.Id,
                t.Quantidade,
                t.Tipo,
                t.Descricao,
                t.PrecoPago,
                t.DataCriacao
            })
            .ToListAsync();

        return Ok(historico);
    }

    [HttpPost("comprar-pacote")]
    public async Task<IActionResult> ComprarPacote([FromBody] CompraPacoteRequest request)
    {
        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        int creditos = 0;
        decimal preco = 0;

        switch (request.PacoteNome.Trim().ToLower())
        {
            case "exploradora":
                creditos = 20;
                preco = 130.00m;
                break;
            case "segurancatotal":
            case "seguranca total":
            case "segurança total":
                creditos = 40;
                preco = 250.00m;
                break;
            case "imersaorecife":
            case "imersao recife":
            case "imersão recife":
                creditos = 70;
                preco = 430.00m;
                break;
            default:
                return BadRequest(new { mensagem = "Pacote de créditos inválido." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Atualizar saldo da usuária
            usuaria.SaldoCreditos += creditos;
            await _userManager.UpdateAsync(usuaria);

            // 2. Registrar transação
            var transacao = new TransacaoCredito
            {
                UsuariaId = usuaria.Id,
                Quantidade = creditos,
                Tipo = "Compra",
                Descricao = $"Compra de Pacote {request.PacoteNome}",
                PrecoPago = preco,
                DataCriacao = DateTime.UtcNow
            };
            _context.Add(transacao);
            await _context.SaveChangesAsync();

            // 3. Criar a Solicitação/Viagem ativa com MadrinhaId = null
            string? timeNome = null;
            if (!string.IsNullOrWhiteSpace(request.Destino))
            {
                var destinoNormalizado = request.Destino.Trim().ToLower();
                var timeLocal = await _context.Set<TimeLocal>()
                    .FirstOrDefaultAsync(t => t.Cidade.ToLower().Contains(destinoNormalizado) || t.Nome.ToLower().Contains(destinoNormalizado));

                if (timeLocal != null)
                {
                    timeNome = timeLocal.Nome;

                    var dataPartida = request.DataInicio ?? DateTime.UtcNow;
                    var dataRetorno = request.DataFim ?? DateTime.UtcNow.AddDays(7);
                    int diarias = (int)Math.Max(1, Math.Ceiling((dataRetorno - dataPartida).TotalDays));

                    var solicitacao = new Solicitacao
                    {
                        UsuariaId = usuaria.Id,
                        MadrinhaId = null, // Inicialmente nulo (sem match)
                        Destino = $"{timeLocal.Cidade}, {timeLocal.Estado}",
                        Descricao = $"Viagem assistida vinculada ao pacote {request.PacoteNome}.",
                        DataCriacao = DateTime.UtcNow,
                        DataInicio = dataPartida,
                        DataFim = dataRetorno,
                        QtdDiarias = diarias,
                        Status = "Aberta",
                        Valor = preco,
                        Usuaria = usuaria,
                        Madrinha = null
                    };
                    _context.Add(solicitacao);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    return BadRequest(new { mensagem = "Destino inválido ou time local não ativo." });
                }
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                mensagem = $"Pacote {request.PacoteNome} adquirido com sucesso! Sua viagem para {request.Destino} está ativa no {timeNome}. Contrate um serviço no painel de suporte para chamar a equipe local.",
                saldo = usuaria.SaldoCreditos,
                destino = request.Destino
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mensagem = "Erro ao processar compra de créditos.", detalhe = ex.Message });
        }
    }

    [HttpPost("adicionar-creditos")]
    public async Task<IActionResult> AdicionarCreditos([FromBody] AdicionarCreditosRequest request)
    {
        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        if (request.Quantidade <= 0)
            return BadRequest(new { mensagem = "A quantidade deve ser maior que zero." });

        usuaria.SaldoCreditos += request.Quantidade;
        await _userManager.UpdateAsync(usuaria);

        var transacao = new TransacaoCredito
        {
            UsuariaId = usuaria.Id,
            Quantidade = request.Quantidade,
            Tipo = "Compra",
            Descricao = "Créditos de Teste/Ajuste manual",
            PrecoPago = 0,
            DataCriacao = DateTime.UtcNow
        };
        _context.Add(transacao);
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = $"{request.Quantidade} créditos adicionados com sucesso.", saldo = usuaria.SaldoCreditos });
    }

    [HttpPost("comprar-creditos-individuais")]
    public async Task<IActionResult> ComprarCreditosIndividuais([FromBody] PortoSeguraAPI.Dtos.ComprarCreditosIndividuaisRequest request)
    {
        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        if (request.Quantidade <= 0)
            return BadRequest(new { mensagem = "A quantidade de créditos deve ser maior que zero." });

        decimal precoUnitario = 7.00m;
        decimal totalPago = request.Quantidade * precoUnitario;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            usuaria.SaldoCreditos += request.Quantidade;
            await _userManager.UpdateAsync(usuaria);

            var transacao = new TransacaoCredito
            {
                UsuariaId = usuaria.Id,
                Quantidade = request.Quantidade,
                Tipo = "Compra",
                Descricao = $"Compra de {request.Quantidade} créditos individuais",
                PrecoPago = totalPago,
                DataCriacao = DateTime.UtcNow
            };
            _context.Add(transacao);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new
            {
                mensagem = $"{request.Quantidade} créditos individuais adquiridos com sucesso!",
                saldo = usuaria.SaldoCreditos
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mensagem = "Erro ao processar compra de créditos.", detalhe = ex.Message });
        }
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
}

public class CompraPacoteRequest
{
    public required string PacoteNome { get; set; }
    public string? Destino { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
}

public class AdicionarCreditosRequest
{
    public int Quantidade { get; set; }
}
