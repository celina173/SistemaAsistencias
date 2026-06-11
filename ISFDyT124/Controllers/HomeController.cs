using Microsoft.AspNetCore.Mvc;
using ISFDyT124.Models;

namespace ISFDyT124.Controllers
{
    public class HomeController : Controller
    {
        // Pantalla de inicio -> /Home/Index
        public IActionResult Index()
        {
            return View();
        }

        // Pagina de error -> /Home/Error
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier
            });
        }
    }
}
