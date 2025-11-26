using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stock.Data;
using Stock.Models;

namespace Stock.Controllers
{
    public class StockTransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StockTransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách giao dịch
        public async Task<IActionResult> Index()
        {
            var list = await _context.StockTransactions
                                     .Include(t => t.Product)
                                     .ToListAsync();
            return View(list);
        }

        // Chi tiết giao dịch
        public async Task<IActionResult> Details(int id)
        {
            var transaction = await _context.StockTransactions
                                            .Include(t => t.Product)
                                            .FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null) return NotFound();
            return View(transaction);
        }

        //  Report
        [HttpGet]
        // ================================
        // NHẬP KHO
        // ================================
        [HttpGet]
        public IActionResult ReportImp()
        {
            ViewBag.Products = _context.Products.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ReportImp(StockTransaction model)
        {
            if (ModelState.IsValid)
            {
                // logic nhập kho
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.Products = _context.Products.ToList();
            return View(model);
        }

        // ================================
        // XUẤT KHO
        // ================================
        [HttpGet]
        public IActionResult ReportExp()
        {
            ViewBag.Products = _context.Products.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ReportExp(StockTransaction model)
        {
            if (ModelState.IsValid)
            {
                // logic xuất kho
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.Products = _context.Products.ToList();
            return View(model);
        }

        // Sửa giao dịch
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var transaction = await _context.StockTransactions.FindAsync(id);
            if (transaction == null) return NotFound();
            ViewBag.Products = _context.Products.ToList();
            return View(transaction);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(StockTransaction model)
        {
            if (ModelState.IsValid)
            {
                model.StockRemaining = model.ImportQuantity - (model.ExportQuantity ?? 0);
                _context.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.Products = _context.Products.ToList();
            return View(model);
        }

        // Xóa giao dịch
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var transaction = await _context.StockTransactions
                                            .Include(t => t.Product)
                                            .FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null) return NotFound();
            return View(transaction);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.StockTransactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Remove(transaction);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}