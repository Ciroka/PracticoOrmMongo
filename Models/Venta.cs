using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PracticoOrmMongo.Models;

public class Venta
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("fechaDeVenta")]
    public DateTime FechaDeVenta { get; set; } = DateTime.UtcNow;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("mostradorId")]
    public string MostradorId { get; set; } = null!;

     // Los detalles viven adentro de la venta, no en colección propia
    [BsonElement("detalles")]
    public List<DetalleVenta> Detalles { get; set; } = [];
}