using System;
using System.Collections.Generic;

namespace HospitalApp.Models
{
    public class ExpedientePacienteViewModel
    {
        // Datos del Expediente
        public int NumeroExpediente { get; set; }
        public string NombreCompleto { get; set; } = null!;
        public string TipoSangre { get; set; } = null!;
        public decimal Peso { get; set; }
        public decimal Estatura { get; set; }
        public string Alergias { get; set; } = null!;

        // Lista de Recetas
        public List<RecetaViewItem> Recetas { get; set; } = new List<RecetaViewItem>();
    }

    public class RecetaViewItem
    {
        public int FolioReceta { get; set; }
        public string Fecha { get; set; } = null!;
        public string Doctor { get; set; } = null!;
        public string Diagnostico { get; set; } = null!;
        public string Observaciones { get; set; } = null!;
        public List<DetalleMedicamentoItem> Medicamentos { get; set; } = new List<DetalleMedicamentoItem>();
    }

    public class DetalleMedicamentoItem
    {
        public string NombreMedicamento { get; set; } = null!;
        public string Dosis { get; set; } = null!;
        public string Frecuencia { get; set; } = null!;
        public int DuracionDias { get; set; }
        public string Indicaciones { get; set; } = null!;
    }
}