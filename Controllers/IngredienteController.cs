// Controllers/IngredientesController.cs
using Microsoft.AspNetCore.Mvc;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngredientesController : ControllerBase
{
    private readonly IngredienteService _service;

    public IngredientesController(IngredienteService service) =>
        _service = service;

    [HttpGet]
    public async Task<ActionResult<List<Ingrediente>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Ingrediente>> GetById(string id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Ingrediente ingrediente)
    {
        await _service.CreateAsync(ingrediente);
        return CreatedAtAction(nameof(GetById), new { id = ingrediente.Id }, ingrediente);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Ingrediente updated)
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