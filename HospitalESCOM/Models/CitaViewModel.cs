using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalApp.Models // Asegúrate de que coincida con el nombre de tu proyecto
{
    public class CitaViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un paciente.")]
        public int IdPaciente { get; set; }

        [Required]
        public int IdRecepcionista { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un médico.")]
        public int IdDoctor { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un consultorio.")]
        public int IdConsultorio { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
        public TimeSpan HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria.")]
        public TimeSpan HoraFin { get; set; }

        [Required(ErrorMessage = "El costo es obligatorio.")]
        public int Costo { get; set; }
    }
}