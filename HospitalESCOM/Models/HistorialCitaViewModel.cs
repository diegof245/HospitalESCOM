using System;

namespace HospitalApp.Models
{
    public class HistorialCitaViewModel
    {
        public int IdCita { get; set; }
        public string Fecha { get; set; } = null!;
        public string HoraInicio { get; set; } = null!;
        public string Especialidad { get; set; } = null!;
        public string NombreDoctor { get; set; } = null!;
        public int Consultorio { get; set; }
        public decimal Costo { get; set; }
        public int EstadoCita { get; set; }
        public string EstadoDescripcion { get; set; } = null!;
    }
}