using Microsoft.AspNetCore.Authentication.Cookies; // <--- CẦN DÒNG NÀY
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Stock.Data;
using Stock.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CẤU HÌNH KẾT NỐI SQL SERVER
// ==========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Chưa tìm thấy 'DefaultConnection' trong file appsettings.json");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ==========================================
// 2. CẤU HÌNH ĐĂNG NHẬP (SỬA LỖI CỦA BẠN TẠI ĐÂY)
// ==========================================
// Bạn đang thiếu đoạn này nên nó báo lỗi InvalidOperationException
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";  // Chưa đăng nhập thì chuyển về đây
        options.AccessDeniedPath = "/Account/AccessDenied"; // Không có quyền thì chuyển về đây
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Tự đăng xuất sau 60 phút
    });
// ------------------------------------------

// ==========================================
// 3. ĐĂNG KÝ CÁC DỊCH VỤ KHÁC
// ==========================================
builder.Services.AddScoped<QRService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();


var app = builder.Build();

// ==========================================
// 4. PIPELINE
// ==========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thứ tự bắt buộc: Authentication (Xác thực) -> Authorization (Phân quyền)
app.UseAuthentication(); // <--- BẮT BUỘC PHẢI CÓ DÒNG NÀY
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

app.Run();