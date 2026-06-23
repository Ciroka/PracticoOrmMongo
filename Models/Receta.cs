using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PracticoOrmMongo.Models;

public class Receta
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    // Los detalles viven adentro de la receta, no en colección propia
    [BsonElement("detalles")]
    public List<DetalleReceta> Detalles { get; set; } = [];
}

