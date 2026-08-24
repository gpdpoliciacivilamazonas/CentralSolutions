using CentralSolutions.Data;
using CentralSolutions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CentralSolutions.Controllers;

public class SupportTicketsController(ApplicationDbContext context) : Controller
{
    private static readonly string[] Departments =
    [
        "APOIO",
        "ARMAMENTO",
        "ARQUIVO",
        "CAF",
        "CAPACITAÇÃO",
        "CHEFIA",
        "COMPRAS",
        "CONTRATOS",
        "CORE",
        "CVA",
        "DAF",
        "DCA",
        "DENARC",
        "DEPLAN",
        "DERCC",
        "DGA",
        "DIPC",
        "DPI",
        "DPM",
        "DRAD",
        "DTI",
        "ENGENHARIA",
        "ESTATISTICA",
        "GA",
        "GEPAT",
        "GETRAN",
        "GMF",
        "GOFIN",
        "GP",
        "IDENTIFICAÇÃO",
        "IMPRENSA",
        "JUNTA MÉDICA",
        "JURÍDICO",
        "NURATI",
        "OUVIDORIA",
        "PROMOÇÃO",
        "PROTOCOLO",
        "UAIP",
        "DELEGACIA EXTERNA"
    ];

    private static readonly string[] TicketTypes =
    [
        "Rede",
        "Hardware",
        "Sistema",
        "Outros"
    ];

    public async Task<IActionResult> Index(string? department, TicketResolutionStatus? status)
    {
        IQueryable<SupportTicket> tickets = context.SupportTickets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(department))
        {
            tickets = tickets.Where(ticket => ticket.Department == department);
        }

        if (status.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.ResolutionStatus == status.Value);
        }

        ViewBag.Departments = GetDepartmentSelectList(department);
        ViewBag.Statuses = GetStatusSelectList(status);

        return View(await tickets
            .OrderBy(ticket => ticket.Department)
            .ThenByDescending(ticket => ticket.CreatedAt)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var ticket = await context.SupportTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    public IActionResult Create()
    {
        PopulateSelectLists();
        return View(new SupportTicket());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Department,TicketType,ResponsibleTechnician,Resolution,ResolutionStatus")] SupportTicket ticket)
    {
        if (!ModelState.IsValid)
        {
            PopulateSelectLists(ticket.Department, ticket.TicketType, ticket.ResolutionStatus);
            return View(ticket);
        }

        ticket.CreatedAt = DateTime.UtcNow;
        context.Add(ticket);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var ticket = await context.SupportTickets.FindAsync(id);

        if (ticket is null)
        {
            return NotFound();
        }

        PopulateSelectLists(ticket.Department, ticket.TicketType, ticket.ResolutionStatus);
        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Department,TicketType,ResponsibleTechnician,Resolution,ResolutionStatus,CreatedAt")] SupportTicket ticket)
    {
        if (id != ticket.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            PopulateSelectLists(ticket.Department, ticket.TicketType, ticket.ResolutionStatus);
            return View(ticket);
        }

        try
        {
            ticket.UpdatedAt = DateTime.UtcNow;
            context.Update(ticket);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await TicketExists(ticket.Id))
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

        var ticket = await context.SupportTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ticket = await context.SupportTickets.FindAsync(id);

        if (ticket is not null)
        {
            context.SupportTickets.Remove(ticket);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> TicketExists(int id)
    {
        return await context.SupportTickets.AnyAsync(ticket => ticket.Id == id);
    }

    private void PopulateSelectLists(string? selectedDepartment = null, string? selectedTicketType = null, TicketResolutionStatus? selectedStatus = null)
    {
        ViewBag.Departments = GetDepartmentSelectList(selectedDepartment);
        ViewBag.TicketTypes = TicketTypes.Select(type => new SelectListItem(type, type, type == selectedTicketType)).ToList();
        ViewBag.Statuses = GetStatusSelectList(selectedStatus);
    }

    private static List<SelectListItem> GetDepartmentSelectList(string? selectedDepartment)
    {
        return Departments.Select(department => new SelectListItem(department, department, department == selectedDepartment)).ToList();
    }

    private static List<SelectListItem> GetStatusSelectList(TicketResolutionStatus? selectedStatus)
    {
        return Enum.GetValues<TicketResolutionStatus>()
            .Select(status => new SelectListItem(GetStatusName(status), status.ToString(), selectedStatus == status))
            .ToList();
    }

    private static string GetStatusName(TicketResolutionStatus status)
    {
        return status switch
        {
            TicketResolutionStatus.Open => "Em aberto",
            TicketResolutionStatus.No => "Não",
            TicketResolutionStatus.Yes => "Sim",
            _ => status.ToString()
        };
    }
}
