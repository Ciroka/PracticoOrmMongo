// Services/IngredienteService.cs
using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Settings;
using Microsoft.Extensions.Options;

namespace PracticoOrmMongo.Services;

public class IngredienteService
{
    private readonly IMongoCollection<Ingrediente> _collection;

    public IngredienteService(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Ingrediente>("ingredientes");
    }

    public async Task<List<Ingrediente>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<Ingrediente?> GetByIdAsync(string id) =>
        await _collection.Find(i => i.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Ingrediente ingrediente) =>
        await _collection.InsertOneAsync(ingrediente);

    public async Task UpdateAsync(string id, Ingrediente updated) =>
        await _collection.ReplaceOneAsync(i => i.Id == id, updated);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(i => i.Id == id);
}