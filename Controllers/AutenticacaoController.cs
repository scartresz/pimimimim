using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ScarFood.Models;
using ScarFood.Services; // Referência ao serviço NoSQL
using MongoDB.Driver;    // Driver do MongoDB
using System.Collections.Generic;
using System.Linq;
using System;

namespace ScarFood.Controllers
{
    public class AutenticacaoController : Controller
    {
        private readonly DatabaseService _dbService;

        public AutenticacaoController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet] public IActionResult Login() => View();
        [HttpGet] public IActionResult Registro() => View();

        [HttpPost]
        [ValidateAntiForgeryToken] // ADICIONADO: Proteção contra ataques CSRF
        public IActionResult Registro(Usuario novoUsuario)
        {
            if (ModelState.IsValid)
            {
                // Verifica se o e-mail já existe na coleção do MongoDB
                if (_dbService.Usuarios.Find(u => u.Email.ToLower() == novoUsuario.Email.ToLower()).Any())
                {
                    ModelState.AddModelError("Email", "Este e-mail já está cadastrado.");
                    return View(novoUsuario);
                }

                // O NoSQL gera o ID automaticamente ao inserir
                _dbService.Usuarios.InsertOne(novoUsuario);

                return RedirectToAction("Login");
            }
            return View(novoUsuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // ADICIONADO: Proteção contra ataques CSRF
        public IActionResult Login(Usuario usuario)
        {
            // Lógica do Admin
            if (usuario.Email == "admin@scarfood.com" && usuario.Senha == "admin123")
            {
                HttpContext.Session.SetString("UserEmail", "admin@scarfood.com");
                HttpContext.Session.SetString("UserNome", "Administrador");
                return RedirectToAction("Index", "Home");
            }

            // Busca o usuário válido na coleção NoSQL
            var userValido = _dbService.Usuarios.Find(u => u.Email.ToLower() == usuario.Email.ToLower() && u.Senha == usuario.Senha).FirstOrDefault();

            if (userValido != null)
            {
                HttpContext.Session.SetString("UserEmail", userValido.Email);
                HttpContext.Session.SetString("UserNome", userValido.Nome);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
            return View(usuario);
        }

        [HttpGet]
        public IActionResult Perfil()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            if (email == "admin@scarfood.com")
            {
                return View(new Usuario
                {
                    Nome = "Administrador",
                    Email = email,
                    Cpf = "000.000.000-00",
                    Telefone = "(00) 00000-0000"
                });
            }

            // Busca os dados do perfil diretamente no MongoDB
            var usuario = _dbService.Usuarios.Find(u => u.Email == email).FirstOrDefault();

            if (usuario == null) return RedirectToAction("Sair");

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // ADICIONADO: Proteção contra ataques CSRF
        public IActionResult AtualizarPerfil(Usuario model)
        {
            var emailSessao = HttpContext.Session.GetString("UserEmail");
           
            // Localiza o usuário no banco NoSQL usando a SESSÃO (Excelente contra IDOR!)
            var usuario = _dbService.Usuarios.Find(u => u.Email == emailSessao).FirstOrDefault();

            if (usuario != null)
            {
                if (!string.IsNullOrEmpty(model.Senha))
                {
                    if (model.SenhaAtual != usuario.Senha)
                    {
                        ModelState.AddModelError("SenhaAtual", "Senha atual incorreta.");
                        return View("Perfil", usuario);
                    }
                    usuario.Senha = model.Senha;
                }

                usuario.Nome = model.Nome;
                usuario.Cpf = model.Cpf;
                usuario.Telefone = model.Telefone;
                usuario.Cep = model.Cep;
                usuario.Logradouro = model.Logradouro;
                usuario.Numero = model.Numero;
                usuario.Complemento = model.Complemento;

                // Atualiza o documento no MongoDB
                _dbService.Usuarios.ReplaceOne(u => u.Email == emailSessao, usuario);

                HttpContext.Session.SetString("UserNome", usuario.Nome);
                ViewBag.Mensagem = "Perfil atualizado com sucesso!";
                return View("Perfil", usuario);
            }
            return RedirectToAction("Login");
        }

        public IActionResult Sair()
        {
            HttpContext.Session.Clear(); // Perfeito! Esvazia a sessão inteira.
            return RedirectToAction("Index", "Home");
        }
    }
}