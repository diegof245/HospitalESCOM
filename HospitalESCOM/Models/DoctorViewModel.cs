using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalApp.Models
{
    public class DoctorViewModel
    {
        // Datos Personales
        [Required] public string Nombre { get; set; } = null!;
        [Required] public string ApellidoPaterno { get; set; } = null!;
        public string? ApellidoMaterno { get; set; }
        [Required] public DateTime FechaNacimiento { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }

        // Datos de Cuenta de Usuario
        [Required] public string NombreUsuario { get; set; } = null!;
        [Required] public string Password { get; set; } = null!;

        // Datos de Empleado y Doctor
        [Required] public int IdEspecialidad { get; set; } // Combo box en la vista
        [Required] public int Salario { get; set; }
        [Required] public string Cedula { get; set; } = null!;
        [Required] public string Turno { get; set; } = null!; // Matutino/Vespertino
        [Required] public TimeSpan HoraInicio { get; set; }
        [Required] public TimeSpan HoraFin { get; set; }
    }
}