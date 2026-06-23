// Services/ProductoService.cs
using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Settings;
using Microsoft.Extensions.Options;

namespace PracticoOrmMongo.Services;

public class ProductoService
{
    private readonly IMongoCollection<Producto> _collection;

    public ProductoService(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Producto>("productos");
    }

    public async Task<List<Producto>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<Producto?> GetByIdAsync(string id) =>
        await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Producto producto) =>
        await _collection.InsertOneAsync(producto);

    public async Task UpdateAsync(string id, Producto updated) =>
        await _collection.ReplaceOneAsync(p => p.Id == id, updated);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(p => p.Id == id);
}