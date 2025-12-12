using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting; 
using Microsoft.AspNetCore.Http;   
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stock.Data;
using Stock.Models;
using Stock.Services;
using System.IO;                    

namespace Stock.Controllers
{
	public class ProductsController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly QRService _qrService;
		private readonly IWebHostEnvironment _webHostEnvironment; 

		public ProductsController(ApplicationDbContext context, QRService qrService, IWebHostEnvironment webHostEnvironment)
		{
			_context = context;
			_qrService = qrService;
			_webHostEnvironment = webHostEnvironment;
		}

		// 1. DANH SÁCH
		public async Task<IActionResult> Index()
		{
			ViewBag.BaseUrl = _qrService.GetBaseUrl();
			return View(await _context.Products.ToListAsync());
		}

		// 2. CHI TIẾT
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();
			var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
			if (product == null) return NotFound();
			return View(product);
		}

		// ==================================================
		// 3. THÊM MỚI (ADMIN)
		// ==================================================
		[Authorize(Roles = "Admin")]
		public IActionResult Create()
		{
			// Lấy danh sách sản phẩm đầy đủ thông tin để làm dữ liệu đổ vào form
			var existingProducts = _context.Products
								   .Select(p => new
								   {
									   p.Name,
									   p.Code,
									   p.Quantity,
									   p.Price,
									   p.Description
								   })
								   .OrderBy(p => p.Name)
								   .ToList();

			ViewBag.ExistingProducts = existingProducts;
			return View();
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
		{
			if (ModelState.IsValid)
			{
				try
				{
					// Kiểm tra trùng mã
					if (await _context.Products.AnyAsync(p => p.Code == product.Code))
					{
						TempData["Error"] = $"Mã sản phẩm '{product.Code}' đã tồn tại!";
						return View(product);
					}

					// Xử lý ảnh upload
					if (imageFile != null && imageFile.Length > 0)
					{
						string extension = Path.GetExtension(imageFile.FileName);
						string newFileName = product.Code + extension; // Tự đặt tên theo mã SP

						string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
						if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

						string filePath = Path.Combine(uploadFolder, newFileName);

						using (var fileStream = new FileStream(filePath, FileMode.Create))
						{
							await imageFile.CopyToAsync(fileStream);
						}

						product.ImageUrl = newFileName;
					}
					else
					{
						product.ImageUrl = "anh.png";
					}

					_context.Add(product);
					await _context.SaveChangesAsync();

					TempData["Success"] = "Thêm mới thành công!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
				}
			}
			else
			{
				// Báo lỗi chi tiết
				var errors = new List<string>();
				foreach (var key in ModelState.Keys)
				{
					foreach (var err in ModelState[key].Errors)
					{
						errors.Add($"{key}: {err.ErrorMessage}");
					}
				}
				TempData["Error"] = string.Join(" | ", errors);
			}
			return View(product);
		}

		// ==================================================
		// 4. CHỈNH SỬA
		// ==================================================
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();
			var p = await _context.Products.FindAsync(id);
			return p == null ? NotFound() : View(p);
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
		{
			if (id != product.Id) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					if (imageFile != null && imageFile.Length > 0)
					{
						string extension = Path.GetExtension(imageFile.FileName);
						string newFileName = product.Code + extension;
						string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", newFileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await imageFile.CopyToAsync(stream);
						}
						product.ImageUrl = newFileName;
					}

					_context.Update(product);
					await _context.SaveChangesAsync();
					TempData["Success"] = "Cập nhật thành công!";
					return RedirectToAction(nameof(Index));
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!_context.Products.Any(e => e.Id == product.Id)) return NotFound();
					else throw;
				}
			}
			return View(product);
		}

		// ==================================================
		// 5. XÓA
		// ==================================================
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();
			var p = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
			return p == null ? NotFound() : View(p);
		}

		[HttpPost, ActionName("Delete")]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var p = await _context.Products.FindAsync(id);
			if (p != null)
			{
				_context.Products.Remove(p);
				await _context.SaveChangesAsync();
				TempData["Success"] = "Đã xóa sản phẩm!";
			}
			return RedirectToAction(nameof(Index));
		}
	}
}