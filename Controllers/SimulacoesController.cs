using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;
using PimTintas.Api.Models;

namespace PimTintas.Api.Controllers;

/// <summary>Projetos salvos do cliente: a foto já pintada, a cor e o tipo de tinta. Tudo é privado do dono.</summary>
[ApiController]
[Route("api/simulacoes")]
[Authorize]
public class SimulacoesController(AppDbContext db, IWebHostEnvironment ambiente) : ControllerBase
{
    private const long TamanhoMaximoBytes = 10 * 1024 * 1024; // 10 MB

    // Novas imagens vão para App_Data, que NÃO é servida como arquivo estático: só sai pelo endpoint abaixo.
    private string PastaPrivada => Path.Combine(ambiente.ContentRootPath, "App_Data");

    // Imagens salvas pela versão anterior da API (uploads/...) continuam lá, mas também só saem pelo endpoint.
    private string PastaLegada => Path.Combine(ambiente.ContentRootPath, "wwwroot");

    private static SimulacaoDto Mapear(Simulacao s) =>
        new(s.Id, s.CriadoEm, s.Nome, s.AreaM2, s.CorId, s.Cor.Nome, s.Cor.CodigoHex,
            s.TipoTintaId, s.TipoTinta.Nome, $"/api/simulacoes/{s.Id}/imagem");

    /// <summary>Projetos do cliente logado (mais recentes primeiro).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SimulacaoDto>>> Listar(CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var lista = await db.Simulacoes
            .AsNoTracking()
            .Include(s => s.Cor)
            .Include(s => s.TipoTinta)
            .Where(s => s.UsuarioId == usuarioId)
            .OrderByDescending(s => s.CriadoEm)
            .ToListAsync(ct);

        return Ok(lista.Select(Mapear));
    }

    /// <summary>A foto do projeto. Só o dono recebe; para os outros é 404.</summary>
    [HttpGet("{id:int}/imagem")]
    public async Task<IActionResult> Imagem(int id, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var caminho = await db.Simulacoes
            .AsNoTracking()
            .Where(s => s.Id == id && s.UsuarioId == usuarioId)
            .Select(s => s.CaminhoImagem)
            .FirstOrDefaultAsync(ct);
        if (caminho is null) return NotFound();

        var arquivo = ResolverArquivo(caminho);
        if (arquivo is null || !System.IO.File.Exists(arquivo)) return NotFound();

        Response.Headers.CacheControl = "private, max-age=3600";
        var tipo = arquivo.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
        return PhysicalFile(arquivo, tipo);
    }

    /// <summary>Salva o projeto: a escolha do cliente e a foto já pintada (multipart/form-data).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12_000_000)]
    [ProducesResponseType<SimulacaoDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromForm] CriarSimulacaoRequest requisicao, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var arquivo = requisicao.Imagem;
        if (arquivo.Length == 0 || arquivo.Length > TamanhoMaximoBytes)
            return Problem(detail: "A imagem deve ter até 10 MB.", statusCode: StatusCodes.Status400BadRequest);

        var extensao = await DetectarExtensaoAsync(arquivo, ct);
        if (extensao is null)
            return Problem(detail: "Envie uma imagem JPEG ou PNG.", statusCode: StatusCodes.Status400BadRequest);

        var cor = await db.Cores.AsNoTracking().FirstOrDefaultAsync(c => c.Id == requisicao.CorId && !c.Inativa, ct);
        if (cor is null)
            return Problem(detail: "Cor não encontrada.", statusCode: StatusCodes.Status400BadRequest);

        var tipo = await db.TiposTinta.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == requisicao.TipoTintaId && !t.Inativo, ct);
        if (tipo is null)
            return Problem(detail: "Tipo de tinta não encontrado.", statusCode: StatusCodes.Status400BadRequest);

        // O nome do arquivo é sempre gerado aqui: nunca usamos o nome enviado pelo cliente.
        var pasta = Path.Combine(PastaPrivada, "simulacoes");
        Directory.CreateDirectory(pasta);

        var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(pasta, nomeArquivo);

        await using (var destino = new FileStream(caminhoCompleto, FileMode.CreateNew))
        {
            await arquivo.CopyToAsync(destino, ct);
        }

        var simulacao = new Simulacao
        {
            UsuarioId = usuarioId,
            CorId = cor.Id,
            TipoTintaId = tipo.Id,
            Nome = string.IsNullOrWhiteSpace(requisicao.Nome) ? null : requisicao.Nome.Trim(),
            AreaM2 = requisicao.AreaM2,
            CaminhoImagem = $"simulacoes/{nomeArquivo}",
        };

        try
        {
            db.Simulacoes.Add(simulacao);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            // Não deixa imagem órfã se o banco falhar.
            if (System.IO.File.Exists(caminhoCompleto)) System.IO.File.Delete(caminhoCompleto);
            throw;
        }

        simulacao.Cor = cor;
        simulacao.TipoTinta = tipo;
        return Created($"/api/simulacoes/{simulacao.Id}", Mapear(simulacao));
    }

    /// <summary>Apaga o projeto e a foto.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        if (User.ObterUsuarioId() is not int usuarioId) return Unauthorized();

        var simulacao = await db.Simulacoes.FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuarioId, ct);
        if (simulacao is null) return NotFound();

        db.Simulacoes.Remove(simulacao);
        await db.SaveChangesAsync(ct);

        try
        {
            var arquivo = ResolverArquivo(simulacao.CaminhoImagem);
            if (arquivo is not null && System.IO.File.Exists(arquivo)) System.IO.File.Delete(arquivo);
        }
        catch (IOException)
        {
            // O registro já foi apagado; sobrar um arquivo solto não afeta o cliente.
        }

        return NoContent();
    }

    /// <summary>
    /// Transforma o caminho guardado no banco em um arquivo do disco, garantindo que ele esteja
    /// dentro das pastas permitidas (impede caminhos com "..").
    /// </summary>
    private string? ResolverArquivo(string caminho)
    {
        var relativo = caminho.Replace('\\', '/').TrimStart('/');
        var raiz = relativo.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase) ? PastaLegada : PastaPrivada;

        var completo = Path.GetFullPath(Path.Combine(raiz, relativo));
        var raizCompleta = Path.GetFullPath(raiz) + Path.DirectorySeparatorChar;

        return completo.StartsWith(raizCompleta, StringComparison.OrdinalIgnoreCase) ? completo : null;
    }

    /// <summary>Confere os primeiros bytes do arquivo (não confia só na extensão enviada).</summary>
    private static async Task<string?> DetectarExtensaoAsync(IFormFile arquivo, CancellationToken ct)
    {
        var cabecalho = new byte[8];
        await using var fluxo = arquivo.OpenReadStream();
        var lidos = await fluxo.ReadAsync(cabecalho.AsMemory(0, cabecalho.Length), ct);

        // JPEG: FF D8 FF
        if (lidos >= 3 && cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF)
            return ".jpg";

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (lidos >= 8
            && cabecalho[0] == 0x89 && cabecalho[1] == 0x50 && cabecalho[2] == 0x4E && cabecalho[3] == 0x47
            && cabecalho[4] == 0x0D && cabecalho[5] == 0x0A && cabecalho[6] == 0x1A && cabecalho[7] == 0x0A)
            return ".png";

        return null;
    }
}
