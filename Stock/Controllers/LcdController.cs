using Microsoft.AspNetCore.Mvc;
using Stock.Data;
using System.Net.Http.Json;

namespace Stock.Controllers
{
    public class LcdController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LcdController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProductToLcd(int productId)
        {
            var product = _context.Products
                .Where(p => p.Id == productId)
                .Select(p => new
                {
                    p.Name,
                    p.Price
                })
                .FirstOrDefault();

            if (product == null)
                return NotFound();

            using var client = new HttpClient();

            await client.PostAsJsonAsync(
                "http://192.168.1.50/display", // IP ESP32
                new
                {
                    name = product.Name,
                    price = product.Price
                });

            return RedirectToAction("Index", "Products");
        }
    }
}
