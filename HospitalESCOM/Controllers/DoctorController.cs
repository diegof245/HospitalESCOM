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
        // 1. Ajustamos la consulta para traer Teléfono y Correo reales
        // 2. Concatenamos la hora de inicio y fin para armar el 'Turno'
        string query = @"
            SELECT e.IdEmpleado, 
                   per.Nombre + ' ' + per.ApellidoPaterno + ' ' + ISNULL(per.ApellidoMaterno, '') AS NombreCompleto,
                   ISNULL(per.Telefono, 'Sin Registro') AS Telefono, 
                   ISNULL(per.Correo, 'Sin Registro') AS Correo,
                   d.Cedula, 
                   esp.Nombre AS Especialidad, 
                   CAST(d.HoraInicio AS VARCHAR(5)) + ' a ' + CAST(d.HoraFin AS VARCHAR(5)) AS Turno
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
                    modelo.IdEmpleado = Convert.ToInt32(r["IdEmpleado"]);
                    modelo.NombreCompleto = r["NombreCompleto"].ToString()!.Trim();
                    modelo.Telefono = r["Telefono"].ToString()!;
                    modelo.Correo = r["Correo"].ToString()!;
                    modelo.Cedula = r["Cedula"].ToString()!;
                    modelo.Especialidad = r["Especialidad"].ToString()!;
                    modelo.Turno = r["Turno"].ToString()!;
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

[HttpGet]
public IActionResult HistorialClinico(string filtro)
{
    int idUsuario = HttpContext.Session.GetInt32("UserId") ?? 1; // Ajusta si usas otro nombre de sesión
    var historial = new List<dynamic>();

    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        // 1. Obtenemos el IdDoctor basado en el usuario logueado
        // 2. Traemos las citas, sacando el último estatus de la Bitácora
        string query = @"
            DECLARE @DoctorId INT;
            SELECT @DoctorId = d.IdDoctor 
            FROM Doctor d 
            JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado 
            JOIN Usuario u ON e.IdPersona = u.IdPersona 
            WHERE u.IdUsuario = @IdUsuario;

            SELECT c.IdCita AS Folio, c.Fecha, 
                   ISNULL((SELECT TOP 1 Estado FROM Bitacora b WHERE b.IdCita = c.IdCita ORDER BY Fecha DESC), 'Agendada') AS Estatus,
                   docPer.Nombre + ' ' + docPer.ApellidoPaterno AS NombreDoctor,
                   esp.Nombre AS Especialidad,
                   c.Costo,
                   pacPer.Nombre + ' ' + pacPer.ApellidoPaterno AS NombrePaciente,
                   ISNULL(r.Diagnostico, 'Sin diagnóstico registrado') AS Diagnostico
            FROM Cita c
            JOIN Paciente p ON c.IdPaciente = p.IdPaciente
            JOIN Persona pacPer ON p.IdPersona = pacPer.IdPersona
            JOIN Doctor d ON c.IdDoctor = d.IdDoctor
            JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado
            JOIN Persona docPer ON e.IdPersona = docPer.IdPersona
            JOIN Especialidad esp ON d.IdEspecialidad = esp.IdEspecialidad
            LEFT JOIN Receta r ON c.IdCita = r.IdCita
            WHERE d.IdDoctor = @DoctorId
              AND (@Filtro IS NULL OR @Filtro = '' 
                   OR pacPer.Nombre LIKE '%' + @Filtro + '%' 
                   OR pacPer.ApellidoPaterno LIKE '%' + @Filtro + '%'
                   OR CAST(p.IdPaciente AS VARCHAR) = @Filtro)
            ORDER BY c.Fecha DESC";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@Filtro", string.IsNullOrEmpty(filtro) ? DBNull.Value : (object)filtro);
            
            conn.Open();
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    historial.Add(new {
                        Folio = r["Folio"],
                        Fecha = Convert.ToDateTime(r["Fecha"]).ToString("dd/MM/yyyy"),
                        Estatus = r["Estatus"].ToString(),
                        NombreDoctor = r["NombreDoctor"].ToString(),
                        Especialidad = r["Especialidad"].ToString(),
                        Costo = Convert.ToDecimal(r["Costo"]),
                        NombrePaciente = r["NombrePaciente"].ToString(),
                        Diagnostico = r["Diagnostico"].ToString()
                    });
                }
            }
        }
    }
    
    ViewBag.FiltroActual = filtro;
    return View(historial);
}

    }
}