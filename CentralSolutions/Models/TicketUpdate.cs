using System.ComponentModel.DataAnnotations;

namespace CentralSolutions.Models;

public class TicketUpdate
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid SupportTicketId { get; set; }

	public SupportTicket SupportTicket { get; set; } = null!;

	[Display(Name = "Atualização")]
	[Required(ErrorMessage = "Informe a descrição da atualização.")]
	[StringLength(2000)]
	public string Description { get; set; } = string.Empty;

	[Display(Name = "Registrado por")]
	[StringLength(120)]
	public string? CreatedBy { get; set; }

	[Display(Name = "Data da atualização")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
