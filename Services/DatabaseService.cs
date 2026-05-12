using MongoDB.Driver;
using ScarFood.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ScarFood.Services
{
    public class DatabaseService
    {
        private readonly IMongoDatabase _database;

        public DatabaseService(IOptions<DatabaseSettings> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionString);
            _database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            SeedData().Wait();
        }

        public IMongoCollection<Produto> Produtos => _database.GetCollection<Produto>("Produtos");
        public IMongoCollection<Pedido> Pedidos => _database.GetCollection<Pedido>("Pedidos");
        public IMongoCollection<Usuario> Usuarios => _database.GetCollection<Usuario>("Usuarios");
        public IMongoCollection<CategoriaItem> Categorias => _database.GetCollection<CategoriaItem>("Categorias");

        // NOVO: Adicione esta linha para o sistema reconhecer os endereços!
        public IMongoCollection<EnderecoUsuario> Enderecos => _database.GetCollection<EnderecoUsuario>("Enderecos");

        private async Task SeedData()
        {
            await ImportJson<Produto>("produtos.json", Produtos);
            await ImportJson<Pedido>("pedidos.json", Pedidos);
            await ImportJson<Usuario>("usuarios.json", Usuarios);
            await ImportJson<CategoriaItem>("categorias.json", Categorias);
        }

        private async Task ImportJson<T>(string fileName, IMongoCollection<T> collection) where T : class
        {
            if (await collection.CountDocumentsAsync(_ => true) == 0)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                if (File.Exists(path))
                {
                    var json = await File.ReadAllTextAsync(path);
                    var lista = JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (lista != null && lista.Count > 0) await collection.InsertManyAsync(lista);
                }
            }
        }
    }
}