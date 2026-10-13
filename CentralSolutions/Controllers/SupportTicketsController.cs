using CentralSolutions.Data;
using CentralSolutions.Models;
using CentralSolutions.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CentralSolutions.Controllers;

[Authorize]
public class SupportTicketsController(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration) : Controller
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

	private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		// Imagens
		".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg",
		// Vídeos
		".mp4", ".webm", ".avi", ".mov", ".mkv", ".m4v",
		// Documentos
		".pdf"
	};

	private string GetAttachmentsDirectory()
	{
		var dir = configuration["AttachmentsDirectory"] ?? "attachments";
		if (!Path.IsPathRooted(dir))
		{
			dir = Path.Combine(environment.ContentRootPath, dir);
		}
		Directory.CreateDirectory(dir);
		return dir;
	}

	private async Task<TicketAttachment?> ProcessAndSaveFileAsync(Guid ticketId, IFormFile file)
	{
		if (file.Length == 0) return null;

		var extension = Path.GetExtension(file.FileName);
		if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
		{
			return null;
		}

		var originalFileName = Path.GetFileName(file.FileName);
		var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
		var filePath = Path.Combine(GetAttachmentsDirectory(), storedFileName);

		await using (var stream = new FileStream(filePath, FileMode.Create))
		{
			await file.CopyToAsync(stream);
		}

		return new TicketAttachment
		{
			Id = Guid.NewGuid(),
			SupportTicketId = ticketId,
			FileName = originalFileName,
			StoredFileName = storedFileName,
			ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
			FileSize = file.Length,
			CreatedAt = DateTime.UtcNow
		};
	}

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
		if (!TryDecodeTicketCode(code, out var ticketId))
		{
			return NotFound();
		}

		ViewBag.Code = code;
		return await Details(ticketId);
	}

	private static bool TryDecodeTicketCode(string code, out Guid ticketId)
	{
		ticketId = Guid.Empty;
		if (string.IsNullOrWhiteSpace(code))
		{
			return false;
		}

		try
		{
			var bytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(code);
			if (bytes.Length != 16)
			{
				return false;
			}

			ticketId = new Guid(bytes);
			return true;
		}
		catch
		{
			return false;
		}
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
				.Include(item => item.Attachments)
				.FirstOrDefaultAsync(item => item.Id == id);

			if (ticket is null)
			{
				return NotFound();
			}

			ticket.Updates = ticket.Updates.OrderByDescending(u => u.CreatedAt).ToList();
			ticket.Attachments = ticket.Attachments.OrderByDescending(a => a.CreatedAt).ToList();
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
					.ToList(),
				Attachments = ticket.Attachments
					.OrderByDescending(a => a.CreatedAt)
					.Select(a => new TicketAttachmentViewModel
					{
						Id = a.Id,
						FileName = a.FileName,
						ContentType = a.ContentType,
						FileSize = a.FileSize,
						CreatedAt = a.CreatedAt
					})
					.ToList()
			})
			.FirstOrDefaultAsync();

		if (publicTicket == null)
		{
			return NotFound();
		}

		ViewBag.Code ??= id.HasValue ? ToShortSlug(id.Value) : string.Empty;
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
		csv.AppendLine($"Criado em;{ticket.CreatedAt.ToDisplayString()}");
		csv.AppendLine($"Atualizado em;{ticket.UpdatedAt.ToDisplayString()}");

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
	public async Task<IActionResult> Create([Bind("Department,TicketType,ResponsibleTechnician,Problem")] SupportTicket ticket, List<IFormFile>? attachments)
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

				if (attachments != null && attachments.Count > 0)
				{
					foreach (var file in attachments)
					{
						var attachment = await ProcessAndSaveFileAsync(ticket.Id, file);
						if (attachment != null)
						{
							context.TicketAttachments.Add(attachment);
						}
					}
					await context.SaveChangesAsync();
				}

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
		var ticket = await context.SupportTickets
			.Include(t => t.Attachments)
			.FirstOrDefaultAsync(t => t.Id == id);

		if (ticket is not null)
		{
			var attachmentsDir = GetAttachmentsDirectory();
			foreach (var attachment in ticket.Attachments)
			{
				var filePath = Path.Combine(attachmentsDir, attachment.StoredFileName);
				if (System.IO.File.Exists(filePath))
				{
					try { System.IO.File.Delete(filePath); } catch { /* ignore */ }
				}
			}

			context.SupportTickets.Remove(ticket);
			await context.SaveChangesAsync();
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UploadAttachments(Guid id, List<IFormFile> files)
	{
		var ticket = await context.SupportTickets.FindAsync(id);
		if (ticket is null)
		{
			return NotFound();
		}

		if (files != null && files.Count > 0)
		{
			foreach (var file in files)
			{
				var attachment = await ProcessAndSaveFileAsync(ticket.Id, file);
				if (attachment != null)
				{
					context.TicketAttachments.Add(attachment);
				}
			}
			await context.SaveChangesAsync();
		}

		return RedirectToAction(nameof(Details), new { id = ticket.Id });
	}

	[HttpGet("SupportTickets/attachment/{id:guid}")]
	public async Task<IActionResult> DownloadAttachment(Guid id, bool download = false)
	{
		var attachment = await context.TicketAttachments.FindAsync(id);
		if (attachment is null)
		{
			return NotFound();
		}

		var filePath = Path.Combine(GetAttachmentsDirectory(), attachment.StoredFileName);
		if (!System.IO.File.Exists(filePath))
		{
			return NotFound();
		}

		if (download)
		{
			return PhysicalFile(filePath, attachment.ContentType, attachment.FileName);
		}

		return PhysicalFile(filePath, attachment.ContentType);
	}

	[HttpGet("SupportTickets/{id:guid}/download-zip")]
	public async Task<IActionResult> DownloadAllAttachments(Guid id)
	{
		var ticket = await context.SupportTickets
			.Include(t => t.Attachments)
			.FirstOrDefaultAsync(t => t.Id == id);

		if (ticket is null || ticket.Attachments.Count == 0)
		{
			return NotFound();
		}

		return GenerateZipResult(ticket);
	}

	[AllowAnonymous]
	[HttpGet("c/{code}/zip")]
	public async Task<IActionResult> DownloadZipByCode(string code)
	{
		if (!TryDecodeTicketCode(code, out var ticketId))
		{
			return NotFound();
		}

		var ticket = await context.SupportTickets
			.Include(t => t.Attachments)
			.FirstOrDefaultAsync(t => t.Id == ticketId);

		if (ticket is null || ticket.Attachments.Count == 0)
		{
			return NotFound();
		}

		return GenerateZipResult(ticket);
	}

	[AllowAnonymous]
	[HttpGet("c/{code}/anexo/{id:guid}")]
	public async Task<IActionResult> DownloadAttachmentByCode(string code, Guid id, bool download = false)
	{
		if (!TryDecodeTicketCode(code, out var ticketId))
		{
			return NotFound();
		}

		var attachment = await context.TicketAttachments
			.FirstOrDefaultAsync(a => a.Id == id && a.SupportTicketId == ticketId);

		if (attachment is null)
		{
			return NotFound();
		}

		var filePath = Path.Combine(GetAttachmentsDirectory(), attachment.StoredFileName);
		if (!System.IO.File.Exists(filePath))
		{
			return NotFound();
		}

		if (download)
		{
			return PhysicalFile(filePath, attachment.ContentType, attachment.FileName);
		}

		return PhysicalFile(filePath, attachment.ContentType);
	}

	private IActionResult GenerateZipResult(SupportTicket ticket)
	{
		var attachmentsDir = GetAttachmentsDirectory();
		var memoryStream = new MemoryStream();
		using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
		{
			var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var attachment in ticket.Attachments)
			{
				var filePath = Path.Combine(attachmentsDir, attachment.StoredFileName);
				if (!System.IO.File.Exists(filePath))
				{
					continue;
				}

				var entryName = GetUniqueEntryName(attachment.FileName, usedNames);
				archive.CreateEntryFromFile(filePath, entryName);
			}
		}

		memoryStream.Position = 0;
		return File(memoryStream.ToArray(), "application/zip", $"Chamado #{ticket.TicketNumber}.zip");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteAttachment(Guid id)
	{
		var attachment = await context.TicketAttachments.FindAsync(id);
		if (attachment is null)
		{
			return NotFound();
		}

		var ticketId = attachment.SupportTicketId;
		var filePath = Path.Combine(GetAttachmentsDirectory(), attachment.StoredFileName);
		if (System.IO.File.Exists(filePath))
		{
			try { System.IO.File.Delete(filePath); } catch { /* ignore */ }
		}

		context.TicketAttachments.Remove(attachment);
		await context.SaveChangesAsync();

		return RedirectToAction(nameof(Details), new { id = ticketId });
	}

	private static string GetUniqueEntryName(string fileName, HashSet<string> usedNames)
	{
		var baseName = Path.GetFileNameWithoutExtension(fileName);
		var ext = Path.GetExtension(fileName);
		var name = fileName;
		var counter = 1;

		while (!usedNames.Add(name))
		{
			name = $"{baseName} ({counter++}){ext}";
		}

		return name;
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
