using System;

namespace HospitalApp.Models
{
    public class BitacoraViewModel
    {
        public int IdBitacora { get; set; }
        public int? IdCita { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = null!;
        public string Observacion { get; set; } = null!;
    }
}