using System.ComponentModel.DataAnnotations;
using CentralSolutions.Models;

namespace CentralSolutions.ViewModels;

public class AddTicketUpdateViewModel
{
	public Guid TicketId { get; set; }

	[Display(Name = "Nº")]
	public int TicketNumber { get; set; }

	[Display(Name = "Setor")]
	public string Department { get; set; } = string.Empty;

	[Display(Name = "Chamado")]
	public string TicketType { get; set; } = string.Empty;

	[Display(Name = "Responsável")]
	public string? ResponsibleTechnician { get; set; }

	[Display(Name = "Problema")]
	public string Problem { get; set; } = string.Empty;

	[Display(Name = "Status Atual")]
	public TicketResolutionStatus CurrentStatus { get; set; }

	[Display(Name = "Nome do Responsável pela Atualização")]
	[Required(ErrorMessage = "Informe seu nome.")]
	[StringLength(120, ErrorMessage = "O nome não pode exceder 120 caracteres.")]
	public string CreatedBy { get; set; } = string.Empty;

	[Display(Name = "Descrição da Atualização")]
	[Required(ErrorMessage = "Informe a descrição da nova atualização.")]
	[StringLength(2000, ErrorMessage = "A descrição não pode exceder 2000 caracteres.")]
	public string Description { get; set; } = string.Empty;

	[Display(Name = "Concluir chamado com esta atualização (Marcar como Concluído / Done)")]
	public bool MarkAsDone { get; set; }
}
