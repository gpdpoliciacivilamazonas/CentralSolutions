using System.ComponentModel.DataAnnotations;

namespace CentralSolutions.Models;

public class Department
{
    public int Id { get; set; }

    [Display(Name = "Setor")]
    [Required(ErrorMessage = "Informe o nome do setor.")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Criado em")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Atualizado em")]
    public DateTime? UpdatedAt { get; set; }
}
