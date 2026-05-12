using Microsoft.AspNetCore.Mvc;
using ScarFood.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System;

namespace ScarFood.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _caminhoPedidos = Path.Combine(Directory.GetCurrentDirectory(), "pedidos.json");
        private readonly string _caminhoProdutos = Path.Combine(Directory.GetCurrentDirectory(), "produtos.json");
        
        // NOVO: O Home agora sabe onde fica o arquivo de Categorias!
        private readonly string _caminhoCategorias = Path.Combine(Directory.GetCurrentDirectory(), "categorias.json"); 

        private List<Pedido> CarregarPedidos()
        {
            if (!System.IO.File.Exists(_caminhoPedidos)) return new List<Pedido>();

            var json = System.IO.File.ReadAllText(_caminhoPedidos);
            if (string.IsNullOrWhiteSpace(json)) return new List<Pedido>();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                return JsonSerializer.Deserialize<List<Pedido>>(json, options) ?? new List<Pedido>();
            }
            catch
            {
                return new List<Pedido>();
            }
        }

        private void SalvarPedidos(List<Pedido> pedidos)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(_caminhoPedidos, JsonSerializer.Serialize(pedidos, options));
        }

        // NOVO: Função para o Cardápio ler a ordem exata do Admin
        private List<CategoriaItem> CarregarCategorias()
        {
            if (!System.IO.File.Exists(_caminhoCategorias)) return new List<CategoriaItem>();
            var json = System.IO.File.ReadAllText(_caminhoCategorias);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try {
                return JsonSerializer.Deserialize<List<CategoriaItem>>(json, options) ?? new List<CategoriaItem>();
            } catch {
                return new List<CategoriaItem>();
            }
        }

        public IActionResult Index()
        {
            // MANDA A ORDEM DEFINIDA PELO ADMIN PARA A TELA DO CLIENTE!
            ViewBag.Categorias = CarregarCategorias().OrderBy(c => c.Ordem).ToList();

            if (!System.IO.File.Exists(_caminhoProdutos)) return View(new List<Produto>());
            var json = System.IO.File.ReadAllText(_caminhoProdutos);
            var produtos = JsonSerializer.Deserialize<List<Produto>>(json) ?? new List<Produto>();
            return View(produtos);
        }

        public IActionResult Pedidos()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Autenticacao");

            var todosPedidos = CarregarPedidos();
            var pedidosFiltrados = todosPedidos.Where(p => p.UserEmail == email).OrderByDescending(p => p.Data).ToList();

            return View(pedidosFiltrados);
        }

        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        [HttpGet]
        public IActionResult ObterChat(int pedidoId)
        {
            var pedidos = CarregarPedidos();
            var pedido = pedidos.FirstOrDefault(p => p.Id == pedidoId);

            if (pedido == null) return Json(new { success = false });

            return Json(new { success = true, mensagens = pedido.Mensagens ?? new List<MensagemChat>() });
        }

        [HttpPost]
        public IActionResult EnviarMensagemChat(int pedidoId, string texto)
        {
            var pedidos = CarregarPedidos();
            var pedido = pedidos.FirstOrDefault(p => p.Id == pedidoId);

            if (pedido != null && !string.IsNullOrWhiteSpace(texto))
            {
                if (pedido.Mensagens == null) pedido.Mensagens = new List<MensagemChat>();

                var novaMsg = new MensagemChat
                {
                    Remetente = "Cliente",
                    Texto = texto,
                    DataHora = System.DateTime.Now.ToString("HH:mm"),
                    StatusVisto = "Enviado"
                };

                pedido.Mensagens.Add(novaMsg);
                SalvarPedidos(pedidos);

                return Json(new { success = true, mensagem = novaMsg });
            }
            return Json(new { success = false });
        }
    }
}