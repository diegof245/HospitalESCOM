using System.ComponentModel.DataAnnotations;

namespace HospitalApp.Models
{
    public class AgendaDoctorViewModel
    {
        public int IdCita { get; set; }
        public int IdPaciente { get; set; }
        public string Paciente { get; set; } = null!;
        public string Fecha { get; set; } = null!;
        public string Hora { get; set; } = null!;
        public string Estado { get; set; } = null!;
    }

    public class RecetaFormViewModel
    {
        public int IdCita { get; set; }
        public string Paciente { get; set; } = null!;
        
        [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
        public string Diagnostico { get; set; } = null!;
        
        public string Observaciones { get; set; } = "";
        
        // Aquí recibiremos la lista de medicamentos (esto lo manejaremos por IDs en un select)
        public int IdMedicamento { get; set; }
        public string Dosis { get; set; } = "1 tableta";
        public string Frecuencia { get; set; } = "Cada 8 horas";
        public int Duracion { get; set; } = 7;
    }
}