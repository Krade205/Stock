using Microsoft.AspNetCore.Http;
using System.Net;

namespace Stock.Services
{
    public class QRService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public QRService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Hàm lấy đường dẫn gốc (VD: http://192.168.1.5:5000)
        public string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return "";
            return $"{request.Scheme}://{request.Host}";
        }

        // Hàm tạo link ảnh QR code từ API (không cần lưu file)
        public string GetQRCodeUrl(string content)
        {
            return $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={WebUtility.UrlEncode(content)}";
        }
    }
}