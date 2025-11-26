using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stock.Data;
using Stock.Models;
using Stock.Services;

namespace Stock.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QRService _qrService;
        private readonly IWebHostEnvironment _webHostEnvironment; // Dùng để xử lý file ảnh

        public ProductsController(ApplicationDbContext context, QRService qrService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _qrService = qrService;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==================================================
        // 1. XEM DANH SÁCH (Ai cũng xem được)
        // ==================================================
        public async Task<IActionResult> Index()
        {
            // Lấy đường dẫn gốc để tạo QR Code
            ViewBag.BaseUrl = _qrService.GetBaseUrl();
            return View(await _context.Products.ToListAsync());
        }

        // ==================================================
        // 2. XEM CHI TIẾT (Ai cũng xem được)
        // ==================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ==================================================
        // 3. THÊM MỚI (CHỈ ADMIN) - CÓ UPLOAD ẢNH & BÁO LỖI CHI TIẾT
        // ==================================================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
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
                    // A. Kiểm tra trùng Mã sản phẩm
                    bool isDuplicate = await _context.Products.AnyAsync(p => p.Code == product.Code);
                    if (isDuplicate)
                    {
                        TempData["Error"] = $"Thất bại! Mã sản phẩm '{product.Code}' đã tồn tại.";
                        return View(product);
                    }

                    // B. Xử lý Upload Ảnh
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // 1. Đặt tên file chuẩn: MãSP.png (hoặc .jpg tùy file gốc)
                        string extension = Path.GetExtension(imageFile.FileName);
                        string newFileName = product.Code + extension;

                        // 2. Tạo đường dẫn lưu: wwwroot/images/
                        string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                        string filePath = Path.Combine(uploadFolder, newFileName);

                        // 3. Lưu file vào server
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        // 4. Gán tên file vào Database
                        product.ImageUrl = newFileName;
                    }
                    else
                    {
                        // Nếu không up ảnh -> Dùng ảnh mặc định
                        product.ImageUrl = "anh.png";
                    }

                    // C. Lưu vào Database
                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã thêm sản phẩm '{product.Name}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Lấy nội dung lỗi gốc từ bên trong (InnerException)
                    var realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                    // Kiểm tra các lỗi phổ biến để báo tiếng Việt cho dễ hiểu
                    if (realError.Contains("duplicate") || realError.Contains("UNIQUE"))
                    {
                        TempData["Error"] = $"Lỗi: Mã sản phẩm '{product.Code}' đã tồn tại rồi!";
                    }
                    else if (realError.Contains("truncated") || realError.Contains("length"))
                    {
                        TempData["Error"] = "Lỗi: Dữ liệu nhập vào quá dài so với quy định của Database!";
                    }
                    else
                    {
                        // Hiện nguyên văn lỗi kỹ thuật để bạn đọc
                        TempData["Error"] = "Lỗi chi tiết: " + realError;
                    }
                }
            }
            else
            {
                // --- HIỂN THỊ LỖI CHI TIẾT (Giúp bạn biết sai ở đâu) ---
                var errorMessages = new List<string>();
                foreach (var modelStateKey in ModelState.Keys)
                {
                    var modelStateVal = ModelState[modelStateKey];
                    foreach (var error in modelStateVal.Errors)
                    {
                        var message = error.ErrorMessage;
                        if (string.IsNullOrEmpty(message)) message = error.Exception?.Message;
                        errorMessages.Add($"Lỗi tại ô '{modelStateKey}': {message}");
                    }
                }
                TempData["Error"] = string.Join(" | ", errorMessages);
            }
            return View(product);
        }

        // ==================================================
        // 4. CHỈNH SỬA (CHỈ ADMIN)
        // ==================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
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
                    // Xử lý ảnh khi sửa: Nếu có up ảnh mới thì thay thế ảnh cũ
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
                    // Nếu không up ảnh mới -> Giữ nguyên giá trị ImageUrl cũ (đã có sẵn trong biến product do View gửi lên)

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id)) return NotFound();
                    else throw;
                }
            }
            else
            {
                TempData["Error"] = "Cập nhật thất bại. Vui lòng kiểm tra lại dữ liệu nhập.";
            }
            return View(product);
        }

        // ==================================================
        // 5. XÓA (CHỈ ADMIN)
        // ==================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                try
                {
                    // (Tuỳ chọn) Nếu muốn xóa luôn file ảnh khỏi server để tiết kiệm chỗ:
                    /*
                    if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != "anh.png")
                    {
                        string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", product.ImageUrl);
                        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                    }
                    */

                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã xóa sản phẩm khỏi hệ thống!";
                }
                catch
                {
                    TempData["Error"] = "Xóa thất bại! Có lỗi xảy ra.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}