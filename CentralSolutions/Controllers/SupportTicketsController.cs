using CentralSolutions.Data;
using CentralSolutions.Models;
using CentralSolutions.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CentralSolutions.Controllers;

[Authorize]
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
			tickets = tickets.Where(ticket => ticket.TicketNumber == ticketNumber.Value);
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
			.OrderByDescending(ticket => ticket.Id)
			.ToListAsync());
	}

	[AllowAnonymous]
	public async Task<IActionResult> Details(Guid? id)
	{
		if (id is null)
		{
			return NotFound();
		}

		if (User.Identity?.IsAuthenticated == true)
		{
			var ticket = await context.SupportTickets
				.FirstOrDefaultAsync(item => item.Id == id);

			if (ticket is null)
			{
				return NotFound();
			}

			return View("Details", ticket);
		}

		var publicTicket = await context.SupportTickets
			.Where(ticket => ticket.Id == id)
			.Select(ticket => new SupportTicketPublicViewModel
			{
				TicketNumber = ticket.TicketNumber,
				Department = ticket.Department,
				TicketType = ticket.TicketType,
				ResponsibleTechnician = ticket.ResponsibleTechnician,
				Problem = ticket.Problem,
				Solution = ticket.Solution,
				ResolutionStatus = ticket.ResolutionStatus,
				CreatedAt = ticket.CreatedAt,
				UpdatedAt = ticket.UpdatedAt
			})
			.FirstOrDefaultAsync();

		if (publicTicket == null)
		{
			return NotFound();
		}

		return View("PublicDetails", publicTicket);
	}

	[AllowAnonymous]
	[HttpGet("SupportTickets/{ticketNumber:int}/exportar-excel")]
	public async Task<IActionResult> Export(int ticketNumber)
	{
		var ticket = await context.SupportTickets
			.AsNoTracking()
			.FirstOrDefaultAsync(item => item.TicketNumber == ticketNumber);

		if (ticket is null)
		{
			return NotFound();
		}

		var csv = new StringBuilder();
		csv.AppendLine("Campo;Valor");
		csv.AppendLine($"Nº;{ticket.TicketNumber}");
		csv.AppendLine($"Setor;{EscapeCsv(ticket.Department)}");
		csv.AppendLine($"Chamado;{EscapeCsv(ticket.TicketType)}");
		csv.AppendLine($"Responsável;{EscapeCsv(ticket.ResponsibleTechnician)}");
		csv.AppendLine($"Problema;{EscapeCsv(ticket.Problem)}");
		csv.AppendLine($"Solução;{EscapeCsv(ticket.Solution)}");
		csv.AppendLine($"Status;{EscapeCsv(GetStatusName(ticket.ResolutionStatus))}");
		csv.AppendLine($"Criado em;{ticket.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}");
		csv.AppendLine($"Atualizado em;{(ticket.UpdatedAt.HasValue ? ticket.UpdatedAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "-")}");

		var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
		return File(bytes, "application/vnd.ms-excel; charset=utf-8", $"chamado-{ticket.TicketNumber}.csv");
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

		for (var attempt = 0; attempt < 3; attempt++)
		{
			var lastNumber = await context.SupportTickets
			  .MaxAsync(x => (int?)x.TicketNumber) ?? 0;

			ticket.TicketNumber = lastNumber + 1;

			try
			{
				context.SupportTickets.Add(ticket);
				await context.SaveChangesAsync();

				return RedirectToAction(nameof(Index));
			}
			catch (DbUpdateException)
			{
				context.Entry(ticket).State = EntityState.Detached;

				if (attempt == 2)
				{
					throw;
				}
			}
		}
		var errorMsg = "Não foi possivel gerar o número do chamado. ";
		throw new InvalidOperationException(errorMsg);

	}

	public async Task<IActionResult> Edit(Guid? id)
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
	public async Task<IActionResult> Edit(Guid id, [Bind("Id,TicketNumber,Department,TicketType,ResponsibleTechnician,Problem,Solution,ResolutionStatus,CreatedAt")] SupportTicket ticket)
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

	public async Task<IActionResult> Delete(Guid? id)
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
	public async Task<IActionResult> DeleteConfirmed(Guid id)
	{
		var ticket = await context.SupportTickets.FindAsync(id);

		if (ticket is not null)
		{
			context.SupportTickets.Remove(ticket);
			await context.SaveChangesAsync();
		}

		return RedirectToAction(nameof(Index));
	}

	private async Task<bool> TicketExists(Guid id)
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
			TicketResolutionStatus.No => "Incompleto",
			TicketResolutionStatus.Yes => "Completo",
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
