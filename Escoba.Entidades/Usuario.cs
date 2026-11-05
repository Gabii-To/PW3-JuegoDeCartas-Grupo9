using System.ComponentModel.DataAnnotations;

namespace Escoba.Entidades
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 20 caracteres.")]
        [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "El usuario solo puede contener letras, números y guiones bajos.")]
        [Display(Name = "Nombre de Usuario")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

    }
}
