using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PortoSeguraAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Madrinha" )]
public class UsuariaController : ControllerBase
{
    [HttpGet("teste")]
    public IActionResult Teste()
    {
        return Ok("Funcionando!");
    }
}