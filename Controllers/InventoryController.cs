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
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Inventory
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            var inventory = _context.Inventory.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                inventory = inventory.Where(i => i.AssetCode.Contains(searchString) ||
                                                i.Model.Contains(searchString) ||
                                                i.ImportedBy.Contains(searchString) ||
                                                i.Time.ToString("MM-yyyy").Contains(searchString));
            }
            return View(await inventory.ToListAsync());
        }

        // GET: Inventory/Statistics
        [AllowAnonymous]
        public async Task<IActionResult> Statistics(int? year, int? month)
        {
            var inventory = _context.Inventory.AsQueryable();

            if (year.HasValue)
            {
                inventory = inventory.Where(i => i.Time.Year == year.Value);
            }
            if (month.HasValue)
            {
                inventory = inventory.Where(i => i.Time.Month == month.Value);
            }

            var groupedStats = await inventory
                .GroupBy(i => new { i.Time.Year, i.Time.Month, i.TransactionType })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TransactionType = g.Key.TransactionType,
                    TotalQuantity = g.Sum(i => i.Quantity)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            var formattedStats = groupedStats.Select(s => new
            {
                YearMonth = $"{s.Month:D2}-{s.Year}",
                TransactionType = s.TransactionType,
                TotalQuantity = s.TotalQuantity
            }).ToList();

            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;
            return View(formattedStats);
        }

        // GET: Inventory/Create
        [Authorize(Roles = "admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([Bind("AssetCode,Model,Quantity,Time,ImportedBy")] Inventory inventory)
        {
            if (ModelState.IsValid)
            {
                inventory.TransactionType = "Import"; // Ghi nhận là nhập kho
                _context.Add(inventory);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm nhập kho thành công.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(inventory);
        }

        // GET: Inventory/Export
        [Authorize(Roles = "admin")]
        public IActionResult Export()
        {
            return View();
        }

        // POST: Inventory/Export
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Export([Bind("AssetCode,Model,Quantity,Time,ImportedBy")] Inventory inventory)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra số lượng tồn kho
                var totalImported = await _context.Inventory
                    .Where(i => i.AssetCode == inventory.AssetCode && i.TransactionType == "Import")
                    .SumAsync(i => i.Quantity);
                var totalExported = await _context.Inventory
                    .Where(i => i.AssetCode == inventory.AssetCode && i.TransactionType == "Export")
                    .SumAsync(i => i.Quantity);
                var currentStock = totalImported - totalExported;

                if (inventory.Quantity > currentStock)
                {
                    ModelState.AddModelError("Quantity", $"Số lượng xuất kho vượt quá tồn kho hiện tại ({currentStock}).");
                    return View(inventory);
                }

                inventory.TransactionType = "Export"; // Ghi nhận là xuất kho
                _context.Add(inventory);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xuất kho thành công.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(inventory);
        }

        // GET: Inventory/Edit/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }
            return View(inventory);
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id, [Bind("InventoryID,AssetCode,Model,Quantity,Time,ImportedBy,TransactionType")] Inventory inventory)
        {
            if (id != inventory.InventoryID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventory);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sửa nhập/xuất kho thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryExists(inventory.InventoryID))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(inventory);
        }

        // GET: Inventory/Delete/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var inventory = await _context.Inventory.FirstOrDefaultAsync(m => m.InventoryID == id);
            if (inventory == null)
            {
                return NotFound();
            }
            return View(inventory);
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory != null)
            {
                _context.Inventory.Remove(inventory);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa nhập/xuất kho thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Inventory/ImportExcel
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
                    var model = worksheet.Cells[row, 2].Value?.ToString();
                    var quantityStr = worksheet.Cells[row, 3].Value?.ToString();
                    var timeStr = worksheet.Cells[row, 4].Value?.ToString();
                    var importedBy = worksheet.Cells[row, 5].Value?.ToString();

                    if (string.IsNullOrEmpty(assetCode) || string.IsNullOrEmpty(model) || string.IsNullOrEmpty(quantityStr) ||
                        string.IsNullOrEmpty(timeStr) || string.IsNullOrEmpty(importedBy))
                    {
                        continue; // Bỏ qua dòng thiếu dữ liệu
                    }

                    if (!int.TryParse(quantityStr, out var quantity) ||
                        !DateTime.TryParse(timeStr, out var time))
                    {
                        continue; // Bỏ qua nếu dữ liệu không hợp lệ
                    }

                    var inventory = new Inventory
                    {
                        AssetCode = assetCode,
                        Model = model,
                        Quantity = quantity,
                        Time = time,
                        ImportedBy = importedBy,
                        TransactionType = "Import"
                    };

                    _context.Add(inventory);
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

        // GET: Inventory/ExportExcel
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ExportExcel()
        {
            var inventory = await _context.Inventory.ToListAsync();
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Inventory");
            worksheet.Cells[1, 1].Value = "Mã thiết bị";
            worksheet.Cells[1, 2].Value = "Model";
            worksheet.Cells[1, 3].Value = "Số lượng";
            worksheet.Cells[1, 4].Value = "Thời gian";
            worksheet.Cells[1, 5].Value = "Người nhập/xuất";
            worksheet.Cells[1, 6].Value = "Loại giao dịch";

            var vietnameseCulture = new CultureInfo("vi-VN");
            for (int i = 0; i < inventory.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = inventory[i].AssetCode;
                worksheet.Cells[i + 2, 2].Value = inventory[i].Model;
                worksheet.Cells[i + 2, 3].Value = inventory[i].Quantity;
                worksheet.Cells[i + 2, 4].Value = inventory[i].Time.ToString("MM-yyyy");
                worksheet.Cells[i + 2, 5].Value = inventory[i].ImportedBy;
                worksheet.Cells[i + 2, 6].Value = inventory[i].TransactionType;
            }

            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Inventory.xlsx");
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventory.Any(e => e.InventoryID == id);
        }
    }
}