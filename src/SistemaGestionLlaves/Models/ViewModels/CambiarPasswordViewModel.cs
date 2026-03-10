using System.ComponentModel.DataAnnotations;

namespace SistemaGestionLlaves.Models.ViewModels;

public class CambiarPasswordViewModel
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña Actual")]
    public string PasswordActual { get; set; } = null!;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*#?&]{8,}$", ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres, incluyendo al menos una letra y un número.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña")]
    public string NuevaPassword { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Nueva Contraseña")]
    [Compare("NuevaPassword", ErrorMessage = "La nueva contraseña y la confirmación no coinciden.")]
    public string ConfirmarPassword { get; set; } = null!;
}
