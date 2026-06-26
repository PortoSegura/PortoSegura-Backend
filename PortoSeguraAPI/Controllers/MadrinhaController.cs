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
    private readonly BlobStorageService _blobStorageService;

    public MadrinhaController(AppDbContext context, IWebHostEnvironment environment, BlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ObterMadrinhas([FromQuery] string? destino, [FromQuery] decimal? precoMaximo ) {

        var madrinhasList = await _context.Set<Madrinha>()
            .Include(m => m.Usuario)
            .Include(m => m.Servicos)
            .Include(m => m.Solicitacoes)
            .Where(m => (string.IsNullOrEmpty(destino) || m.Usuario.Cidade.Contains(destino)) && (!precoMaximo.HasValue || m.PrecoDiaria <= precoMaximo))
            .Select(m => new
            {
                m.Id,
                m.PrecoDiaria,
                RawFotoPerfilUrl = m.Usuario.FotoPerfilUrl,
                m.Motivacao,
                UsuarioId = m.Usuario.Id,
                Nome = m.Usuario.Nome,
                Cidade = m.Usuario.Cidade,
                Estado = m.Usuario.Estado,
                Servicos = m.Servicos.Select(s => s.Descricao).ToList(),
                QtdSolicitacoes = m.Solicitacoes.Count(s => s.Status == "Concluida" || s.Status == "Avaliada"),
                MediaAvaliacao = m.Avaliacoes.Any() ? m.Avaliacoes.Average(a => a.Nota) : 0
            })
            .ToListAsync();

        var madrinhas = madrinhasList.Select(m => new MadrinhaSummaryDto
        {
            Id = m.Id,
            PrecoDiaria = m.PrecoDiaria,
            FotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(m.RawFotoPerfilUrl),
            Motivacao = m.Motivacao,
            UsuarioId = m.UsuarioId,
            Nome = m.Nome,
            Cidade = m.Cidade,
            Estado = m.Estado,
            Servicos = m.Servicos,
            QtdSolicitacoes = m.QtdSolicitacoes,
            MediaAvaliacao = m.MediaAvaliacao
        }).ToList();

        return Ok(madrinhas);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> ObterMadrinhaPorId(int id) {

        var madrinhaDb = await _context.Set<Madrinha>()
            .Where(m => m.Id == id)
            .Select(m => new
            {
                m.Id,
                m.PrecoDiaria,
                RawFotoPerfilUrl = m.Usuario.FotoPerfilUrl,
                m.Motivacao,
                UsuarioId = m.Usuario.Id,
                Nome = m.Usuario.Nome,
                Cidade = m.Usuario.Cidade,
                Estado = m.Usuario.Estado,
                Bio = m.Usuario.Bio,
                Servicos = m.Servicos.Select(s => s.Descricao).ToList(),
                QtdSolicitacoes = m.Solicitacoes.Count(s => s.Status == "Concluida" || s.Status == "Avaliada"),
                MediaAvaliacao = m.Avaliacoes.Any() ? m.Avaliacoes.Average(a => a.Nota) : 0,
                Avaliacoes = m.Avaliacoes.Select(a => new AvaliacaoSummaryDto
                {
                    Id = a.Id,
                    Nota = a.Nota,
                    Comentario = a.Comentario ?? string.Empty,
                    DataCriacao = a.DataCriacao,
                    NomeUsuaria = a.Usuaria.Nome,
                    ServicoTipo = a.ServicoTipo
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (madrinhaDb == null)
            return NotFound(new { mensagem = "Madrinha não encontrada." });

        // Calcular estatísticas categorizadas por serviço
        var mediaPorServico = new System.Collections.Generic.Dictionary<string, double>();
        var qtdPorServico = new System.Collections.Generic.Dictionary<string, int>();

        var servicosAtivos = new[] { "Dicas Locais", "Ligação/Suporte", "Busca no Aeroporto", "Acompanhamento Presencial" };
        foreach (var s in servicosAtivos)
        {
            var avs = madrinhaDb.Avaliacoes.Where(a => !string.IsNullOrEmpty(a.ServicoTipo) && a.ServicoTipo.Equals(s, StringComparison.OrdinalIgnoreCase)).ToList();
            if (avs.Any())
            {
                mediaPorServico[s] = Math.Round(avs.Average(a => a.Nota), 1);
                qtdPorServico[s] = avs.Count;
            }
            else
            {
                mediaPorServico[s] = 0;
                qtdPorServico[s] = 0;
            }
        }

        var madrinha = new MadrinhaSummaryDto
        {
            Id = madrinhaDb.Id,
            PrecoDiaria = madrinhaDb.PrecoDiaria,
            FotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(madrinhaDb.RawFotoPerfilUrl),
            Motivacao = madrinhaDb.Motivacao,
            UsuarioId = madrinhaDb.UsuarioId,
            Bio = madrinhaDb.Bio,
            Nome = madrinhaDb.Nome,
            Cidade = madrinhaDb.Cidade,
            Estado = madrinhaDb.Estado,
            Servicos = madrinhaDb.Servicos,
            QtdSolicitacoes = madrinhaDb.QtdSolicitacoes,
            MediaAvaliacao = madrinhaDb.MediaAvaliacao,
            Avaliacoes = madrinhaDb.Avaliacoes,
            MediaPorServico = mediaPorServico,
            QtdPorServico = qtdPorServico
        };

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

        var madrinhaDb = await _context.Set<Madrinha>()
            .AsNoTracking()
            .Where(m => m.UsuarioID == usuariaAutenticada.Id)
            .Select(m => new 
            {
                Id = m.Id,
                PrecoDiaria = m.PrecoDiaria,
                RawFotoPerfilUrl = m.Usuario.FotoPerfilUrl,
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
                TimeLocalNome = m.TimeLocal != null ? m.TimeLocal.Nome : "Nenhum",
                AtivaFilaAlocacao = m.AtivaFilaAlocacao,
                SlaMinutos = m.SlaMinutos,
                Disponivel = m.Disponivel,
                CargaAtendimentosAtivos = m.CargaAtendimentosAtivos,
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

        if (madrinhaDb == null)
            return NotFound(new { mensagem = "Perfil de madrinha não encontrado." });

        var madrinha = new
        {
            madrinhaDb.Id,
            madrinhaDb.PrecoDiaria,
            FotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(madrinhaDb.RawFotoPerfilUrl),
            madrinhaDb.Motivacao,
            madrinhaDb.UsuarioId,
            madrinhaDb.Nome,
            madrinhaDb.Cidade,
            madrinhaDb.Estado,
            madrinhaDb.Bio,
            madrinhaDb.Telefone,
            madrinhaDb.Email,
            madrinhaDb.Linkedin,
            madrinhaDb.Instagram,
            madrinhaDb.Facebook,
            madrinhaDb.TimeLocalNome,
            madrinhaDb.AtivaFilaAlocacao,
            madrinhaDb.SlaMinutos,
            madrinhaDb.Disponivel,
            madrinhaDb.CargaAtendimentosAtivos,
            madrinhaDb.Solicitacaoes,
            madrinhaDb.Servicos,
            madrinhaDb.QtdSolicitacoes
        };

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

    [HttpPut("profile/disponibilidade")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> AlternarDisponibilidade()
    {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();
        if (usuariaAutenticada == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var madrinha = await _context.Set<Madrinha>()
            .FirstOrDefaultAsync(m => m.UsuarioID == usuariaAutenticada.Id);

        if (madrinha == null)
            return NotFound(new { mensagem = "Perfil de madrinha não encontrado." });

        madrinha.Disponivel = !madrinha.Disponivel;
        _context.Update(madrinha);
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = "Disponibilidade alterada com sucesso!", disponivel = madrinha.Disponivel });
    }

    [HttpPut("profile/reativar-fila")]
    [Authorize(Roles = "Madrinha")]
    public async Task<IActionResult> ReativarFila()
    {
        var usuariaAutenticada = await ObterUsuarioAutenticadoAsync();
        if (usuariaAutenticada == null)
            return Unauthorized(new { mensagem = "Usuária não autenticada." });

        var madrinha = await _context.Set<Madrinha>()
            .FirstOrDefaultAsync(m => m.UsuarioID == usuariaAutenticada.Id);

        if (madrinha == null)
            return NotFound(new { mensagem = "Perfil de madrinha não encontrado." });

        madrinha.AtivaFilaAlocacao = true;
        _context.Update(madrinha);
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = "Fila de alocação automática reativada com sucesso!", ativaFilaAlocacao = madrinha.AtivaFilaAlocacao });
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