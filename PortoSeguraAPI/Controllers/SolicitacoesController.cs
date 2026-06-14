using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortoSeguraAPI.Data;
using PortoSeguraAPI.Models;

[ApiController]
[Route("api/[controller]")]
public class SolicitacoesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<Usuaria> _userManager;
    private readonly BlobStorageService _blobStorageService;

    public SolicitacoesController(AppDbContext context, UserManager<Usuaria> userManager, BlobStorageService blobStorageService)
    {
        _context = context;
        _userManager = userManager;
        _blobStorageService = blobStorageService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CriarSolicitacao([FromBody] CriarSolicitacaoRequest request)
    {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();
        if (usuariaAutenticada == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        if (request.UsuariaId != 0 && request.UsuariaId != usuariaAutenticada.Id)
        {
            return Forbid();
        }

        if (request.DataFim <= request.DataInicio)
        {
            return BadRequest(new { mensagem = "A data final deve ser maior que a data inicial." });
        }

        if (request.QtdDiarias <= 0)
        {
            return BadRequest(new { mensagem = "A quantidade de diárias deve ser maior que zero." });
        }

        var madrinha = await _context.Set<Madrinha>()
            .Include(m => m.Usuario)
            .FirstOrDefaultAsync(m => m.Id == request.MadrinhaId);

        if (madrinha == null)
        {
            return NotFound(new { mensagem = "Madrinha não encontrada." });
        }

        var solicitacao = new Solicitacao
        {
            UsuariaId = usuariaAutenticada.Id,
            MadrinhaId = madrinha.Id,
            Destino = madrinha.Usuario.Cidade + ", " + madrinha.Usuario.Estado,
            Descricao = request.Descricao.Trim(),
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            QtdDiarias = request.QtdDiarias,
            Valor = request.Valor,
            Status = "Aberta",
            DataCriacao = DateTime.UtcNow,
            Usuaria = usuariaAutenticada,
            Madrinha = madrinha
        };

        _context.Set<Solicitacao>().Add(solicitacao);
        await _context.SaveChangesAsync();

        return Ok(MapearSolicitacao(solicitacao));
    }

    [HttpGet("minhas-solicitacoes")]
    [Authorize]
    public async Task<IActionResult> ObterMinhasSolicitacoes()
    {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();
        if (usuariaAutenticada == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var solicitacoesDb = await _context.Set<Solicitacao>()
            .AsNoTracking()
            .Include(s => s.Madrinha)
                .ThenInclude(m => m.Usuario)
            .Where(s => s.UsuariaId == usuariaAutenticada.Id)
            .OrderByDescending(s => s.DataCriacao)
            .Select(s => new
            {
                s.Id,
                s.UsuariaId,
                s.MadrinhaId,
                s.Descricao,
                s.Destino,
                s.DataInicio,
                s.DataFim,
                s.QtdDiarias,
                s.Valor,
                s.Status,
                s.DataCriacao,
                Madrinha = new
                {
                    s.Madrinha.Id,
                    Nome = s.Madrinha.Usuario.Nome,
                    Telefone = s.Status == "Aceita" ? s.Madrinha.Usuario.Telefone : null,
                    s.Madrinha.PrecoDiaria,
                    s.Madrinha.VerificadoIdentidade,
                    s.Madrinha.VerificadoResidencia,
                    s.Madrinha.TrilhaCursoCompleto,
                    RawFotoPerfilUrl = s.Madrinha.Usuario.FotoPerfilUrl
                },
                Avaliacao = s.Avaliacoes.Select(a => new
                {
                    a.Id,
                    a.Nota,
                    a.Comentario,
                    a.DataCriacao
                }).FirstOrDefault()
            })
            .ToListAsync();

        var solicitacoes = solicitacoesDb.Select(s => new
        {
            s.Id,
            s.UsuariaId,
            s.MadrinhaId,
            s.Descricao,
            s.Destino,
            s.DataInicio,
            s.DataFim,
            s.QtdDiarias,
            s.Valor,
            s.Status,
            s.DataCriacao,
            Madrinha = new
            {
                s.Madrinha.Id,
                s.Madrinha.Nome,
                s.Madrinha.Telefone,
                s.Madrinha.PrecoDiaria,
                s.Madrinha.VerificadoIdentidade,
                s.Madrinha.VerificadoResidencia,
                s.Madrinha.TrilhaCursoCompleto,
                FotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(s.Madrinha.RawFotoPerfilUrl)
            },
            s.Avaliacao
        }).ToList();

        return Ok(solicitacoes);
    }


    [HttpGet("solicitacoes-madrinha")]
    [Authorize (Roles = "Madrinha")]
    public async Task<IActionResult> ObterSolicitacoesPorMadrinha()
    {

        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();


        if (usuariaAutenticada == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var madrinha = await _context.Set<Madrinha>()
            .FirstOrDefaultAsync(m => m.UsuarioID == usuariaAutenticada.Id);

        if (madrinha == null)
            return NotFound(new { mensagem = "Perfil de madrinha não encontrado." });
        
    
        var solicitacoesDb = await _context.Set<Solicitacao>()
            .AsNoTracking()
            .Include(s => s.Madrinha)
                .ThenInclude(m => m.Usuario)
            .Include(s => s.Usuaria)
            .Where(s => s.MadrinhaId == madrinha.Id)
            .OrderByDescending(s => s.DataCriacao)
            .Select(s => new
            {
                s.Id,
                s.UsuariaId,
                s.MadrinhaId,
                s.Descricao,
                s.DataInicio,
                s.DataFim,
                s.QtdDiarias,
                s.Valor,
                s.Status,
                s.DataCriacao,
                Usuaria = new
                {
                    s.Usuaria.Id,
                    Nome = s.Usuaria.Nome,
                    s.Usuaria.Email,
                    Telefone = s.Status == "Aceita" ? s.Usuaria.Telefone : null,
                    s.Usuaria.Bio,
                    s.Usuaria.Estado,
                    s.Usuaria.Cidade,
                    RawFotoPerfilUrl = s.Usuaria.FotoPerfilUrl
                },
                Avaliacao = s.Avaliacoes.Select(a => new
                {
                    a.Id,
                    a.Nota,
                    a.Comentario,
                    a.DataCriacao
                }).FirstOrDefault()
            })
            .ToListAsync();

        var solicitacoes = solicitacoesDb.Select(s => new
        {
            s.Id,
            s.UsuariaId,
            s.MadrinhaId,
            s.Descricao,
            s.DataInicio,
            s.DataFim,
            s.QtdDiarias,
            s.Valor,
            s.Status,
            s.DataCriacao,
            Usuaria = new
            {
                s.Usuaria.Id,
                s.Usuaria.Nome,
                s.Usuaria.Email,
                s.Usuaria.Telefone,
                s.Usuaria.Bio,
                s.Usuaria.Estado,
                s.Usuaria.Cidade,
                FotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(s.Usuaria.RawFotoPerfilUrl)
            },
            s.Avaliacao
        }).ToList();

        return Ok(solicitacoes);
    }

    [HttpPost("{id}/cancelar")]
    [Authorize]
    public async Task<IActionResult> CancelarSolicitacao(int id)
    {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();
        if (usuariaAutenticada == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var madrinhaAutenticada = await _context.Set<Madrinha>()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UsuarioID == usuariaAutenticada.Id);

        var solicitacao = await _context.Set<Solicitacao>()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitacao == null)
        {
            return NotFound(new { mensagem = "Solicitação não encontrada." });
        }

        if (solicitacao.UsuariaId != usuariaAutenticada.Id && (madrinhaAutenticada == null || solicitacao.MadrinhaId != madrinhaAutenticada.Id))
        {
            return Forbid();
        }

        if (solicitacao.Status is "Cancelada" or "Concluida")
        {
            return BadRequest(new { mensagem = "Essa solicitação não pode mais ser cancelada." });
        }

        solicitacao.Status = "Cancelada";
        await _context.SaveChangesAsync();

        return Ok(MapearSolicitacao(solicitacao));
    }

    [HttpPost("{id}/concluir")]
    [Authorize]
    public async Task<IActionResult> ConcluirSolicitacao(int id)
    {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();
        if (usuariaAutenticada == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var solicitacao = await _context.Set<Solicitacao>()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitacao == null)
        {
            return NotFound(new { mensagem = "Solicitação não encontrada." });
        }

        var madrinhaAutenticada = await _context.Set<Madrinha>()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UsuarioID == usuariaAutenticada.Id);

        if (solicitacao.UsuariaId != usuariaAutenticada.Id && (madrinhaAutenticada == null || solicitacao.MadrinhaId != madrinhaAutenticada.Id))
        {
            return Forbid();
        }

        if (solicitacao.Status != "Aceita")
        {
            return BadRequest(new { mensagem = "Apenas solicitações aceitas podem ser concluídas." });
        }

        solicitacao.Status = "Concluida";
        await _context.SaveChangesAsync();

        return Ok(MapearSolicitacao(solicitacao));
    }

    [HttpPost("{id}/avaliar")]
    [Authorize(Roles = "Usuaria")]
    public async Task<IActionResult> AvaliarSolicitacao(int id)
    {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();
        if (usuariaAutenticada == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var solicitacao = await _context.Set<Solicitacao>()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitacao == null)
        {
            return NotFound(new { mensagem = "Solicitação não encontrada." });
        }

        if (solicitacao.UsuariaId != usuariaAutenticada.Id)
        {
            return Forbid();
        }

        if (solicitacao.Status != "Concluida")
        {
            return BadRequest(new { mensagem = "A solicitação só pode ser avaliada após a conclusão." });
        }

        solicitacao.Status = "Avaliada";
        await _context.SaveChangesAsync();

        return Ok(MapearSolicitacao(solicitacao));
    }

    [HttpPost("{id}/aceitar")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> AceitarSolicitacao(int id)
    {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();
        if (usuariaAutenticada == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var madrinha = await _context.Set<Madrinha>()
            .FirstOrDefaultAsync(m => m.UsuarioID == usuariaAutenticada.Id);

        if (madrinha == null)
        {
            return NotFound(new { mensagem = "Perfil de madrinha não encontrado." });
        }

        var solicitacao = await _context.Set<Solicitacao>()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitacao == null)
        {
            return NotFound(new { mensagem = "Solicitação não encontrada." });
        }

        if (solicitacao.MadrinhaId != madrinha.Id)
        {
            return Forbid();
        }

        if (solicitacao.Status != "Aberta")
        {
            return BadRequest(new { mensagem = "A solicitação não está disponível para aceite." });
        }

        solicitacao.Status = "Aceita";
        await _context.SaveChangesAsync();

        return Ok(MapearSolicitacao(solicitacao));
    }

    [HttpPost("{id}/recusar")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> RecusarSolicitacao(int id) {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();
        if (usuariaAutenticada == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var madrinha = await _context.Set<Madrinha>()
            .FirstOrDefaultAsync(m => m.UsuarioID == usuariaAutenticada.Id);

        if (madrinha == null)
        {
            return NotFound(new { mensagem = "Perfil de madrinha não encontrado." });
        }

        var solicitacao = await _context.Set<Solicitacao>()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitacao == null)
        {
            return NotFound(new { mensagem = "Solicitação não encontrada." });
        }

        if (solicitacao.MadrinhaId != madrinha.Id)
        {
            return Forbid();
        }

        if (solicitacao.Status != "Aberta")
        {
            return BadRequest(new { mensagem = "A solicitação não está disponível para ser recusada." });
        }

        solicitacao.Status = "Recusada";
        await _context.SaveChangesAsync();

        return Ok(MapearSolicitacao(solicitacao));
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

    private static object MapearSolicitacao(Solicitacao solicitacao)
    {
        return new
        {
            solicitacao.Id,
            solicitacao.UsuariaId,
            solicitacao.MadrinhaId,
            solicitacao.Descricao,
            solicitacao.DataInicio,
            solicitacao.DataFim,
            solicitacao.QtdDiarias,
            solicitacao.Valor,
            solicitacao.Status,
            solicitacao.DataCriacao
        };
    }
}