using System.ComponentModel.DataAnnotations;

namespace CentralSolutions.Models;

public class TicketAttachment
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[Required]
	public Guid SupportTicketId { get; set; }

	public SupportTicket SupportTicket { get; set; } = null!;

	[Required]
	[StringLength(255)]
	public string FileName { get; set; } = string.Empty;

	[Required]
	[StringLength(255)]
	public string StoredFileName { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string ContentType { get; set; } = string.Empty;

	public long FileSize { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
