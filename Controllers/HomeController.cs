using Microsoft.AspNetCore.Mvc;
using ScarFood.Models;
using ScarFood.Services; // ADICIONADO: Referência ao serviço do MongoDB
using MongoDB.Driver;    // ADICIONADO: Driver do NoSQL
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace ScarFood.Controllers
{
    public class HomeController : Controller
    {
        // Substituímos os caminhos de arquivo físicos pela injeção do serviço de banco
        private readonly DatabaseService _dbService;

        public HomeController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // Funções de carregamento agora buscam diretamente no MongoDB
        private List<Pedido> CarregarPedidos()
        {
            return _dbService.Pedidos.Find(_ => true).ToList();
        }

        private List<CategoriaItem> CarregarCategorias()
        {
            return _dbService.Categorias.Find(_ => true).ToList();
        }

        public IActionResult Index()
        {
            // MANTIDO: Manda a ordem definida pelo admin para a tela do cliente
            ViewBag.Categorias = CarregarCategorias().OrderBy(c => c.Ordem).ToList();

            // Busca os produtos na coleção do NoSQL
            var produtos = _dbService.Produtos.Find(_ => true).ToList();
            return View(produtos);
        }

        public IActionResult Pedidos()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Autenticacao");

            // MANTIDO: Filtra apenas os pedidos do usuário logado e ordena pela data
            var pedidosFiltrados = _dbService.Pedidos
                .Find(p => p.UserEmail == email)
                .SortByDescending(p => p.Data)
                .ToList();

            return View(pedidosFiltrados);
        }

        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        // ==========================================
        // PROTEÇÃO ADICIONADA: Validação de Posse do Pedido no Chat
        // ==========================================

        [HttpGet]
        public IActionResult ObterChat(string pedidoId) // Alterado para string devido ao NoSQL
        {
            // 1. Verifica se o usuário está logado
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });

            // 2. PROTEÇÃO: O pedido deve existir E pertencer ao e-mail do usuário logado
            var pedido = _dbService.Pedidos.Find(p => p.Id == pedidoId && p.UserEmail == email).FirstOrDefault();

            if (pedido == null) return Json(new { success = false });

            return Json(new { success = true, mensagens = pedido.Mensagens ?? new List<MensagemChat>() });
        }

        [HttpPost]
        public IActionResult EnviarMensagemChat(string pedidoId, string texto) // Alterado para string devido ao NoSQL
        {
            // 1. Verifica se o usuário está logado
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });

            // 2. PROTEÇÃO: O pedido deve existir E pertencer ao e-mail do usuário logado
            var pedido = _dbService.Pedidos.Find(p => p.Id == pedidoId && p.UserEmail == email).FirstOrDefault();

            if (pedido != null && !string.IsNullOrWhiteSpace(texto))
            {
                if (pedido.Mensagens == null) pedido.Mensagens = new List<MensagemChat>();

                var novaMsg = new MensagemChat
                {
                    Remetente = "Cliente",
                    Texto = texto,
                    DataHora = DateTime.Now.ToString("HH:mm"),
                    StatusVisto = "Enviado"
                };

                pedido.Mensagens.Add(novaMsg);
               
                // Salva a atualização no documento específico do pedido
                _dbService.Pedidos.ReplaceOne(p => p.Id == pedidoId, pedido);

                return Json(new { success = true, mensagem = novaMsg });
            }
            return Json(new { success = false });
        }
    }
}