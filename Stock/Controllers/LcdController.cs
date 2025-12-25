using Microsoft.AspNetCore.Mvc;
using Stock.Data;
using System.IO.Ports;

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
		public IActionResult UpdateProductToLcd(int productId)
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

			// ⚠️ ĐỔI COM9 cho đúng máy bạn
			using (SerialPort port = new SerialPort("COM9", 9600))
			{
				port.Open();

				string data = $"{product.Name}|{product.Price}";
				port.WriteLine(data);

				port.Close();
			}

			return RedirectToAction("Index", "Products");
		}
	}
}
