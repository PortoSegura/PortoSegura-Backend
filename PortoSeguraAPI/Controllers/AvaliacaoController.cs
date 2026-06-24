using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortoSeguraAPI.Data;
using PortoSeguraAPI.Dtos;
using PortoSeguraAPI.Models;

namespace PortoSeguraAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AvaliacaoController : ControllerBase
{
    private readonly AppDbContext _context;

    public AvaliacaoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CriarAvaliacao([FromBody] CriarAvaliacaoRequest request)
    {
        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null) return Unauthorized(new { message = "Usuária não autenticada." });

        if (request.Nota < 1 || request.Nota > 5)
        {
            return BadRequest(new { message = "A nota deve ser entre 1 e 5." });
        }

        // 1. Avaliação vinculada ao serviço (SessaoChat)
        if (request.SessaoChatId.HasValue)
        {
            var sessao = await _context.Set<SessaoChat>().FindAsync(request.SessaoChatId.Value);
            if (sessao == null)
                return NotFound(new { message = "Sessão de serviço não encontrada." });

            if (sessao.UsuariaId != usuaria.Id)
                return Forbid();

            if (sessao.Status != "Finalizada" && sessao.Status != "Expirada")
                return BadRequest(new { message = "Apenas serviços finalizados ou expirados podem ser avaliados." });

            if (!sessao.MadrinhaId.HasValue)
                return BadRequest(new { message = "Este serviço não possui uma Madrinha associada para avaliar." });

            if (sessao.Avaliada)
                return BadRequest(new { message = "Este serviço já foi avaliado anteriormente." });

            var avaliacaoServico = new Avaliacao
            {
                SessaoChatId = request.SessaoChatId.Value,
                ServicoTipo = sessao.ServicoTipo,
                MadrinhaId = sessao.MadrinhaId.Value,
                UsuariaId = usuaria.Id,
                Nota = request.Nota,
                Comentario = request.Comentario,
                IsAvaliacaoMadrinha = true,
                SolicitacaoId = null
            };

            _context.Set<Avaliacao>().Add(avaliacaoServico);

            sessao.Avaliada = true;
            _context.Set<SessaoChat>().Update(sessao);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Serviço avaliado com sucesso!" });
        }

        // 2. Fallback para avaliação de viagem legada (Solicitacao)
        if (request.SolicitacaoId.HasValue)
        {
            var solicitacao = await _context.Set<Solicitacao>().FindAsync(request.SolicitacaoId.Value);
            if (solicitacao == null) return BadRequest(new { message = "Solicitação de viagem não encontrada." });

            if (solicitacao.UsuariaId != usuaria.Id && solicitacao.MadrinhaId != usuaria.Id) return Forbid();

            if (solicitacao.Status != "Concluida")
            {
                return BadRequest(new { message = "Apenas solicitações concluídas podem ser avaliadas." });
            }

            var avaliacaoViagem = new Avaliacao
            {
                SolicitacaoId = request.SolicitacaoId.Value,
                MadrinhaId = request.MadrinhaId,
                UsuariaId = usuaria.Id,
                Nota = request.Nota,
                Comentario = request.Comentario,
                IsAvaliacaoMadrinha = solicitacao.UsuariaId == usuaria.Id
            };

            _context.Set<Avaliacao>().Add(avaliacaoViagem);

            solicitacao.Status = "Avaliada";
            _context.Set<Solicitacao>().Update(solicitacao);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Viagem avaliada com sucesso!" });
        }

        return BadRequest(new { message = "É necessário fornecer o id do serviço (SessaoChatId) ou o id da viagem (SolicitacaoId) para avaliar." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletarAvaliacao(int id)
    {
        var avaliacao = await _context.Set<Avaliacao>().FindAsync(id);
        if (avaliacao == null) return NotFound(new { message = "Avaliação não encontrada." });

        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null) return Unauthorized(new { message = "Usuária não autenticada." });
        if (avaliacao.UsuariaId != usuaria.Id && avaliacao.MadrinhaId != usuaria.Id) return Forbid();

        _context.Set<Avaliacao>().Remove(avaliacao);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Avaliação deletada com sucesso!" });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterAvaliacao(int id)
    {
        var avaliacao = await _context.Set<Avaliacao>().FindAsync(id);
        if (avaliacao == null) return NotFound(new { message = "Avaliação não encontrada." });

        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null) return Unauthorized(new { message = "Usuária não autenticada." });
        if (avaliacao.UsuariaId != usuaria.Id && avaliacao.MadrinhaId != usuaria.Id) return Forbid();

        return Ok(avaliacao);
    }

    [HttpGet]
    public async Task<IActionResult> ListarAvaliacoes()
    {
        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null) return Unauthorized(new { message = "Usuária não autenticada." });

        var avaliacoes = await _context.Set<Avaliacao>()
            .Where(a => a.UsuariaId == usuaria.Id || a.MadrinhaId == usuaria.Id).ToListAsync();

        return Ok(avaliacoes);
    }

    private async Task<Usuaria?> ObterUsuarioAutenticadoAsync()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return null;

        var userId = int.Parse(userIdClaim.Value);
        var user = await _context.Set<Usuaria>().FindAsync(userId);
        return user;
    }
}
