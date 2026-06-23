// Controllers/TiposProductoController.cs
using Microsoft.AspNetCore.Mvc;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TiposProductoController : ControllerBase
{
    private readonly TipoProductoService _service;

    public TiposProductoController(TipoProductoService service) =>
        _service = service;

    [HttpGet]
    public async Task<ActionResult<List<TipoProducto>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<TipoProducto>> GetById(string id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TipoProducto tipoProducto)
    {
        await _service.CreateAsync(tipoProducto);
        return CreatedAtAction(nameof(GetById), new { id = tipoProducto.Id }, tipoProducto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, TipoProducto updated)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.UpdateAsync(id, updated);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.DeleteAsync(id);
        return NoContent();
    }
}