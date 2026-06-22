namespace HospitalApp.Models
{
    public class PerfilDoctorViewModel
    {
        public int IdEmpleado { get; set; }
        public string NombreCompleto { get; set; } = null!;
        public string Telefono { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Turno { get; set; } = null!;
        public string Cedula { get; set; } = null!;
        public string Especialidad { get; set; } = null!;
    }
}