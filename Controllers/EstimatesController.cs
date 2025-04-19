using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITWebManagement.Data;
using ITWebManagement.Models;
using OfficeOpenXml;
using System.IO;
using System.Globalization;

namespace ITWebManagement.Controllers
{
    public class EstimatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EstimatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Estimates
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            var estimates = _context.Estimates.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                estimates = estimates.Where(e => e.AssetCode.Contains(searchString) ||
                                                e.Department.Contains(searchString) ||
                                                e.Time.ToString("MM-yyyy").Contains(searchString));
            }
            return View(await estimates.ToListAsync());
        }

        // GET: Estimates/Statistics
        [AllowAnonymous]
        public async Task<IActionResult> Statistics(int? year, int? month)
        {
            var stats = _context.Estimates.AsQueryable();

            if (year.HasValue)
            {
                stats = stats.Where(e => e.Time.Year == year.Value);
            }
            if (month.HasValue)
            {
                stats = stats.Where(e => e.Time.Month == month.Value);
            }

            var groupedStats = await stats
                .GroupBy(e => new { e.Time.Year, e.Time.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalPrice = g.Sum(e => e.TotalPrice)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            var formattedStats = groupedStats.Select(s => new
            {
                YearMonth = $"{s.Month:D2}-{s.Year}",
                TotalPrice = s.TotalPrice
            }).ToList();

            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;
            return View(formattedStats);
        }

        // GET: Estimates/Create
        [Authorize(Roles = "admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Estimates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([Bind("AssetCode,Quantity,UnitPrice,Time,Department")] Estimate estimate)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra UnitPrice hợp lệ
                if (estimate.UnitPrice < 0)
                {
                    ModelState.AddModelError("UnitPrice", "Đơn giá không được âm.");
                    return View(estimate);
                }

                estimate.TotalPrice = estimate.Quantity * estimate.UnitPrice;
                _context.Add(estimate);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm dự toán thành công.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(estimate);
        }

        // GET: Estimates/Edit/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var estimate = await _context.Estimates.FindAsync(id);
            if (estimate == null)
            {
                return NotFound();
            }
            return View(estimate);
        }

        // POST: Estimates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id, [Bind("EstimateID,AssetCode,Quantity,UnitPrice,Time,Department")] Estimate estimate)
        {
            if (id != estimate.EstimateID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra UnitPrice hợp lệ
                if (estimate.UnitPrice < 0)
                {
                    ModelState.AddModelError("UnitPrice", "Đơn giá không được âm.");
                    return View(estimate);
                }

                try
                {
                    estimate.TotalPrice = estimate.Quantity * estimate.UnitPrice;
                    _context.Update(estimate);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sửa dự toán thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EstimateExists(estimate.EstimateID))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(estimate);
        }

        // GET: Estimates/Delete/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var estimate = await _context.Estimates.FirstOrDefaultAsync(m => m.EstimateID == id);
            if (estimate == null)
            {
                return NotFound();
            }
            return View(estimate);
        }

        // POST: Estimates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var estimate = await _context.Estimates.FindAsync(id);
            if (estimate != null)
            {
                _context.Estimates.Remove(estimate);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa dự toán thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Estimates/ImportExcel
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    var assetCode = worksheet.Cells[row, 1].Value?.ToString();
                    var quantityStr = worksheet.Cells[row, 2].Value?.ToString();
                    var unitPriceStr = worksheet.Cells[row, 3].Value?.ToString();
                    var timeStr = worksheet.Cells[row, 4].Value?.ToString();
                    var department = worksheet.Cells[row, 5].Value?.ToString();

                    if (string.IsNullOrEmpty(assetCode) || string.IsNullOrEmpty(quantityStr) || string.IsNullOrEmpty(unitPriceStr) ||
                        string.IsNullOrEmpty(timeStr) || string.IsNullOrEmpty(department))
                    {
                        continue; // Bỏ qua dòng thiếu dữ liệu
                    }

                    if (!int.TryParse(quantityStr, out var quantity) ||
                        !decimal.TryParse(unitPriceStr, out var unitPrice) ||
                        !DateTime.TryParse(timeStr, out var time))
                    {
                        continue; // Bỏ qua nếu dữ liệu không hợp lệ
                    }

                    // Kiểm tra UnitPrice hợp lệ
                    if (unitPrice < 0)
                    {
                        continue; // Bỏ qua nếu đơn giá âm
                    }

                    var estimate = new Estimate
                    {
                        AssetCode = assetCode,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Time = time,
                        Department = department,
                        TotalPrice = quantity * unitPrice
                    };

                    _context.Add(estimate);
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = "Nhập dữ liệu thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi nhập Excel: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Estimates/ExportExcel
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ExportExcel()
        {
            var estimates = await _context.Estimates.ToListAsync();
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Estimates");
            worksheet.Cells[1, 1].Value = "Mã thiết bị";
            worksheet.Cells[1, 2].Value = "Số lượng";
            worksheet.Cells[1, 3].Value = "Đơn giá";
            worksheet.Cells[1, 4].Value = "Thời gian";
            worksheet.Cells[1, 5].Value = "Bộ phận";
            worksheet.Cells[1, 6].Value = "Tổng giá";

            var vietnameseCulture = new CultureInfo("vi-VN");
            for (int i = 0; i < estimates.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = estimates[i].AssetCode;
                worksheet.Cells[i + 2, 2].Value = estimates[i].Quantity;
                worksheet.Cells[i + 2, 3].Value = estimates[i].UnitPrice.ToString("N0", vietnameseCulture);
                worksheet.Cells[i + 2, 4].Value = estimates[i].Time.ToString("MM-yyyy");
                worksheet.Cells[i + 2, 5].Value = estimates[i].Department;
                worksheet.Cells[i + 2, 6].Value = estimates[i].TotalPrice.ToString("N0", vietnameseCulture);
            }

            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Estimates.xlsx");
        }

        // POST: Estimates/DeleteAll
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                var estimates = await _context.Estimates.ToListAsync();
                _context.Estimates.RemoveRange(estimates);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa toàn bộ dữ liệu.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa toàn bộ dữ liệu: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EstimateExists(int id)
        {
            return _context.Estimates.Any(e => e.EstimateID == id);
        }
    }
}