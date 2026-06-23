using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PracticoOrmMongo.Models;

public class DetalleVenta
{
    // No necesita [BsonId] porque no es una colección propia
    [BsonElement("cantidad")]
    public int Cantidad { get; set; }

    [BsonElement("producto")]
    public Producto Producto { get; set; } = null!;
}