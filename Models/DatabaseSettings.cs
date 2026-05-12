namespace ScarFood.Models
{
    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string ProdutosCollectionName { get; set; } = null!;
        public string PedidosCollectionName { get; set; } = null!;
        public string UsuariosCollectionName { get; set; } = null!;
        public string CategoriasCollectionName { get; set; } = null!;
    }
}