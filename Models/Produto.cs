using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScarFood.Models
{
    public class Produto
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string? Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        public int Vendas { get; set; }
        public List<IngredienteExtra> Extras { get; set; } = new();
    }

    public class IngredienteExtra
    {
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
    }

    public class CategoriaItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; }
    }
}