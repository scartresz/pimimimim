namespace ScarFood.Models
{
    public class ItemCarrinho
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
        public string Observacao { get; set; } = string.Empty;
        public decimal PrecoTotal => PrecoUnitario * Quantidade;
    }
}