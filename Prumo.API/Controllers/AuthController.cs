using Microsoft.AspNetCore.Mvc;

namespace Prumo.API.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
