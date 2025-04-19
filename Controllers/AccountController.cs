using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using ITWebManagement.Data;
using ITWebManagement.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITWebManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            try
            {
                _context.Database.OpenConnection();
                _context.Database.CloseConnection();
                TempData["Debug"] = "Kết nối database thành công.";
            }
            catch (Exception ex)
            {
                TempData["Debug"] = $"Lỗi kết nối database: {ex.Message}";
                _logger.LogError(ex, "Lỗi kết nối database khi vào trang Login.");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Log dữ liệu thô từ form
            var formData = HttpContext.Request.Form;
            string formLog = "Dữ liệu thô từ form: ";
            foreach (var key in formData.Keys)
            {
                formLog += $"{key} = {formData[key]}; ";
            }
            _logger.LogInformation(formLog);
            TempData["Debug"] = formLog;

            try
            {
                _context.Database.OpenConnection();
                _context.Database.CloseConnection();
                TempData["Debug"] += "; Kết nối database thành công.";
            }
            catch (Exception ex)
            {
                TempData["Debug"] += $"; Lỗi kết nối database: {ex.Message}";
                _logger.LogError(ex, "Lỗi kết nối database trong action Login POST.");
                return View(model);
            }

            // Log dữ liệu sau khi bind vào model
            TempData["Debug"] += $"; Dữ liệu sau bind: Username = {model.Username}, Password = {(string.IsNullOrEmpty(model.Password) ? "trống" : "có giá trị")}";

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["Debug"] += $"; ModelState lỗi: {errors}";
                _logger.LogWarning("ModelState không hợp lệ: {Errors}", errors);
                return View(model);
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
                    TempData["Debug"] += "; Tài khoản không tồn tại trong DB.";
                    _logger.LogWarning("Tài khoản không tồn tại: {Username}", model.Username);
                    return View(model);
                }

                TempData["Debug"] += $"; Tìm thấy tài khoản: Username = {user.Username}, Role = {user.Role}, Password = {user.Password}";

                bool passwordMatch = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
                if (!passwordMatch)
                {
                    ModelState.AddModelError(string.Empty, "Mật khẩu không đúng.");
                    TempData["Debug"] += $"; Mật khẩu không khớp. Mật khẩu nhập: {model.Password}, Mật khẩu trong DB: {user.Password}";
                    _logger.LogWarning("Mật khẩu không khớp cho tài khoản: {Username}", model.Username);
                    return View(model);
                }

                if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Role))
                {
                    ModelState.AddModelError(string.Empty, "Thông tin người dùng không hợp lệ.");
                    TempData["Debug"] += "; Username hoặc Role của tài khoản bị null.";
                    _logger.LogWarning("Thông tin tài khoản không hợp lệ: Username = {Username}, Role = {Role}", user.Username, user.Role);
                    return View(model);
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["Debug"] = "Đăng nhập thành công, cookie đã được tạo. Chuyển hướng đến Devices.";
                _logger.LogInformation("Đăng nhập thành công cho tài khoản: {Username}", user.Username);
                return RedirectToAction("Index", "Devices");
            }
            catch (Exception ex)
            {
                TempData["Debug"] += $"; Lỗi trong quá trình đăng nhập: {ex.Message}";
                _logger.LogError(ex, "Lỗi trong quá trình đăng nhập cho tài khoản: {Username}", model.Username);
                return View(model);
            }
        }

        [AllowAnonymous]
        public IActionResult CreateAdmin()
        {
            try
            {
                _context.Database.OpenConnection();
                _context.Database.CloseConnection();
                TempData["Debug"] = "Kết nối database thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối database trong CreateAdmin.");
                return Content($"Lỗi kết nối database: {ex.Message}");
            }

            try
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword("admin123");
                var adminUser = new User
                {
                    Username = "admin",
                    Password = hashedPassword,
                    Role = "admin"
                };

                var existingUser = _context.Users.FirstOrDefault(u => u.Username == "admin");
                if (existingUser != null)
                {
                    _context.Users.Remove(existingUser);
                    _context.SaveChanges();
                }

                _context.Users.Add(adminUser);
                _context.SaveChanges();

                _logger.LogInformation("Tài khoản admin đã được tạo với mật khẩu mã hóa: {HashedPassword}", hashedPassword);
                return Content($"Tài khoản admin đã được tạo. Mật khẩu mã hóa: {hashedPassword}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo tài khoản admin.");
                return Content($"Lỗi khi tạo tài khoản admin: {ex.Message}");
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Devices");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}