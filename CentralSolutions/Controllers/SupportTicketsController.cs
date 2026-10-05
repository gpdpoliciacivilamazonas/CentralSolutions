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
			.OrderByDescending(ticket => ticket.TicketNumber)
			.ToListAsync());
	}

	[AllowAnonymous]
	[HttpGet("c/{code}")]
	public async Task<IActionResult> ByCode(string code)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			return NotFound();
		}

		Guid ticketId;
		try
		{
			var bytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(code);
			if (bytes.Length != 16)
			{
				return NotFound();
			}

			ticketId = new Guid(bytes);
		}
		catch
		{
			return NotFound();
		}

		return await Details(ticketId);
	}

	public static string ToShortSlug(Guid id)
	{
		return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(id.ToByteArray());
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
				.Include(item => item.Updates)
				.FirstOrDefaultAsync(item => item.Id == id);

			if (ticket is null)
			{
				return NotFound();
			}

			ticket.Updates = ticket.Updates.OrderByDescending(u => u.CreatedAt).ToList();
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
				ResolutionStatus = ticket.ResolutionStatus,
				CreatedAt = ticket.CreatedAt,
				UpdatedAt = ticket.UpdatedAt,
				Updates = ticket.Updates
					.OrderByDescending(u => u.CreatedAt)
					.Select(u => new TicketUpdateViewModel
					{
						Description = u.Description,
						CreatedBy = u.CreatedBy,
						CreatedAt = u.CreatedAt
					})
					.ToList()
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
			.Include(item => item.Updates)
			.FirstOrDefaultAsync(item => item.TicketNumber == ticketNumber);

		if (ticket is null)
		{
			return NotFound();
		}

		var latestUpdate = ticket.Updates.OrderByDescending(u => u.CreatedAt).FirstOrDefault()?.Description ?? "-";

		var csv = new StringBuilder();
		csv.AppendLine("Campo;Valor");
		csv.AppendLine($"Nº;{ticket.TicketNumber}");
		csv.AppendLine($"Setor;{EscapeCsv(ticket.Department)}");
		csv.AppendLine($"Chamado;{EscapeCsv(ticket.TicketType)}");
		csv.AppendLine($"Responsável;{EscapeCsv(ticket.ResponsibleTechnician)}");
		csv.AppendLine($"Problema;{EscapeCsv(ticket.Problem)}");
		csv.AppendLine($"Última Atualização;{EscapeCsv(latestUpdate)}");
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
	public async Task<IActionResult> Create([Bind("Department,TicketType,ResponsibleTechnician,Problem")] SupportTicket ticket)
	{
		if (!ModelState.IsValid)
		{
			await PopulateSelectLists(ticket.Department, ticket.TicketType, TicketResolutionStatus.Open);
			return View(ticket);
		}

		ticket.ResolutionStatus = TicketResolutionStatus.Open;
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

	[HttpGet]
	public async Task<IActionResult> AddUpdate(Guid? id)
	{
		if (id is null)
		{
			return NotFound();
		}

		var ticket = await context.SupportTickets
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Id == id);

		if (ticket is null)
		{
			return NotFound();
		}

		var model = new AddTicketUpdateViewModel
		{
			TicketId = ticket.Id,
			TicketNumber = ticket.TicketNumber,
			Department = ticket.Department,
			TicketType = ticket.TicketType,
			ResponsibleTechnician = ticket.ResponsibleTechnician,
			Problem = ticket.Problem,
			CurrentStatus = ticket.ResolutionStatus,
			CreatedBy = ticket.ResponsibleTechnician ?? User.Identity?.Name ?? string.Empty,
			MarkAsDone = false
		};

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AddUpdate(AddTicketUpdateViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var ticket = await context.SupportTickets
			.FirstOrDefaultAsync(t => t.Id == model.TicketId);

		if (ticket is null)
		{
			return NotFound();
		}

		var author = !string.IsNullOrWhiteSpace(model.CreatedBy)
			? model.CreatedBy.Trim()
			: (User.Identity?.Name ?? "Técnico");

		var update = new TicketUpdate
		{
			SupportTicketId = ticket.Id,
			Description = model.Description.Trim(),
			CreatedBy = author,
			CreatedAt = DateTime.UtcNow
		};

		context.TicketUpdates.Add(update);

		ticket.ResolutionStatus = model.MarkAsDone
			? TicketResolutionStatus.Done
			: TicketResolutionStatus.OnGoing;

		ticket.UpdatedAt = DateTime.UtcNow;

		await context.SaveChangesAsync();

		return RedirectToAction(nameof(Details), new { id = ticket.Id });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CloseTicket(Guid id)
	{
		var ticket = await context.SupportTickets.FindAsync(id);

		if (ticket is null)
		{
			return NotFound();
		}

		ticket.ResolutionStatus = TicketResolutionStatus.Done;
		ticket.UpdatedAt = DateTime.UtcNow;

		await context.SaveChangesAsync();

		return RedirectToAction(nameof(Details), new { id = ticket.Id });
	}

	public async Task<IActionResult> Edit(Guid? id, string? returnUrl = null)
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

		ViewBag.ReturnUrl = returnUrl;
		await PopulateSelectLists(ticket.Department, ticket.TicketType, ticket.ResolutionStatus);
		return View(ticket);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(Guid id, [Bind("Id,TicketNumber,Department,TicketType,ResponsibleTechnician,Problem,ResolutionStatus,CreatedAt")] SupportTicket ticket, string? returnUrl = null)
	{
		if (id != ticket.Id)
		{
			return NotFound();
		}

		if (!ModelState.IsValid)
		{
			ViewBag.ReturnUrl = returnUrl;
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

		if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
		{
			return LocalRedirect(returnUrl);
		}

		return RedirectToAction(nameof(Details), new { id = ticket.Id });
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
			TicketResolutionStatus.OnGoing => "Em andamento",
			TicketResolutionStatus.Done => "Concluído",
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
