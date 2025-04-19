using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITWebManagement.Data;
using ITWebManagement.Models;

namespace ITWebManagement.Controllers
{
    public class ConfigController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfigController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Config
        [Authorize(Roles = "admin")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: Config/RegisterDevice
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RegisterDevice(string searchString)
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

        // GET: Config/RegisterDeviceCreate
        [Authorize(Roles = "admin")]
        public IActionResult RegisterDeviceCreate()
        {
            return View();
        }

        // POST: Config/RegisterDeviceCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RegisterDeviceCreate([Bind("AssetCode,Model,StartDate,EndDate,Depreciation,Department,Status")] Device device)
        {
            if (ModelState.IsValid)
            {
                _context.Add(device);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm mã thiết bị thành công.";
                return RedirectToAction(nameof(RegisterDevice));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(device);
        }

        // GET: Config/RegisterDepartment
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RegisterDepartment(string searchString)
        {
            var departments = _context.Departments.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                departments = departments.Where(d => d.DepartmentCode.Contains(searchString) ||
                                                    d.DepartmentName.Contains(searchString));
            }
            return View(await departments.ToListAsync());
        }

        // GET: Config/RegisterDepartmentCreate
        [Authorize(Roles = "admin")]
        public IActionResult RegisterDepartmentCreate()
        {
            return View();
        }

        // POST: Config/RegisterDepartmentCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RegisterDepartmentCreate([Bind("DepartmentCode,DepartmentName")] Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm bộ phận thành công.";
                return RedirectToAction(nameof(RegisterDepartment));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(department);
        }

        // GET: Config/EditDepartment/DEPT001
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EditDepartment(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }

        // POST: Config/EditDepartment/DEPT001
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EditDepartment(string id, [Bind("DepartmentCode,DepartmentName")] Department department)
        {
            if (id != department.DepartmentCode)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sửa bộ phận thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DepartmentCode))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(RegisterDepartment));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(department);
        }

        // GET: Config/RegisterSupplier
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RegisterSupplier(string searchString)
        {
            var suppliers = _context.Suppliers.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                suppliers = suppliers.Where(s => s.SupplierID.Contains(searchString) ||
                                                s.SupplierName.Contains(searchString));
            }
            return View(await suppliers.ToListAsync());
        }

        // GET: Config/RegisterSupplierCreate
        [Authorize(Roles = "admin")]
        public IActionResult RegisterSupplierCreate()
        {
            return View();
        }

        // POST: Config/RegisterSupplierCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RegisterSupplierCreate([Bind("SupplierID,SupplierName")] Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm nhà cung cấp thành công.";
                return RedirectToAction(nameof(RegisterSupplier));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(supplier);
        }

        // GET: Config/EditSupplier/SUP001
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EditSupplier(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        // POST: Config/EditSupplier/SUP001
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EditSupplier(string id, [Bind("SupplierID,SupplierName")] Supplier supplier)
        {
            if (id != supplier.SupplierID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sửa nhà cung cấp thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.SupplierID))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(RegisterSupplier));
            }
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return View(supplier);
        }

        private bool DepartmentExists(string id)
        {
            return _context.Departments.Any(e => e.DepartmentCode == id);
        }

        private bool SupplierExists(string id)
        {
            return _context.Suppliers.Any(e => e.SupplierID == id);
        }
    }
}