using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stock.Data;
using Stock.Models;

namespace Stock.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được vào kho
    public class Stock_ManageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public Stock_ManageController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================================================
        // 1. TRANG CHỦ DASHBOARD
        // ==================================================
        public async Task<IActionResult> Index()
        {
            // Tính toán số liệu thống kê
            var totalImport = await _context.StockTransactions.SumAsync(t => t.ImportQuantity);
            var totalImportVal = await _context.StockTransactions.SumAsync(t => t.ImportQuantity * t.ImportPrice);
            var totalExport = await _context.StockTransactions.SumAsync(t => t.ExportQuantity ?? 0);
            var totalExportVal = await _context.StockTransactions.SumAsync(t => (t.ExportQuantity ?? 0) * (t.ExportPrice ?? 0));
            var totalStock = await _context.Products.SumAsync(p => p.Quantity);
            var products = await _context.Products.ToListAsync();

            var model = new InventoryReportViewModel
            {
                TotalImport = totalImport,
                TotalExport = totalExport,
                CurrentStock = totalStock,
                TotalImportValue = totalImportVal,
                TotalExportValue = totalExportVal,
                ProductList = products
            };

            return View(model);
        }

        // ===============================================
        // 2. LỊCH SỬ NHẬP KHO
        // ===============================================
        public async Task<IActionResult> ImportHistory()
        {
            var list = await _context.StockTransactions
                .Include(t => t.Product)
                .Where(t => t.ImportQuantity > 0)
                .OrderByDescending(t => t.ImportDateTime)
                .ToListAsync();
            return View(list);
        }

        // ===============================================
        // 3. LỊCH SỬ XUẤT KHO
        // ===============================================
        public async Task<IActionResult> ExportHistory()
        {
            var list = await _context.StockTransactions
                .Include(t => t.Product)
                .Where(t => t.ExportQuantity > 0)
                .OrderByDescending(t => t.ExportDateTime)
                .ToListAsync();
            return View(list);
        }

        // ===============================================
        // 4. TẠO PHIẾU NHẬP KHO (ReportImp)
        // ===============================================
        [HttpGet]
        public IActionResult ReportImp()
        {
            var productList = _context.Products
                .Select(p => new { Id = p.Id, DisplayText = $"[{p.Code}] - {p.Name}" })
                .ToList();

            ViewData["ProductId"] = new SelectList(productList, "Id", "DisplayText");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportImp(StockTransaction model, string productCode, string productName)
        {
            // 1. Tìm xem mã này đã có chưa?
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Code == productCode);

            if (product == null)
            {
                // --- TRƯỜNG HỢP: SẢN PHẨM MỚI ---
                // Vì chưa có, bắt buộc phải có Tên để tạo mới
                if (string.IsNullOrEmpty(productName))
                {
                    TempData["Error"] = "❌ Mã này mới, vui lòng nhập Tên sản phẩm!";
                    ViewBag.EnteredCode = productCode;
                    return View(model);
                }

                // Tạo mới sản phẩm
                product = new Product
                {
                    Code = productCode,
                    Name = productName,   // Lấy tên bạn vừa nhập
                    Quantity = 0,         // Tồn đầu = 0
                    Price = 0,            // Giá bán tạm = 0
                    ImageUrl = "anh.png",
                    Description = "Tạo nhanh từ phiếu nhập kho"
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }
            else
            {
                // --- TRƯỜNG HỢP: SẢN PHẨM CŨ ---
                // Nếu muốn cập nhật tên mới cho sản phẩm cũ thì bỏ comment dòng dưới:
                // product.Name = productName; 
            }

            // Gán ID sản phẩm (dù mới hay cũ) vào phiếu
            model.ProductId = product.Id;

            // Bỏ qua kiểm tra ProductId
            ModelState.Remove("ProductId");
            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ImportQuantity <= 0)
                    {
                        TempData["Error"] = "⚠️ Số lượng nhập phải lớn hơn 0!";
                        ViewBag.EnteredCode = productCode;
                        ViewBag.EnteredName = productName;
                        return View(model);
                    }

                    // Cộng kho
                    product.Quantity += model.ImportQuantity;

                    // Reset dữ liệu xuất
                    model.ExportQuantity = 0;
                    model.ExportPrice = 0;
                    model.ExportDateTime = null;

                    model.StockRemaining = product.Quantity;

                    _context.Add(model);
                    _context.Products.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"✅ Đã nhập thêm {model.ImportQuantity} {model.Unit} cho '{product.Name}'.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
            }

            // Trả lại dữ liệu nếu lỗi
            ViewBag.EnteredCode = productCode;
            ViewBag.EnteredName = productName;
            return View(model);
        }

        // ===============================================
        // 5. TẠO PHIẾU XUẤT KHO (ReportExp)
        // ===============================================
        [HttpGet]
        public IActionResult ReportExp()
        {
            var products = _context.Products.ToList();

            // 1. Tạo SelectList chỉ hiện [Mã] - Tên (Cho gọn)
            var selectList = products.Select(p => new
            {
                Id = p.Id,
                DisplayText = $"[{p.Code}] - {p.Name}"
            }).ToList();

            ViewData["ProductId"] = new SelectList(selectList, "Id", "DisplayText");

            // 2. Gửi danh sách gốc sang View để Javascript lấy số lượng tồn
            ViewBag.Products = products;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportExp(StockTransaction model)
        {
            // (Phần xử lý POST giữ nguyên logic cũ, chỉ sửa lại phần nạp lại Dropdown nếu lỗi)
            var products = _context.Products.ToList();
            var selectList = products.Select(p => new { Id = p.Id, DisplayText = $"[{p.Code}] - {p.Name}" }).ToList();

            ViewData["ProductId"] = new SelectList(selectList, "Id", "DisplayText", model.ProductId);
            ViewBag.Products = products; // Gửi lại danh sách nếu validate lỗi

            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);

                if (product == null) { TempData["Error"] = "❌ Vui lòng chọn sản phẩm!"; return View(model); }

                try
                {
                    if (model.ExportQuantity <= 0) { TempData["Error"] = "⚠️ Số lượng xuất phải > 0!"; return View(model); }
                    if (product.Quantity < model.ExportQuantity) { TempData["Error"] = $"⚠️ Không đủ hàng! Kho còn {product.Quantity}."; return View(model); }

                    product.Quantity -= (int)model.ExportQuantity;
                    if (model.ExportDateTime == null) model.ExportDateTime = DateTime.Now;

                    model.ImportQuantity = 0; model.ImportPrice = 0;
                    model.StockRemaining = product.Quantity;

                    _context.Add(model); _context.Products.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"✅ Đã xuất {model.ExportQuantity} {model.Unit} '{product.Name}'.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex) { TempData["Error"] = "Lỗi: " + ex.Message; }
            }
            return View(model);
        }

        // ===============================================
        // 6. XÓA GIAO DỊCH (HOÀN TRẢ LẠI KHO)
        // ===============================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var transaction = await _context.StockTransactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null) return NotFound();
            return View(transaction);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.StockTransactions.FindAsync(id);
            if (transaction != null)
            {
                var product = await _context.Products.FindAsync(transaction.ProductId);
                if (product != null)
                {
                    // HOÀN TRẢ KHO (Reverse Logic)
                    if (transaction.ImportQuantity > 0) // Xóa phiếu NHẬP -> Phải TRỪ lại kho
                    {
                        if (product.Quantity >= transaction.ImportQuantity)
                            product.Quantity -= transaction.ImportQuantity;
                        else
                            product.Quantity = 0;
                    }

                    if (transaction.ExportQuantity > 0) // Xóa phiếu XUẤT -> Phải CỘNG lại kho
                    {
                        product.Quantity += (int)transaction.ExportQuantity;
                    }

                    _context.Products.Update(product);
                }

                _context.StockTransactions.Remove(transaction);
                await _context.SaveChangesAsync();

                TempData["Success"] = "🗑️ Đã xóa phiếu giao dịch và hoàn trả số lượng kho!";
            }
            // Quay lại trang chi tiết của sản phẩm đó (hoặc trang Index nếu muốn)
            return RedirectToAction(nameof(Index));
        }

        // ===============================================
        // 7. CHI TIẾT TỒN KHO (CỦA 1 SẢN PHẨM)
        // ===============================================
        public async Task<IActionResult> InventoryDetails(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var transactions = await _context.StockTransactions
                .Where(t => t.ProductId == id)
                .OrderByDescending(t => t.ImportDateTime)
                .ToListAsync();

            var model = new ProductInventoryViewModel
            {
                Product = product,
                TotalImport = transactions.Sum(t => t.ImportQuantity),
                TotalExport = transactions.Sum(t => t.ExportQuantity ?? 0),
                CurrentStock = product.Quantity,
                Transactions = transactions
            };

            return View(model);
        }

        // ===============================================
        // 8. BÁO CÁO TỔNG HỢP (DANH SÁCH TRẠNG THÁI)
        // ===============================================
        public async Task<IActionResult> InventoryStatus()
        {
            var products = await _context.Products.ToListAsync();
            var transactions = await _context.StockTransactions.ToListAsync();

            var reportList = new List<InventoryStatusViewModel>();

            foreach (var p in products)
            {
                var pTrans = transactions.Where(t => t.ProductId == p.Id).ToList();
                reportList.Add(new InventoryStatusViewModel
                {
                    ProductId = p.Id,
                    ProductCode = p.Code,
                    ProductName = p.Name,
                    ImageUrl = p.ImageUrl,
                    CurrentStock = p.Quantity,
                    TotalImport = pTrans.Sum(t => t.ImportQuantity),
                    TotalExport = pTrans.Sum(t => t.ExportQuantity ?? 0)
                });
            }
            return View(reportList);
        }
    }
}