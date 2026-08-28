using CentralSolutions.Data;
using CentralSolutions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralSolutions.Controllers;

public class DepartmentsController(ApplicationDbContext context) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        IQueryable<Department> departments = context.Departments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            departments = departments.Where(department => department.Name.Contains(search));
        }

        var totalItems = await departments.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = PageSize;

        return View(await departments
            .OrderBy(department => department.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync());
    }

    public IActionResult Create()
    {
        return View(new Department());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,IsActive")] Department department)
    {
        department.Name = department.Name.Trim();

        if (await DepartmentNameExists(department.Name))
        {
            ModelState.AddModelError(nameof(Department.Name), "Já existe um setor com este nome.");
        }

        if (!ModelState.IsValid)
        {
            return View(department);
        }

        department.CreatedAt = DateTime.UtcNow;
        context.Add(department);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var department = await context.Departments.FindAsync(id);

        if (department is null)
        {
            return NotFound();
        }

        return View(department);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IsActive,CreatedAt")] Department department)
    {
        if (id != department.Id)
        {
            return NotFound();
        }

        department.Name = department.Name.Trim();

        if (await DepartmentNameExists(department.Name, department.Id))
        {
            ModelState.AddModelError(nameof(Department.Name), "Já existe outro setor com este nome.");
        }

        if (!ModelState.IsValid)
        {
            return View(department);
        }

        try
        {
            department.UpdatedAt = DateTime.UtcNow;
            context.Update(department);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await DepartmentExists(department.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var department = await context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (department is null)
        {
            return NotFound();
        }

        return View(department);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var department = await context.Departments.FindAsync(id);

        if (department is not null)
        {
            context.Departments.Remove(department);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> DepartmentExists(int id)
    {
        return await context.Departments.AnyAsync(department => department.Id == id);
    }

    private async Task<bool> DepartmentNameExists(string name, int? currentId = null)
    {
        return await context.Departments.AnyAsync(department =>
            department.Name == name && (!currentId.HasValue || department.Id != currentId.Value));
    }
}
