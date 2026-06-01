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
public class DocumentosController : ControllerBase
{
    private readonly BlobStorageService _blobStorageService;
    private readonly AppDbContext _context;
    private readonly UserManager<Usuaria> _userManager;

    public DocumentosController(BlobStorageService blobStorageService, AppDbContext context, UserManager<Usuaria> userManager)
    {
        _blobStorageService = blobStorageService;
        _context = context;
        _userManager = userManager;
    }

    [HttpPost("solicitar-upload")]
    public async Task<IActionResult> SolicitarUpload([FromBody] SolicitarUploadRequest request)
    {
        // var userId =  await ObterUsuarioAutenticadoAsync();
        // if (userId == null)
        //     return Unauthorized(new { mensagem = "Usuária não autenticada." });
        
        long tamanhoMaximoBytes = 5 * 1024 * 1024; // 5 MB
        
        var extensoesPermitidas = new Dictionary<string, string>
        {
            { "application/pdf", ".pdf" },
            { "image/jpeg", ".jpg" },
            { "image/png", ".png" },
            {"video/webm", ".webm" },
            {"video/mp4", ".mp4" }
        };

        if (!extensoesPermitidas.ContainsKey(request.TipoMime.ToLower()))
            return BadRequest(new { mensagem = "Tipo de arquivo inválido. Apenas PDF, JPG, PNG, WEBM e MP4 são permitidos." });


        if (request.TamanhoEmBytes > tamanhoMaximoBytes)
            return BadRequest(new { mensagem = "O arquivo excede o tamanho máximo permitido (5 MB)." });

        string nomeArquivo = $"{Guid.NewGuid()}{extensoesPermitidas[request.TipoMime.ToLower()]}";

        var urlDeUpload = _blobStorageService.GerarUrlDeUploadDireto(nomeArquivo);

        // var documento = new Documento
        // {
        //     UserId = userId.Id, // Substitua pelo ID do usuário autenticado
        //     Tipo = request.TipoDocumento,
        //     Url = urlDeUpload,
        //     NomeArquivo = nomeArquivo,
        //     StatusUpload = "Pendente"
        // };

        // _context.Set<Documento>().Add(documento);
        // await _context.SaveChangesAsync();

        return Ok(new { Url = urlDeUpload, NomeArquivo = nomeArquivo });
    }


    // [HttpPost("confirmar-upload")]
    // public async Task<IActionResult> ConfirmarUpload(string nomeArquivo)
    // {
    //     var documento = await _context.Set<Documento>().FirstOrDefaultAsync(d => d.NomeArquivo == nomeArquivo);
    //     if (documento == null)
    //         return NotFound(new { mensagem = "Documento não encontrado." });

    //     documento.StatusUpload = "Concluído";
    //     await _context.SaveChangesAsync();

    //     return Ok();
    // }

    // private async Task<Usuaria?> ObterUsuarioAutenticadoAsync()
    // {
    //     var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    //     if (string.IsNullOrWhiteSpace(usuarioId) || !int.TryParse(usuarioId, out var id))
    //     {
    //         return null;
    //     }

    //     return await _userManager.FindByIdAsync(id.ToString());
    // }
}