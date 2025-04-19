using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITWebManagement.Data;
using ITWebManagement.Models;

namespace ITWebManagement.Controllers
{
    public class OutboundController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OutboundController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Outbound
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Outbounds.ToListAsync());
        }

        // GET: Outbound/Create
        [Authorize(Roles = "admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Outbound/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([Bind("AssetCode,Quantity,Time,ExportedBy")] Outbound outbound)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra số lượng tồn kho trước khi xuất
                var totalImported = await _context.Inventory
                    .Where(i => i.AssetCode == outbound.AssetCode && i.TransactionType == "Import")
                    .SumAsync(i => i.Quantity);
                var totalExported = await _context.Inventory
                    .Where(i => i.AssetCode == outbound.AssetCode && i.TransactionType == "Export")
                    .SumAsync(i => i.Quantity);
                var totalOutbound = await _context.Outbounds
                    .Where(o => o.AssetCode == outbound.AssetCode)
                    .SumAsync(o => o.Quantity);
                var currentStock = totalImported - totalExported - totalOutbound;

                if (outbound.Quantity > currentStock)
                {
                    ModelState.AddModelError("Quantity", $"Số lượng xuất kho vượt quá tồn kho hiện tại ({currentStock}).");
                    return View(outbound);
                }

                _context.Add(outbound);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm giao dịch xuất kho thành công.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(outbound);
        }

        // GET: Outbound/Edit/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var outbound = await _context.Outbounds.FindAsync(id);
            if (outbound == null)
            {
                return NotFound();
            }
            return View(outbound);
        }

        // POST: Outbound/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id, [Bind("OutboundID,AssetCode,Quantity,Time,ExportedBy")] Outbound outbound)
        {
            if (id != outbound.OutboundID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra số lượng tồn kho
                    var totalImported = await _context.Inventory
                        .Where(i => i.AssetCode == outbound.AssetCode && i.TransactionType == "Import")
                        .SumAsync(i => i.Quantity);
                    var totalExported = await _context.Inventory
                        .Where(i => i.AssetCode == outbound.AssetCode && i.TransactionType == "Export")
                        .SumAsync(i => i.Quantity);
                    var totalOutbound = await _context.Outbounds
                        .Where(o => o.AssetCode == outbound.AssetCode && o.OutboundID != id)
                        .SumAsync(o => o.Quantity);
                    var currentStock = totalImported - totalExported - totalOutbound;

                    if (outbound.Quantity > currentStock)
                    {
                        ModelState.AddModelError("Quantity", $"Số lượng xuất kho vượt quá tồn kho hiện tại ({currentStock}).");
                        return View(outbound);
                    }

                    _context.Update(outbound);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sửa giao dịch xuất kho thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OutboundExists(outbound.OutboundID))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(outbound);
        }

        // GET: Outbound/Delete/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var outbound = await _context.Outbounds.FirstOrDefaultAsync(m => m.OutboundID == id);
            if (outbound == null)
            {
                return NotFound();
            }
            return View(outbound);
        }

        // POST: Outbound/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var outbound = await _context.Outbounds.FindAsync(id);
            if (outbound != null)
            {
                _context.Outbounds.Remove(outbound);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa giao dịch xuất kho thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool OutboundExists(int id)
        {
            return _context.Outbounds.Any(e => e.OutboundID == id);
        }
    }
}