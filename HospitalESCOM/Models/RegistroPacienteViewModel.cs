using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalApp.Models
{
    public class RegistroPacienteViewModel
    {
        // Datos de Persona
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El apellido paterno es obligatorio")]
        public string ApellidoPaterno { get; set; } = null!;

        public string? ApellidoMaterno { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        public string? Telefono { get; set; }
        
        [EmailAddress]
        public string? Correo { get; set; }

        // Datos de Usuario
        [Required(ErrorMessage = "Cree un nombre de usuario")]
        public string NombreUsuario { get; set; } = null!;

        [Required(ErrorMessage = "Cree una contraseña")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        // Datos Médicos (Paciente)
        [Required]
        public string TipoSangre { get; set; } = null!;

        public string? Alergias { get; set; }

        [Required]
        public decimal Peso { get; set; }

        [Required]
        public decimal Estatura { get; set; }
    }
}