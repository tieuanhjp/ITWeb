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
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Purchases
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            var purchases = _context.Purchases.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                purchases = purchases.Where(p => p.AssetCode.Contains(searchString) ||
                                                p.Department.Contains(searchString) ||
                                                p.SupplierID.Contains(searchString) ||
                                                p.Time.ToString("MM-yyyy").Contains(searchString));
            }
            return View(await purchases.ToListAsync());
        }

        // GET: Purchases/Create
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
            ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
            return View();
        }

        // POST: Purchases/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([Bind("AssetCode,Quantity,UnitPrice,Time,Department,SupplierID")] Purchase purchase)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra AssetCode và SupplierID có tồn tại không
                if (!await _context.Devices.AnyAsync(d => d.AssetCode == purchase.AssetCode))
                {
                    ModelState.AddModelError("AssetCode", "Mã thiết bị không tồn tại.");
                    ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
                    ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
                    return View(purchase);
                }
                if (!await _context.Suppliers.AnyAsync(s => s.SupplierID == purchase.SupplierID))
                {
                    ModelState.AddModelError("SupplierID", "Mã nhà cung cấp không tồn tại.");
                    ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
                    ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
                    return View(purchase);
                }

                // Kiểm tra UnitPrice hợp lệ
                if (purchase.UnitPrice < 0)
                {
                    ModelState.AddModelError("UnitPrice", "Đơn giá không được âm.");
                    ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
                    ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
                    return View(purchase);
                }

                purchase.TotalPrice = purchase.Quantity * purchase.UnitPrice;
                _context.Add(purchase);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm mua hàng thành công.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
            ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
            return View(purchase);
        }

        // GET: Purchases/Edit/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null)
            {
                return NotFound();
            }
            ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
            ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
            return View(purchase);
        }

        // POST: Purchases/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id, [Bind("PurchaseID,AssetCode,Quantity,UnitPrice,Time,Department,SupplierID")] Purchase purchase)
        {
            if (id != purchase.PurchaseID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra AssetCode và SupplierID có tồn tại không
                    if (!await _context.Devices.AnyAsync(d => d.AssetCode == purchase.AssetCode))
                    {
                        ModelState.AddModelError("AssetCode", "Mã thiết bị không tồn tại.");
                        ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
                        ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
                        return View(purchase);
                    }
                    if (!await _context.Suppliers.AnyAsync(s => s.SupplierID == purchase.SupplierID))
                    {
                        ModelState.AddModelError("SupplierID", "Mã nhà cung cấp không tồn tại.");
                        ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
                        ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
                        return View(purchase);
                    }

                    // Kiểm tra UnitPrice hợp lệ
                    if (purchase.UnitPrice < 0)
                    {
                        ModelState.AddModelError("UnitPrice", "Đơn giá không được âm.");
                        ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
                        ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
                        return View(purchase);
                    }

                    purchase.TotalPrice = purchase.Quantity * purchase.UnitPrice;
                    _context.Update(purchase);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sửa mua hàng thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PurchaseExists(purchase.PurchaseID))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            ViewBag.AssetCodes = await _context.Devices.Select(d => d.AssetCode).ToListAsync();
            ViewBag.SupplierIDs = await _context.Suppliers.Select(s => s.SupplierID).ToListAsync();
            return View(purchase);
        }

        // GET: Purchases/Delete/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var purchase = await _context.Purchases.FirstOrDefaultAsync(m => m.PurchaseID == id);
            if (purchase == null)
            {
                return NotFound();
            }
            return View(purchase);
        }

        // POST: Purchases/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase != null)
            {
                _context.Purchases.Remove(purchase);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa mua hàng thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Purchases/ImportExcel
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
                    var supplierID = worksheet.Cells[row, 6].Value?.ToString();

                    if (string.IsNullOrEmpty(assetCode) || string.IsNullOrEmpty(quantityStr) || string.IsNullOrEmpty(unitPriceStr) ||
                        string.IsNullOrEmpty(timeStr) || string.IsNullOrEmpty(department) || string.IsNullOrEmpty(supplierID))
                    {
                        continue; // Bỏ qua dòng thiếu dữ liệu
                    }

                    if (!int.TryParse(quantityStr, out var quantity) ||
                        !decimal.TryParse(unitPriceStr, out var unitPrice) ||
                        !DateTime.TryParse(timeStr, out var time))
                    {
                        continue; // Bỏ qua nếu dữ liệu không hợp lệ
                    }

                    if (!await _context.Devices.AnyAsync(d => d.AssetCode == assetCode) ||
                        !await _context.Suppliers.AnyAsync(s => s.SupplierID == supplierID))
                    {
                        continue; // Bỏ qua nếu AssetCode hoặc SupplierID không tồn tại
                    }

                    var purchase = new Purchase
                    {
                        AssetCode = assetCode,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Time = time,
                        Department = department,
                        SupplierID = supplierID,
                        TotalPrice = quantity * unitPrice
                    };

                    _context.Add(purchase);
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

        // GET: Purchases/ExportExcel
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ExportExcel()
        {
            var purchases = await _context.Purchases.ToListAsync();
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Purchases");
            worksheet.Cells[1, 1].Value = "Mã thiết bị";
            worksheet.Cells[1, 2].Value = "Số lượng";
            worksheet.Cells[1, 3].Value = "Đơn giá";
            worksheet.Cells[1, 4].Value = "Thời gian";
            worksheet.Cells[1, 5].Value = "Bộ phận";
            worksheet.Cells[1, 6].Value = "Nhà cung cấp";
            worksheet.Cells[1, 7].Value = "Tổng giá";

            var vietnameseCulture = new CultureInfo("vi-VN");
            for (int i = 0; i < purchases.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = purchases[i].AssetCode;
                worksheet.Cells[i + 2, 2].Value = purchases[i].Quantity;
                worksheet.Cells[i + 2, 3].Value = purchases[i].UnitPrice.ToString("N0", vietnameseCulture);
                worksheet.Cells[i + 2, 4].Value = purchases[i].Time.ToString("MM-yyyy");
                worksheet.Cells[i + 2, 5].Value = purchases[i].Department;
                worksheet.Cells[i + 2, 6].Value = purchases[i].SupplierID;
                worksheet.Cells[i + 2, 7].Value = purchases[i].TotalPrice.ToString("N0", vietnameseCulture);
            }

            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Purchases.xlsx");
        }

        // POST: Purchases/DeleteAll
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                var purchases = await _context.Purchases.ToListAsync();
                _context.Purchases.RemoveRange(purchases);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa toàn bộ dữ liệu.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa toàn bộ dữ liệu: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PurchaseExists(int id)
        {
            return _context.Purchases.Any(e => e.PurchaseID == id);
        }
    }
}