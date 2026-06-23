// Controllers/RecetasController.cs
using Microsoft.AspNetCore.Mvc;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecetasController : ControllerBase
{
    private readonly RecetaService _service;

    public RecetasController(RecetaService service) =>
        _service = service;

    [HttpGet]
    public async Task<ActionResult<List<Receta>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Receta>> GetById(string id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Receta receta)
    {
        await _service.CreateAsync(receta);
        return CreatedAtAction(nameof(GetById), new { id = receta.Id }, receta);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Receta updated)
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

    // Endpoints para manejar los detalles embebidos
    [HttpPost("{id}/detalles")]
    public async Task<IActionResult> AddDetalle(string id, DetalleReceta detalle)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.AddDetalleAsync(id, detalle);
        return NoContent();
    }

    [HttpPut("{id}/detalles")]
    public async Task<IActionResult> UpdateDetalles(string id, List<DetalleReceta> detalles)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.UpdateDetallesAsync(id, detalles);
        return NoContent();
    }
}