using Microsoft.AspNetCore.Mvc;

namespace Graphene.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
