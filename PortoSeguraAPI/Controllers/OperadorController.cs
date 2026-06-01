using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using PortoSeguraAPI.Data;
using PortoSeguraAPI.Models;
using PortoSeguraAPI.Enums;

[ApiController]
[Route("api/[controller]")]
public class OperadorController : ControllerBase
{
    private readonly UserManager<Usuaria> _userManager;
    private readonly AppDbContext _context;

    public OperadorController(UserManager<Usuaria> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpPost("{id}/aprovar")]
    public async Task<IActionResult> AprovarUsuaria(int id)
    {
        var usuaria = await _userManager.FindByIdAsync(id.ToString());
        if (usuaria == null)
        {
            return NotFound(new { mensagem = "Usuária não encontrada." });
        }

        usuaria.Status = UserStatus.Ativo;
        var resultado = await _userManager.UpdateAsync(usuaria);

        if (resultado.Succeeded)
        {
            return Ok(new { mensagem = "Usuária aprovada com sucesso!" });
        }

        return BadRequest(resultado.Errors);
    }

    [HttpPost("{id}/reprovar")]
    public async Task<IActionResult> ReprovarUsuaria(int id)
    {
        var usuaria = await _userManager.FindByIdAsync(id.ToString());
        if (usuaria == null)
        {
            return NotFound(new { mensagem = "Usuária não encontrada." });
        }

        usuaria.Status = UserStatus.Rejeitado;
        var resultado = await _userManager.UpdateAsync(usuaria);

        if (resultado.Succeeded)
        {
            return Ok(new { mensagem = "Usuária reprovada com sucesso!" });
        }

        return BadRequest(resultado.Errors);
    }
}