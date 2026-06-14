using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortoSeguraAPI.Data;
using PortoSeguraAPI.Dtos;
using PortoSeguraAPI.Models;

namespace PortoSeguraAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Usuaria")]
public class UsuariaController : ControllerBase
{
    private readonly UserManager<Usuaria> _userManager;
    private readonly AppDbContext _context;
    private readonly BlobStorageService _blobStorageService;

    public UsuariaController(UserManager<Usuaria> userManager, AppDbContext context, BlobStorageService blobStorageService)
    {
        _userManager = userManager;
        _context = context;
        _blobStorageService = blobStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> ObterDadosUsuaria()
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        return Ok(new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Telefone,
            usuario.Bio,
            usuario.Status,
            usuario.DataCriacao,
            FotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(usuario.FotoPerfilUrl)
        });
    }

   [HttpPut]
    public async Task<IActionResult> AtualizarDadosUsuaria([FromBody] AtualizarUsuariaRequest request)
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var possuiAtualizacao = false;

        if (!string.IsNullOrWhiteSpace(request.Nome))
        {
            usuario.Nome = request.Nome.Trim();
            possuiAtualizacao = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Telefone))
        {
            usuario.Telefone = request.Telefone.Trim();
            possuiAtualizacao = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Bio))
        {
            usuario.Bio = request.Bio.Trim();
            possuiAtualizacao = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            var emailEmUso = await _userManager.FindByEmailAsync(email);

            if (emailEmUso != null && emailEmUso.Id != usuario.Id)
            {
                return Conflict(new { mensagem = "Este e-mail já está em uso." });
            }

            await _userManager.SetEmailAsync(usuario, email);
            await _userManager.SetUserNameAsync(usuario, email);
            possuiAtualizacao = true;
        }

        if (request.FotoPerfilUrl != null)
        {
            usuario.FotoPerfilUrl = string.IsNullOrWhiteSpace(request.FotoPerfilUrl) ? null : request.FotoPerfilUrl.Trim();
            possuiAtualizacao = true;
        }

        if (!possuiAtualizacao)
        {
            return BadRequest(new { mensagem = "Informe ao menos um campo para atualização." });
        }

        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            return BadRequest(resultado.Errors);
        }

        return Ok(new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Telefone,
            usuario.Bio,
            usuario.Status,
            usuario.DataCriacao,
            FotoPerfilUrl = _blobStorageService.GerarUrlDeLeitura(usuario.FotoPerfilUrl)
        });
    }

    [HttpDelete]
    public async Task<IActionResult> DeletarContaUsuaria()
    {
        var usuario = await ObterUsuarioAutenticadoAsync();
        if (usuario == null)
        {
            return Unauthorized(new { mensagem = "Usuária não autenticada." });
        }

        var possuiSolicitacoes = await _context.Set<Solicitacao>()
            .AnyAsync(s => s.UsuariaId == usuario.Id);

        if (possuiSolicitacoes)
        {
            return Conflict(new { mensagem = "Não é possível excluir a conta enquanto existirem solicitações vinculadas." });
        }

        var resultado = await _userManager.DeleteAsync(usuario);
        if (!resultado.Succeeded)
        {
            return BadRequest(resultado.Errors);
        }

        return Ok(new { mensagem = "Conta da usuária excluída com sucesso." });
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