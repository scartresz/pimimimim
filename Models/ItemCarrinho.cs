namespace ScarFood.Models
{
    public class ItemCarrinho
    {
        // Alterado de int para string (e adicionado = string.Empty para sumir o aviso amarelo CS8618)
        public string Id { get; set; } = string.Empty; 
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
        public string Observacao { get; set; } = string.Empty;
        public decimal PrecoTotal => PrecoUnitario * Quantidade;
    }
}