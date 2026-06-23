using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PracticoOrmMongo.Models;

public class Producto
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("descripcion")]
    public string? Descripcion { get; set; }

    [BsonElement("porcentajeDeGanancia")]
    public decimal PorcentajeDeGanancia { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("recetaId")]
    public string RecetaId { get; set; } = null!;

    [BsonElement("tipoProducto")]
    public TipoProducto TipoProducto { get; set; } = null!;  // embebido
}