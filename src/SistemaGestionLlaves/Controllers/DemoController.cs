using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestionLlaves.Services;

namespace SistemaGestionLlaves.Controllers
{
    [Authorize(Roles = "Administrador")]
    [Route("demo")]
    public class DemoController : Controller
    {
        private readonly DemoDataSeeder _seeder;

        public DemoController(DemoDataSeeder seeder)
        {
            _seeder = seeder;
        }

        [HttpGet("seed")]
        public async Task<IActionResult> Seed()
        {
            var result = await _seeder.SeedAsync();
            return Ok(result);
        }
    }
}
