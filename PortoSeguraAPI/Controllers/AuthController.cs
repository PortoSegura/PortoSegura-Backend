using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PortoSeguraAPI.Dtos;
using PortoSeguraAPI.Enums;
using PortoSeguraAPI.Data;
using PortoSeguraAPI.Models;

namespace PortoSeguraAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<Usuaria> _userManager;
    private readonly TokenService _tokenService;
    private readonly AppDbContext _context;

    public AuthController(UserManager<Usuaria> userManager, TokenService tokenService, AppDbContext context)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
    }

    [HttpPost("cadastrar-usuaria")]
    public async Task<IActionResult> CadastrarUsuaria([FromBody] CadastroUsuariaRequest dto)
    {
        var usuarioExistente = await _userManager.FindByEmailAsync(dto.Email);
        if (usuarioExistente != null)
        {
            return BadRequest(new { mensagem = "Este e-mail já está em uso." });
        }

        var novoUsuario = new Usuaria
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            UserName = dto.Email,
            Bio = dto.Bio,
            Status = UserStatus.Pendente,
            DataCriacao = DateTime.UtcNow,
            Estado = dto.Estado,
            Cidade = dto.Cidade,
            VideoVerificacao = dto.VideoVerificacao,
            urlLinkedin = dto.Linkedin ?? string.Empty,
            urlInstagram = dto.Instagram ?? string.Empty,
            urlFacebook = dto.Facebook ?? string.Empty
        };

        var resultado = await _userManager.CreateAsync(novoUsuario, dto.Senha);

        if (resultado.Succeeded)
        {
            await _userManager.AddToRoleAsync(novoUsuario, "Usuaria");
            return Ok(new { mensagem = "Cadastro realizado com sucesso! Aguardando aprovação." });
        }

        return BadRequest(resultado.Errors);
    }

    [HttpPost("cadastrar-madrinha")]
    public async Task<IActionResult> CadastrarMadrinha([FromBody] CadastroMadrinhaRequest dto)
    {
        var usuarioExistente = await _userManager.FindByEmailAsync(dto.Email);
        if (usuarioExistente != null)
        {
            return BadRequest(new { mensagem = "Este e-mail já está em uso." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var novaUsuario = new Usuaria
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            UserName = dto.Email,
            Bio = dto.Bio,
            Status = UserStatus.Pendente,
            DataCriacao = DateTime.UtcNow,
            Estado = dto.Estado,
            Cidade = dto.Cidade,
            VideoVerificacao = dto.VideoVerificacao,
            urlLinkedin = dto.Linkedin ?? string.Empty,
            urlInstagram = dto.Instagram ?? string.Empty,
            urlFacebook = dto.Facebook ?? string.Empty
        };

        var resultadoUsuario = await _userManager.CreateAsync(novaUsuario, dto.Senha);
        if (!resultadoUsuario.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(resultadoUsuario.Errors);
        }

        var resultadoRoleUsuaria = await _userManager.AddToRoleAsync(novaUsuario, "Usuaria");
        if (!resultadoRoleUsuaria.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(resultadoRoleUsuaria.Errors);
        }

        var resultadoRoleMadrinha = await _userManager.AddToRoleAsync(novaUsuario, "Madrinha");
        if (!resultadoRoleMadrinha.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(resultadoRoleMadrinha.Errors);
        }

        var madrinha = new Madrinha
        {
            UsuarioID = novaUsuario.Id,
            PrecoDiaria = dto.PrecoDiaria,
            Motivacao = dto.Motivacao,
            VerificadoIdentidade = false,
            VerificadoResidencia = false,
            TrilhaCursoCompleto = false,
            DataCriacao = DateTime.UtcNow,
            Usuario = novaUsuario
        };

        _context.Add(madrinha);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            mensagem = "Cadastro de madrinha realizado com sucesso! Aguardando aprovação."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        // 1. Busca o usuário pelo e-mail
        var usuario = await _userManager.FindByEmailAsync(dto.Email);
        if (usuario == null)
            return Unauthorized(new { mensagem = "E-mail ou senha incorretos." });

        // 2. Verifica a senha
        var senhaValida = await _userManager.CheckPasswordAsync(usuario, dto.Senha);
        if (!senhaValida)
            return Unauthorized(new { mensagem = "E-mail ou senha incorretos." });

        // 3. AVALIA A SUA REGRA DE NEGÓCIO (Aprovação)
        if (usuario.Status != UserStatus.Ativo)
            return StatusCode(403, new { mensagem = "Sua conta aguarda a aprovação de um operador." });

        var token = await _tokenService.GenerateToken(usuario);
        var roles = await _userManager.GetRolesAsync(usuario);

        return Ok(new
        {
            token,
            usuario = new
            {
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                usuario.Telefone,
                usuario.Bio,
                usuario.Estado,
                usuario.Cidade,
                usuario.urlLinkedin,
                usuario.urlInstagram,
                usuario.urlFacebook,
                Roles = roles.ToList()
            }
        });

    }

    [HttpPost("esqueci-senha")]
    public async Task<IActionResult> EsqueciSenha([FromQuery] string email)
    {
        var usuario = await _userManager.FindByEmailAsync(email);
        if (usuario == null)
            return BadRequest(new { mensagem = "E-mail não encontrado." });

        // Aqui você pode gerar um token de reset de senha e enviar por e-mail
        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

        // TODO: Integrar com serviço de e-mail real
        Console.WriteLine($"Token de reset para {email}: {token}");

        return Ok(new { mensagem = "Se o e-mail existir, um link para resetar a senha foi enviado." });
    }

    [HttpPost("resetar-senha")]
    public async Task<IActionResult> ResetarSenha([FromBody] ResetarSenhaRequest dto)
    {
        var usuario = await _userManager.FindByEmailAsync(dto.Email);
        if (usuario == null)
            return BadRequest(new { mensagem = "Requisição inválida." });

        var resultado = await _userManager.ResetPasswordAsync(usuario, dto.Token, dto.NovaSenha);
        if (resultado.Succeeded)
            return Ok(new { mensagem = "Senha resetada com sucesso!" });

        return BadRequest(resultado.Errors);
    }
}