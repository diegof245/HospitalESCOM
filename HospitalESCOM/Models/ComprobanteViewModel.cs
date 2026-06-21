namespace HospitalApp.Models
{
    public class ComprobanteViewModel
    {
        public int FolioCita { get; set; }
        public string NombrePaciente { get; set; } = null!;
        public string Fecha { get; set; } = null!;
        public string Horario { get; set; } = null!;
        public int Consultorio { get; set; }
        public string Especialidad { get; set; } = null!;
        public string Doctor { get; set; } = null!;
        public string LineaPago { get; set; } = null!;
        public decimal Costo { get; set; }
    }
}