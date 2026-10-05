using System.ComponentModel.DataAnnotations;

namespace CentralSolutions.Models;

public class SupportTicket
{
	public Guid Id { get; set; }

	[Display(Name = "Nº")]
	public int TicketNumber { get; set; }

	[Display(Name = "Setor")]
	[Required(ErrorMessage = "Informe o setor.")]
	[StringLength(100)]
	public string Department { get; set; } = string.Empty;

	[Display(Name = "Chamado")]
	[Required(ErrorMessage = "Informe o chamado.")]
	[StringLength(100)]
	public string TicketType { get; set; } = string.Empty;

	[Display(Name = "Responsável")]
	[StringLength(120)]
	public string? ResponsibleTechnician { get; set; }

	[Display(Name = "Problema")]
	[Required(ErrorMessage = "Informe o problema.")]
	[StringLength(1000)]
	public string Problem { get; set; } = string.Empty;

	[Display(Name = "Atualizações")]
	public ICollection<TicketUpdate> Updates { get; set; } = new List<TicketUpdate>();

	[Display(Name = "Status")]
	[Required(ErrorMessage = "Informe a situação do chamado.")]
	public TicketResolutionStatus ResolutionStatus { get; set; } = TicketResolutionStatus.Open;

	[Display(Name = "Criado em")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	[Display(Name = "Atualizado em")]
	public DateTime? UpdatedAt { get; set; }
}

public enum TicketResolutionStatus
{
	[Display(Name = "Em aberto")]
	Open = 0,

	[Display(Name = "Em andamento")]
	OnGoing = 1,

	[Display(Name = "Concluído")]
	Done = 2,
}
