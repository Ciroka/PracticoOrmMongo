// Services/TipoProductoService.cs
using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Settings;
using Microsoft.Extensions.Options;

namespace PracticoOrmMongo.Services;

public class TipoProductoService
{
    private readonly IMongoCollection<TipoProducto> _collection;

    public TipoProductoService(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<TipoProducto>("tiposProducto");
    }

    public async Task<List<TipoProducto>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<TipoProducto?> GetByIdAsync(string id) =>
        await _collection.Find(t => t.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(TipoProducto tipoProducto) =>
        await _collection.InsertOneAsync(tipoProducto);

    public async Task UpdateAsync(string id, TipoProducto updated) =>
        await _collection.ReplaceOneAsync(t => t.Id == id, updated);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(t => t.Id == id);
}