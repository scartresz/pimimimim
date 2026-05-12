using System.Collections.Generic;

namespace ScarFood.Models
{
    public class Produto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        public int Vendas { get; set; }

        // ADICIONADO: Lista de ingredientes extras que esse lanche aceita
        public List<IngredienteExtra> Extras { get; set; } = new();
    }

    // NOVA CLASSE: Define o que é o ingrediente e quanto ele custa
    public class IngredienteExtra
    {
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
    }

    // NOVA CLASSE: Para o sistema salvar as suas categorias ordenadas
    public class CategoriaItem
    {
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; }
    }
}