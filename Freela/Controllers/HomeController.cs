// Este controller só entrega as TELAS (Views) e controla o acesso por tipo de conta.
// Os dados de cada tela são buscados pelo JavaScript nas APIs (Controllers/*Controller com [ApiController]).
using System.Diagnostics;
using Freela.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freela.Controllers
{
    public class HomeController : Controller
    {
        // Página inicial:
        // - freelancer logado -> vai para as vagas
        // - cliente logado    -> vai para os candidatos
        // - ninguém logado    -> mostra a tela de boas-vindas (Views/Home/Index.cshtml)
        public IActionResult Index()
        {
            if (User.IsInRole("FREELANCER")) return RedirectToAction("Vagas");
            if (User.IsInRole("CLIENTE")) return RedirectToAction("Candidatos");

            return View();
        }

        // ----- Só FREELANCER -----

        [Authorize(Roles = "FREELANCER")]
        public IActionResult Vagas()
        {
            return View();
        }

        [Authorize(Roles = "FREELANCER")]
        public IActionResult Candidaturas()
        {
            return View();
        }

        // ----- Só CLIENTE -----

        [Authorize(Roles = "CLIENTE")]
        public IActionResult Candidatos()
        {
            return View();
        }

        [Authorize(Roles = "CLIENTE")]
        public IActionResult Publicar()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}