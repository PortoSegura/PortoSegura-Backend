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
        var solicitacao = await _context.Set<Solicitacao>().FindAsync(request.SolicitacaoId);

        if(solicitacao == null) return BadRequest(new { message = "Solicitação não encontrada." });

        var usuaria = await ObterUsuarioAutenticadoAsync();
        if (usuaria == null) return Unauthorized( new { message = "Usuária não autenticada." });
        if (solicitacao.UsuariaId != usuaria.Id && solicitacao.MadrinhaId != usuaria.Id) return Forbid();

        if (solicitacao.Status != "Concluida")
        {
            return BadRequest(new { message = "Apenas solicitações concluídas podem ser avaliadas." });
        }

        if (request.Nota < 1 || request.Nota > 5)
        {
            return BadRequest(new { message = "A nota deve ser entre 1 e 5." });
        }
        
        var avaliacao = new Avaliacao
        {
            SolicitacaoId = request.SolicitacaoId,
            MadrinhaId = request.MadrinhaId,
            UsuariaId = usuaria.Id,
            Nota = request.Nota,
            Comentario = request.Comentario,
            IsAvaliacaoMadrinha = solicitacao.UsuariaId == usuaria.Id
        };

        _context.Set<Avaliacao>().Add(avaliacao);

        solicitacao.Status = "Avaliada";
        _context.Set<Solicitacao>().Update(solicitacao);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Avaliação criada com sucesso!" });
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
