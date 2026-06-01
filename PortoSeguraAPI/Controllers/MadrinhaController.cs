using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortoSeguraAPI.Data;
using PortoSeguraAPI.Dtos;
using PortoSeguraAPI.Models;

[ApiController]
[Route("api/[controller]")]
public class MadrinhaController : ControllerBase
{
    private readonly AppDbContext _context;

    public MadrinhaController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ObterMadrinhas([FromQuery] string? destino, [FromQuery] decimal? precoMaximo ) {

        var madrinhas = await _context.Set<Madrinha>()
            .Include(m => m.Usuario)
            .Include(m => m.Servicos)
            .Include(m => m.Solicitacoes)
            .Where(m => (string.IsNullOrEmpty(destino) || m.Usuario.Cidade.Contains(destino)) && (!precoMaximo.HasValue || m.PrecoDiaria <= precoMaximo))
            .Select(m => new MadrinhaSummaryDto
            {
                Id = m.Id,
                PrecoDiaria = m.PrecoDiaria,
                FotoPerfilUrl = m.Usuario.FotoPerfilUrl,
                Motivacao = m.Motivacao,
                UsuarioId = m.Usuario.Id,
                Nome = m.Usuario.Nome,
                Cidade = m.Usuario.Cidade,
                Estado = m.Usuario.Estado,
                Servicos = m.Servicos.Select(s => s.Descricao).ToList(),
                QtdSolicitacoes = m.Solicitacoes.Select(s => s.Status == "Finalizada").Count()
            })
            .ToListAsync();

        return Ok(madrinhas);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> ObterMadrinhaPorId(int id) {

        var madrinha = await _context.Set<Madrinha>()
            .Include(m => m.Usuario)
            .Include(m => m.Servicos)
            .Include(m => m.Solicitacoes)
            .Where(m => m.Id == id)
            .Select(m => new MadrinhaSummaryDto
            {
                Id = m.Id,
                PrecoDiaria = m.PrecoDiaria,
                FotoPerfilUrl = m.Usuario.FotoPerfilUrl,
                Motivacao = m.Motivacao,
                UsuarioId = m.Usuario.Id,
                Nome = m.Usuario.Nome,
                Cidade = m.Usuario.Cidade,
                Estado = m.Usuario.Estado,
                Servicos = m.Servicos.Select(s => s.Descricao).ToList(),
                QtdSolicitacoes = m.Solicitacoes.Select(s => s.Status == "Finalizada").Count()
            })
            .FirstOrDefaultAsync();

        return Ok(madrinha);
    }

    [HttpGet("profile")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> ObterProfileMadrinha()
    {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();

        if (usuariaAutenticada == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var madrinha = await _context.Set<Madrinha>()
            .AsNoTracking()
            .Where(m => m.UsuarioID == usuariaAutenticada.Id)
            .Select(m => new 
            {
                Id = m.Id,
                PrecoDiaria = m.PrecoDiaria,
                FotoPerfilUrl = m.Usuario.FotoPerfilUrl,
                Motivacao = m.Motivacao,
                UsuarioId = m.Usuario.Id,
                Nome = m.Usuario.Nome,
                Cidade = m.Usuario.Cidade,
                Estado = m.Usuario.Estado,
                Bio = m.Usuario.Bio,
                Telefone = m.Usuario.Telefone,
                Email = m.Usuario.Email,
                Linkedin = m.Usuario.urlLinkedin,
                Instagram = m.Usuario.urlInstagram,
                Facebook = m.Usuario.urlFacebook,
                Solicitacaoes = m.Solicitacoes.Select(s => new 
                {
                    s.Id,
                    s.Descricao,
                    s.DataInicio,
                    s.DataFim,
                    s.QtdDiarias,
                    s.Valor,
                    s.Status
                }).ToList(),
                Servicos = m.Servicos.Select(s => new {s.Descricao, s.Id}).ToList(),
                QtdSolicitacoes = m.Solicitacoes.Count()
            })
            .FirstOrDefaultAsync();

        if (madrinha == null)
            return NotFound(new { mensagem = "Perfil de madrinha não encontrado." });

        return Ok(madrinha);
    }

    [HttpPut("profile")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> AtualizarProfileMadrinha([FromBody] AtualizarProfileMadrinhaRequest request)
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

        // Atualizar os dados da madrinha
        madrinha.PrecoDiaria = request.PrecoDiaria ?? madrinha.PrecoDiaria;
        madrinha.Motivacao = request.Motivacao ?? madrinha.Motivacao;

        _context.Set<Madrinha>().Update(madrinha);
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = "Perfil de madrinha atualizado com sucesso!" });
    }

    [HttpPost("add-servico")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> AdicionarServico([FromBody] AdicionarServicoRequest request)
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

        var servico = new Servico
        {
            MadrinhaId = madrinha.Id,
            Descricao = request.Descricao
        };

        _context.Set<Servico>().Add(servico);
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = "Serviço adicionado com sucesso!" });
    }

    [HttpDelete("remove-servico/{servicoId}")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> RemoverServico(int servicoId)
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

        var servico = await _context.Set<Servico>()
            .FirstOrDefaultAsync(s => s.Id == servicoId && s.MadrinhaId == madrinha.Id);

        if (servico == null)
            return NotFound(new { mensagem = "Serviço não encontrado." });

        _context.Set<Servico>().Remove(servico);
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = "Serviço removido com sucesso!" });
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