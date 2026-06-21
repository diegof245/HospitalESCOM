namespace HospitalApp.Models
{
    public class CajaViewModel
    {
        public int IdCita { get; set; }
        public string Paciente { get; set; } = null!;
        public decimal Costo { get; set; }
    }
}