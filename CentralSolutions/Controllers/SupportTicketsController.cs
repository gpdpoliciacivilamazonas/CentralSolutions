using CentralSolutions.Data;
using CentralSolutions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CentralSolutions.Controllers;

public class SupportTicketsController(ApplicationDbContext context) : Controller
{
    private static readonly string[] TicketTypes =
    [
        "Rede",
        "Computador",
        "Impressora",
        "Periféricos",
        "E-mail",
        "Sistema",
        "Outros"
    ];

    public async Task<IActionResult> Index(int? ticketNumber, string? department, TicketResolutionStatus? status)
    {
        IQueryable<SupportTicket> tickets = context.SupportTickets.AsNoTracking();

        if (ticketNumber.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.Id == ticketNumber.Value);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            tickets = tickets.Where(ticket => ticket.Department.Contains(department));
        }

        if (status.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.ResolutionStatus == status.Value);
        }

        ViewBag.Departments = await GetDepartmentSelectList(department);
        ViewBag.TicketNumber = ticketNumber;
        ViewBag.SelectedDepartment = department;
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

    public async Task<IActionResult> Export(int? id)
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

        var csv = new StringBuilder();
        csv.AppendLine("Campo;Valor");
        csv.AppendLine($"Nº;{ticket.Id}");
        csv.AppendLine($"Setor;{EscapeCsv(ticket.Department)}");
        csv.AppendLine($"Chamado;{EscapeCsv(ticket.TicketType)}");
        csv.AppendLine($"Responsável;{EscapeCsv(ticket.ResponsibleTechnician)}");
        csv.AppendLine($"Problema;{EscapeCsv(ticket.Problem)}");
        csv.AppendLine($"Solução;{EscapeCsv(ticket.Solution)}");
        csv.AppendLine($"Status;{EscapeCsv(GetStatusName(ticket.ResolutionStatus))}");
        csv.AppendLine($"Criado em;{ticket.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}");
        csv.AppendLine($"Atualizado em;{(ticket.UpdatedAt.HasValue ? ticket.UpdatedAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "-")}");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"chamado-{ticket.Id}.csv");
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectLists();
        return View(new SupportTicket());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Department,TicketType,ResponsibleTechnician,Problem,Solution,ResolutionStatus")] SupportTicket ticket)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(ticket.Department, ticket.TicketType, ticket.ResolutionStatus);
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

        await PopulateSelectLists(ticket.Department, ticket.TicketType, ticket.ResolutionStatus);
        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Department,TicketType,ResponsibleTechnician,Problem,Solution,ResolutionStatus,CreatedAt")] SupportTicket ticket)
    {
        if (id != ticket.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(ticket.Department, ticket.TicketType, ticket.ResolutionStatus);
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

    private async Task PopulateSelectLists(string? selectedDepartment = null, string? selectedTicketType = null, TicketResolutionStatus? selectedStatus = null)
    {
        ViewBag.Departments = await GetDepartmentSelectList(selectedDepartment);
        ViewBag.TicketTypes = TicketTypes.Select(type => new SelectListItem(type, type, type == selectedTicketType)).ToList();
        ViewBag.Statuses = GetStatusSelectList(selectedStatus);
    }

    private async Task<List<SelectListItem>> GetDepartmentSelectList(string? selectedDepartment)
    {
        var departments = await context.Departments
            .AsNoTracking()
            .Where(department => department.IsActive || department.Name == selectedDepartment)
            .OrderBy(department => department.Name)
            .Select(department => department.Name)
            .ToListAsync();

        return departments.Select(department => new SelectListItem(department, department, department == selectedDepartment)).ToList();
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
            TicketResolutionStatus.InProgress => "Em andamento",
            _ => status.ToString()
        };
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;

        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
