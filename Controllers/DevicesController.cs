using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITWebManagement.Data;
using ITWebManagement.Models;

namespace ITWebManagement.Controllers
{
    public class DevicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DevicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Devices
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            var devices = _context.Devices.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                // Lọc các trường kiểu string
                devices = devices.Where(d => d.AssetCode.Contains(searchString) ||
                                            d.Model.Contains(searchString) ||
                                            d.Department.Contains(searchString) ||
                                            d.Status.Contains(searchString));

                // Lọc Depreciation riêng nếu searchString là số hợp lệ
                if (decimal.TryParse(searchString, out decimal searchDecimal))
                {
                    devices = devices.Where(d => d.Depreciation == searchDecimal);
                }
            }
            return View(await devices.ToListAsync());
        }

        // GET: Devices/Create
        [Authorize(Roles = "admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Devices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([Bind("AssetCode,Model,StartDate,EndDate,Depreciation,Department,Status")] Device device)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra Depreciation hợp lệ
                if (device.Depreciation < 0)
                {
                    ModelState.AddModelError("Depreciation", "Khấu hao không được âm.");
                    return View(device);
                }

                _context.Add(device);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm thiết bị thành công.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(device);
        }

        // GET: Devices/Edit/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound();
            }
            return View(device);
        }

        // POST: Devices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(string id, [Bind("AssetCode,Model,StartDate,EndDate,Depreciation,Department,Status")] Device device)
        {
            if (id != device.AssetCode)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra Depreciation hợp lệ
                if (device.Depreciation < 0)
                {
                    ModelState.AddModelError("Depreciation", "Khấu hao không được âm.");
                    return View(device);
                }

                try
                {
                    _context.Update(device);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sửa thiết bị thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeviceExists(device.AssetCode))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(device);
        }

        // GET: Devices/Delete/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _context.Devices.FirstOrDefaultAsync(m => m.AssetCode == id);
            if (device == null)
            {
                return NotFound();
            }
            return View(device);
        }

        // POST: Devices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device != null)
            {
                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa thiết bị thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DeviceExists(string id)
        {
            return _context.Devices.Any(e => e.AssetCode == id);
        }
    }
}