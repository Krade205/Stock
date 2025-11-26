using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc; 
using Microsoft.EntityFrameworkCore;
using Stock.Data;
using Stock.Models;
using System.Security.Claims;

namespace Stock.Controllers
{

    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // PHẦN 1: ĐĂNG NHẬP
        // ==========================================
        public IActionResult Login()
        {
            // Nếu đã đăng nhập rồi thì đá về trang chủ luôn, không cần đăng nhập lại
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Products");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Tìm user trong database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                // Tạo thông tin phiên đăng nhập
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    // SỬA LỖI: Nếu FullName trong DB bị Null thì lấy tạm Username để không bị lỗi web
                    new Claim(ClaimTypes.GivenName, user.FullName ?? user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Ghi nhớ đăng nhập
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Products");
            }

            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return Content("Bạn không có quyền truy cập chức năng này!");
        }

        // ==========================================
        // PHẦN 2: ĐĂNG KÝ
        // ==========================================

        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Products");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra trùng tên đăng nhập
                var checkUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (checkUser != null)
                {
                    ViewBag.Error = "Tên đăng nhập này đã tồn tại!";
                    return View(model);
                }

                // 2. Tạo tài khoản mới
                // LƯU Ý: Dùng 'Stock.Models.User' để tránh nhầm lẫn với User đang đăng nhập
                var newUser = new Stock.Models.User
                {
                    Username = model.Username,
                    Password = model.Password,
                    FullName = model.FullName,
                    Role = "User" // Mặc định là User thường
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // 3. Thông báo và chuyển hướng
                TempData["Success"] = "Đăng ký thành công! Hãy đăng nhập ngay.";
                return RedirectToAction("Login");
            }

            return View(model);
        }
    }
}