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
        // 1. TRANG CHỦ DASHBOARD (THỐNG KÊ)
        // ==================================================
        public async Task<IActionResult> Index()
        {
            // 1. Tính Tổng Nhập
            var totalImport = await _context.StockTransactions.SumAsync(t => t.ImportQuantity);
            var totalImportVal = await _context.StockTransactions.SumAsync(t => t.ImportQuantity * t.ImportPrice);

            // 2. Tính Tổng Xuất
            var totalExport = await _context.StockTransactions.SumAsync(t => t.ExportQuantity ?? 0);
            var totalExportVal = await _context.StockTransactions.SumAsync(t => (t.ExportQuantity ?? 0) * (t.ExportPrice ?? 0));

            // 3. Tính Tổng Tồn kho thực tế (Lấy từ bảng Products)
            var totalStock = await _context.Products.SumAsync(p => p.Quantity);

            // 4. Đưa dữ liệu vào Model
            var model = new InventoryReportViewModel
            {
                TotalImport = totalImport,
                TotalExport = totalExport,
                CurrentStock = totalStock,
                TotalImportValue = totalImportVal,
                TotalExportValue = totalExportVal
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
                .Where(t => t.ImportQuantity > 0) // Chỉ lấy phiếu NHẬP
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
                .Where(t => t.ExportQuantity > 0) // Chỉ lấy phiếu XUẤT
                .OrderByDescending(t => t.ExportDateTime)
                .ToListAsync();
            return View(list);
        }

        // ===============================================
        // 4. TẠO PHIẾU NHẬP KHO
        // ===============================================
        [HttpGet]
        public IActionResult ReportImp()
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportImp(StockTransaction model)
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", model.ProductId);

            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                {
                    TempData["Error"] = "❌ Lỗi: Vui lòng chọn sản phẩm trong danh sách!";
                    return View("Import", model);
                }

                try
                {
                    // --- LOGIC NHẬP KHO ---
                    if (model.ImportQuantity > 0)
                    {
                        product.Quantity += model.ImportQuantity;

                        // Xóa thông tin xuất
                        model.ExportQuantity = 0;
                        model.ExportPrice = 0;
                        model.ExportDateTime = null;

                        model.StockRemaining = product.Quantity;

                        _context.Add(model);
                        _context.Products.Update(product);
                        await _context.SaveChangesAsync();

                        TempData["Success"] = $"✅ Đã nhập thêm {model.ImportQuantity} sản phẩm '{product.Name}'.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "⚠️ Vui lòng nhập số lượng lớn hơn 0!";
                        return View("Import", model);
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                    return View("Import", model);
                }
            }

            TempData["Error"] = "Dữ liệu nhập vào chưa hợp lệ.";
            return View("Import", model);
        }

        // ===============================================
        // 5. TẠO PHIẾU XUẤT KHO
        // ===============================================
        [HttpGet]
        public IActionResult ReportExp()
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportExp(StockTransaction model)
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", model.ProductId);

            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                {
                    TempData["Error"] = "❌ Lỗi: Vui lòng chọn sản phẩm trong danh sách!";
                    return View("Export", model);
                }

                try
                {
                    // --- LOGIC XUẤT KHO ---
                    if (model.ExportQuantity > 0)
                    {
                        if (product.Quantity < model.ExportQuantity)
                        {
                            TempData["Error"] = $"⚠️ Xuất thất bại! Kho chỉ còn {product.Quantity}, không đủ xuất {model.ExportQuantity}.";
                            return View("Export", model);
                        }

                        product.Quantity -= (int)model.ExportQuantity;

                        if (model.ExportDateTime == null) model.ExportDateTime = DateTime.Now;

                        model.ImportQuantity = 0;
                        model.ImportPrice = 0;

                        model.StockRemaining = product.Quantity;

                        _context.Add(model);
                        _context.Products.Update(product);
                        await _context.SaveChangesAsync();

                        TempData["Success"] = $"✅ Đã xuất kho {model.ExportQuantity} sản phẩm '{product.Name}'.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "⚠️ Vui lòng nhập số lượng lớn hơn 0!";
                        return View("Export", model);
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                    return View("Export", model);
                }
            }

            TempData["Error"] = "Dữ liệu nhập vào chưa hợp lệ.";
            return View("Export", model);
        }

        // ===============================================
        // 6. XÓA GIAO DỊCH (CÓ THÔNG BÁO)
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
                    // HOÀN TRẢ KHO
                    if (transaction.ImportQuantity > 0) // Xóa phiếu nhập -> Trừ lại kho
                    {
                        if (product.Quantity >= transaction.ImportQuantity)
                            product.Quantity -= transaction.ImportQuantity;
                        else
                            product.Quantity = 0;
                    }

                    if (transaction.ExportQuantity > 0) // Xóa phiếu xuất -> Cộng lại kho
                    {
                        product.Quantity += (int)transaction.ExportQuantity;
                    }

                    _context.Products.Update(product);
                }

                _context.StockTransactions.Remove(transaction);
                await _context.SaveChangesAsync();

                // --- THÔNG BÁO XÓA THÀNH CÔNG ---
                TempData["Success"] = "🗑️ Đã xóa phiếu giao dịch và hoàn trả số lượng kho!";
            }
            return RedirectToAction(nameof(Index));
        }

        // Chi tiết giao dịch (Nếu cần xem lẻ từng cái)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var transaction = await _context.StockTransactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null) return NotFound();
            return View(transaction);
        }

        public async Task<IActionResult> InventoryStatus()
        {
            // 1. Lấy tất cả sản phẩm
            var products = await _context.Products.ToListAsync();

            // 2. Lấy tất cả lịch sử giao dịch
            var transactions = await _context.StockTransactions.ToListAsync();

            // 3. Tạo danh sách báo cáo
            var reportList = new List<InventoryStatusViewModel>();

            foreach (var p in products)
            {
                // Lọc ra các giao dịch của sản phẩm này
                var pTrans = transactions.Where(t => t.ProductId == p.Id).ToList();

                reportList.Add(new InventoryStatusViewModel
                {
                    ProductId = p.Id,
                    ProductCode = p.Code,
                    ProductName = p.Name,
                    ImageUrl = p.ImageUrl,
                    CurrentStock = p.Quantity, // Tồn kho hiện tại

                    // Cộng dồn lịch sử để ra Tổng Nhập / Tổng Xuất
                    TotalImport = pTrans.Sum(t => t.ImportQuantity),
                    TotalExport = pTrans.Sum(t => t.ExportQuantity ?? 0)
                });
            }

            return View(reportList);
        }


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

    }
}