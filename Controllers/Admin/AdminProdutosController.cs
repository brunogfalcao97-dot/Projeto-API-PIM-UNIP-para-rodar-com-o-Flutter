using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PimTintas.Api.Data;
using PimTintas.Api.Dtos;
using PimTintas.Api.Extensions;
using PimTintas.Api.Models;

namespace PimTintas.Api.Controllers.Admin;

/// <summary>Produtos (latas), preço e estoque. Só administradores. "Excluir" apenas desativa.</summary>
[ApiController]
[Route("api/admin/produtos")]
[Authorize(Roles = Perfis.Admin)]
public class AdminProdutosController(AppDbContext db) : ControllerBase
{
    /// <summary>Exige Marca e TipoTinta carregados (Include).</summary>
    private static ProdutoAdminDto Mapear(Produto p) =>
        new(p.Id, p.Sku, p.Descricao(), p.MarcaId, p.TipoTintaId, p.VolumeLitros, p.Preco, p.Estoque, p.Inativo);

    private Task<Produto?> Carregar(int id, CancellationToken ct) =>
        db.Produtos.Include(p => p.Marca).Include(p => p.TipoTinta).FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>Confere os campos que dependem do banco ou de regra de negócio. Devolve a mensagem de erro, se houver.</summary>
    private async Task<string?> Validar(ProdutoRequest r, int? idAtual, CancellationToken ct)
    {
        if (decimal.Round(r.VolumeLitros * 10m) != r.VolumeLitros * 10m)
            return "O volume deve ser múltiplo de 0,1 litro (ex.: 0,9; 3,6; 18).";

        if (!await db.Marcas.AnyAsync(m => m.Id == r.MarcaId, ct)) return "Marca não encontrada.";
        if (!await db.TiposTinta.AnyAsync(t => t.Id == r.TipoTintaId, ct)) return "Tipo de tinta não encontrado.";

        var sku = r.Sku.Trim();
        if (await db.Produtos.AnyAsync(p => p.Sku == sku && p.Id != idAtual, ct))
            return "Já existe um produto com esse SKU.";

        return null;
    }

    private static void Aplicar(Produto produto, ProdutoRequest r)
    {
        produto.Sku = r.Sku.Trim();
        produto.MarcaId = r.MarcaId;
        produto.TipoTintaId = r.TipoTintaId;
        produto.VolumeLitros = r.VolumeLitros;
        produto.Preco = r.Preco;
        produto.Estoque = r.Estoque;
        produto.Inativo = r.Inativo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoAdminDto>>> Listar(CancellationToken ct)
    {
        var produtos = await db.Produtos
            .AsNoTracking()
            .Include(p => p.Marca)
            .Include(p => p.TipoTinta)
            .OrderBy(p => p.Marca.Nome).ThenBy(p => p.TipoTinta.Nome).ThenBy(p => p.VolumeLitros)
            .ToListAsync(ct);

        return Ok(produtos.Select(Mapear));
    }

    [HttpPost]
    public async Task<IActionResult> Criar(ProdutoRequest requisicao, CancellationToken ct)
    {
        var erro = await Validar(requisicao, null, ct);
        if (erro is not null) return Problem(detail: erro, statusCode: StatusCodes.Status400BadRequest);

        var produto = new Produto();
        Aplicar(produto, requisicao);
        db.Produtos.Add(produto);
        await db.SaveChangesAsync(ct);

        var criado = await Carregar(produto.Id, ct);
        return Created($"/api/admin/produtos/{produto.Id}", Mapear(criado!));
    }

    /// <summary>Atualiza dados, preço e estoque. O estoque informado substitui o atual.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, ProdutoRequest requisicao, CancellationToken ct)
    {
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (produto is null) return NotFound();

        var erro = await Validar(requisicao, id, ct);
        if (erro is not null) return Problem(detail: erro, statusCode: StatusCodes.Status400BadRequest);

        Aplicar(produto, requisicao);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(
                detail: "O produto foi alterado (por exemplo, por uma venda) enquanto você editava. Recarregue e tente de novo.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var atualizado = await Carregar(id, ct);
        return Ok(Mapear(atualizado!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (produto is null) return NotFound();

        produto.Inativo = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
