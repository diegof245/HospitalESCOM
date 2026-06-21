using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using HospitalApp.Models;

namespace HospitalApp.Controllers
{
    public class DoctorController : Controller
    {
        private readonly string _connectionString;

        public DoctorController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ConexionHospital")!;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Validar que haya sesión y que estrictamente sea un Doctor
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? rol = HttpContext.Session.GetString("UserRole");

            if (userId == null || rol != "Doctor") 
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.NombreDoctor = HttpContext.Session.GetString("UserName") ?? "Doctor";
            
            return View();
        }

        [HttpGet]
        public IActionResult MiPerfil()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var modelo = new PerfilDoctorViewModel();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Ajustamos la consulta para usar los nombres reales de las columnas 
                // y rellenamos CURP y Universidad con valores por defecto
                string query = @"
                    SELECT CAST(e.IdEmpleado AS VARCHAR) AS NumeroEmpleado, 
                           per.Nombre + ' ' + per.ApellidoPaterno + ' ' + ISNULL(per.ApellidoMaterno, '') AS NombreCompleto,
                           'No registrada' AS Curp, 
                           d.Cedula AS CedulaProfesional, 
                           esp.Nombre AS Especialidad, 
                           'IPN - Escuela Superior de Medicina' AS Universidad,
                           CAST(d.HoraInicio AS VARCHAR(5)) + ' a ' + CAST(d.HoraFin AS VARCHAR(5)) AS HorarioLaboral
                    FROM Doctor d
                    JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado
                    JOIN Persona per ON e.IdPersona = per.IdPersona
                    JOIN Especialidad esp ON d.IdEspecialidad = esp.IdEspecialidad
                    JOIN Usuario u ON u.IdPersona = per.IdPersona
                    WHERE u.IdUsuario = @IdUsuario";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", userId.Value);
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            modelo.NumeroEmpleado = r["NumeroEmpleado"].ToString()!;
                            modelo.NombreCompleto = r["NombreCompleto"].ToString()!.Trim();
                            modelo.Curp = r["Curp"].ToString()!;
                            modelo.CedulaProfesional = r["CedulaProfesional"].ToString()!;
                            modelo.Especialidad = r["Especialidad"].ToString()!;
                            modelo.Universidad = r["Universidad"].ToString()!;
                            modelo.HorarioLaboral = r["HorarioLaboral"].ToString()!;
                        }
                    }
                }
            }

            return View(modelo);
        }

        [HttpGet]
public IActionResult Agenda()
{
    int? userId = HttpContext.Session.GetInt32("UserId");
    var listaCitas = new List<AgendaDoctorViewModel>();

    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        string query = @"SELECT c.IdCita, c.IdPaciente, p.Nombre + ' ' + p.ApellidoPaterno AS Paciente, 
                         c.Fecha, c.HoraInicio, c.Estado 
                         FROM Cita c 
                         JOIN Paciente pac ON c.IdPaciente = pac.IdPaciente
                         JOIN Persona p ON pac.IdPersona = p.IdPersona
                         WHERE c.IdDoctor = (SELECT d.IdDoctor FROM Doctor d JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado JOIN Usuario u ON u.IdPersona = e.IdPersona WHERE u.IdUsuario = @IdUsuario)
                         AND c.Fecha = CAST(GETDATE() AS DATE)";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@IdUsuario", userId);
            conn.Open();
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    listaCitas.Add(new AgendaDoctorViewModel {
                        IdCita = (int)r["IdCita"],
                        Paciente = r["Paciente"].ToString()!,
                        Hora = r["HoraInicio"].ToString()!,
                        Estado = r["Estado"].ToString() == "2" ? "Pagada" : "Pendiente"
                    });
                }
            }
        }
    }
    return View(listaCitas);
}

[HttpGet]
public IActionResult EmitirReceta(int idCita)
{
    // Carga los medicamentos en un ViewBag para el select
    var meds = new List<dynamic>();
    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        conn.Open();
        using (SqlCommand cmd = new SqlCommand("SELECT IdMedicamento, Nombre FROM Medicamento", conn))
        using (SqlDataReader r = cmd.ExecuteReader())
            while (r.Read()) meds.Add(new { Id = r["IdMedicamento"], Nombre = r["Nombre"] });
    }
    ViewBag.Medicamentos = meds;
    return View(new RecetaFormViewModel { IdCita = idCita });
}

[HttpPost]
public IActionResult GuardarReceta(RecetaFormViewModel model)
{
    if (!ModelState.IsValid) return View("EmitirReceta", model);

    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        // Insertamos la receta y actualizamos el estatus de la cita a "Atendida"
        string query = @"
            INSERT INTO Receta (IdCita, Diagnostico, Observaciones) VALUES (@IdCita, @Diag, @Obs);
            UPDATE Cita SET Estado = 'Atendida' WHERE IdCita = @IdCita;";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@IdCita", model.IdCita);
            cmd.Parameters.AddWithValue("@Diag", model.Diagnostico);
            cmd.Parameters.AddWithValue("@Obs", model.Observaciones);
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
    return RedirectToAction("Agenda");
}


    }
}