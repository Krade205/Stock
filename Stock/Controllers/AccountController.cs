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

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Products");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.GivenName, user.FullName ?? user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                return RedirectToAction("Index", "Products");
            }

            ViewBag.LoginError = "Sai tên đăng nhập hoặc mật khẩu";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ShowRegister = true;
                return View("Login", model);
            }

            var checkUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (checkUser != null)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại!");
                ViewBag.ShowRegister = true;
                return View("Login", model);
            }

            var newUser = new User
            {
                Username = model.Username,
                Password = model.Password,
                FullName = model.FullName,
                Role = "User"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            ViewBag.RegisterSuccess = "Đăng ký thành công! Vui lòng đăng nhập.";
            ViewBag.ShowRegister = false;

            return View("Login");
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
    }
}
