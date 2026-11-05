using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freela.Controllers
{
    // Abre as telas. As operações no banco ficam no controller da API.
    [Authorize]
    public class ProjetosController : Controller
    {
        public IActionResult Detalhes(int id)
        {
            return View(id);
        }

        [Authorize(Roles = "CLIENTE")]
        public IActionResult Meus()
        {
            return View();
        }

        [Authorize(Roles = "CLIENTE")]
        public IActionResult Editar(int id)
        {
            return View("~/Views/Home/Publicar.cshtml", id);
        }
    }
}