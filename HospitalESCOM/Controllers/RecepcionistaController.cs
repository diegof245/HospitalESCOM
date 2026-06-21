using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using HospitalApp.Models;

namespace HospitalApp.Controllers
{
    public class RecepcionistaController : Controller
    {
        private readonly string _connectionString;

        public RecepcionistaController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ConexionHospital")!;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.NombreUsuario = HttpContext.Session.GetString("UserRole") ?? "Recepcionista";
            return View();
        }

        [HttpGet]
        public IActionResult AgendarCita()
        {
            var pacientes = new List<dynamic>();
            var doctores = new List<dynamic>();
            var consultorios = new List<int>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 1. Obtener lista de Pacientes (Blindado contra nulos)
                string queryPacientes = @"SELECT p.IdPaciente, per.Nombre + ' ' + per.ApellidoPaterno AS NombreCompleto 
                                          FROM Paciente p JOIN Persona per ON p.IdPersona = per.IdPersona";
                using (SqlCommand cmd = new SqlCommand(queryPacientes, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) {
                        pacientes.Add(new { 
                            Id = Convert.ToInt32(r["IdPaciente"]), 
                            Nombre = r["NombreCompleto"] != DBNull.Value ? r["NombreCompleto"].ToString() : "Sin Nombre" 
                        });
                    }
                }

                string queryDoctores = @"SELECT d.IdDoctor, per.Nombre + ' ' + per.ApellidoPaterno + ' (' + esp.Nombre + ')' AS DocInfo 
                                          FROM Doctor d 
                                          JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado
                                          JOIN Persona per ON e.IdPersona = per.IdPersona
                                          JOIN Especialidad esp ON d.IdEspecialidad = esp.IdEspecialidad";
                using (SqlCommand cmd = new SqlCommand(queryDoctores, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) {
                        doctores.Add(new { Id = Convert.ToInt32(r["IdDoctor"]), Info = r["DocInfo"].ToString() });
                    }
                }

                // 3. Obtener lista de Consultorios
                string queryConsultorios = "SELECT IdConsultorio, Numero FROM Consultorio WHERE Estado = 1";
                using (SqlCommand cmd = new SqlCommand(queryConsultorios, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) {
                        consultorios.Add(Convert.ToInt32(r["IdConsultorio"]));
                    }
                }
            }

            ViewBag.Pacientes = pacientes;
            ViewBag.Doctores = doctores;
            ViewBag.Consultorios = consultorios;

            var model = new CitaViewModel
            {
                IdRecepcionista = 1,
                Fecha = DateTime.Now.AddDays(3),
                HoraInicio = new TimeSpan(10, 0, 0),
                HoraFin = new TimeSpan(11, 0, 0),
                Costo = 400
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult AgendarCita(CitaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                RecargarCombos();
                return View(model);
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("sp_AgendarCita", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdPaciente", model.IdPaciente);
                        cmd.Parameters.AddWithValue("@IdRecepcionista", model.IdRecepcionista);
                        cmd.Parameters.AddWithValue("@IdDoctor", model.IdDoctor);
                        cmd.Parameters.AddWithValue("@IdConsultorio", model.IdConsultorio);
                        cmd.Parameters.AddWithValue("@Fecha", model.Fecha);
                        cmd.Parameters.AddWithValue("@HoraInicio", model.HoraInicio);
                        cmd.Parameters.AddWithValue("@HoraFin", model.HoraFin);
                        cmd.Parameters.AddWithValue("@Costo", model.Costo);

                        conn.Open();
                        cmd.ExecuteNonQuery(); 
                        
                        TempData["SuccessMessage"] = "¡Cita Pre-Agendada con éxito! El paciente tiene un límite de 8 horas para realizar su pago.";
                        return RedirectToAction("AgendarCita");
                    }
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError(string.Empty, "Error de Validación: " + ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado: " + ex.Message);
                }
            }

            RecargarCombos();
            return View(model);
        }

        [HttpGet]
        public IActionResult RegistrarDoctor()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RegistrarDoctor(DoctorViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    string queryPersona = @"INSERT INTO Persona (Nombre, ApellidoPaterno, ApellidoMaterno, FechaNacimiento, Telefono, Correo) 
                                           VALUES (@Nombre, @ApellidoPaterno, @ApellidoMaterno, @FechaNacimiento, @Telefono, @Correo);
                                           SELECT SCOPE_IDENTITY();";
                    
                    int idPersona;
                    using (SqlCommand cmd = new SqlCommand(queryPersona, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                        cmd.Parameters.AddWithValue("@ApellidoPaterno", model.ApellidoPaterno);
                        cmd.Parameters.AddWithValue("@ApellidoMaterno", (object?)model.ApellidoMaterno ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FechaNacimiento", model.FechaNacimiento);
                        cmd.Parameters.AddWithValue("@Telefono", (object?)model.Telefono ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Correo", (object?)model.Correo ?? DBNull.Value);
                        
                        idPersona = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    string queryUsuario = @"INSERT INTO Usuario (IdPersona, NombreUsuario, Password, TipoUsuario, Estado) 
                                           VALUES (@IdPersona, @NombreUsuario, @Password, 'Doctor', 1);";
                    using (SqlCommand cmd = new SqlCommand(queryUsuario, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@IdPersona", idPersona);
                        cmd.Parameters.AddWithValue("@NombreUsuario", model.NombreUsuario);
                        cmd.Parameters.AddWithValue("@Password", model.Password);
                        cmd.ExecuteNonQuery();
                    }

                    string queryEmpleado = @"INSERT INTO Empleado (IdPersona, Puesto, Salario, FechaContratacion, Estatus) 
                                            VALUES (@IdPersona, 1, @Salario, @FechaContratacion, 1);
                                            SELECT SCOPE_IDENTITY();";
                    
                    int idEmpleado;
                    using (SqlCommand cmd = new SqlCommand(queryEmpleado, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@IdPersona", idPersona);
                        cmd.Parameters.AddWithValue("@Salario", model.Salario);
                        cmd.Parameters.AddWithValue("@FechaContratacion", DateTime.Now.ToString("yyyy-MM-dd"));
                        
                        idEmpleado = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    string queryDoctor = @"INSERT INTO Doctor (IdEmpleado, IdEspecialidad, Cedula, Turno, HoraInicio, HoraFin) 
                                          VALUES (@IdEmpleado, @IdEspecialidad, @Cedula, @Turno, @HoraInicio, @HoraFin);";
                    using (SqlCommand cmd = new SqlCommand(queryDoctor, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@IdEmpleado", idEmpleado);
                        cmd.Parameters.AddWithValue("@IdEspecialidad", model.IdEspecialidad);
                        cmd.Parameters.AddWithValue("@Cedula", model.Cedula);
                        cmd.Parameters.AddWithValue("@Turno", model.Turno);
                        cmd.Parameters.AddWithValue("@HoraInicio", model.HoraInicio);
                        cmd.Parameters.AddWithValue("@HoraFin", model.HoraFin);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    ViewBag.Message = "¡Doctor y credenciales de acceso registrados con éxito!";
                    return View();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Error al registrar en la base de datos: " + ex.Message);
                }
            }

            return View(model);
        }

        private void RecargarCombos()
        {
            var pacientes = new List<dynamic>();
            var doctores = new List<dynamic>();
            var consultorios = new List<int>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                
                string queryPacientes = "SELECT p.IdPaciente, per.Nombre + ' ' + per.ApellidoPaterno AS NombreCompleto FROM Paciente p JOIN Persona per ON p.IdPersona = per.IdPersona";
                using (SqlCommand cmd = new SqlCommand(queryPacientes, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) pacientes.Add(new { 
                        Id = Convert.ToInt32(r["IdPaciente"]), 
                        Nombre = r["NombreCompleto"] != DBNull.Value ? r["NombreCompleto"].ToString() : "Sin Nombre" 
                    });

                string queryDoctores = "SELECT d.IdDoctor, per.Nombre + ' ' + per.ApellidoPaterno + ' (' + esp.Nombre + ')' AS DocInfo FROM Doctor d JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado JOIN Persona per ON e.IdPersona = per.IdPersona JOIN Especialidad esp ON d.IdEspecialidad = esp.IdEspecialidad";
                using (SqlCommand cmd = new SqlCommand(queryDoctores, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) doctores.Add(new { Id = Convert.ToInt32(r["IdDoctor"]), Info = r["DocInfo"].ToString() });

                string queryConsultorios = "SELECT IdConsultorio, Numero FROM Consultorio WHERE Estado = 1";
                using (SqlCommand cmd = new SqlCommand(queryConsultorios, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) consultorios.Add(Convert.ToInt32(r["IdConsultorio"]));
            }

            ViewBag.Pacientes = pacientes;
            ViewBag.Doctores = doctores;
            ViewBag.Consultorios = consultorios;
        }

        [HttpGet]
public IActionResult Caja()
{
    // Listamos citas agendadas (Estado 1) pendientes de pago
    var citasPendientes = new List<dynamic>();
    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        string query = @"SELECT c.IdCita, per.Nombre + ' ' + per.ApellidoPaterno AS Paciente, c.Costo 
                         FROM Cita c 
                         JOIN Paciente pac ON c.IdPaciente = pac.IdPaciente 
                         JOIN Persona per ON pac.IdPersona = per.IdPersona 
                         WHERE c.Estado = 1";
        conn.Open();
        using (SqlCommand cmd = new SqlCommand(query, conn))
        using (SqlDataReader r = cmd.ExecuteReader())
            while (r.Read()) citasPendientes.Add(new { Id = r["IdCita"], Paciente = r["Paciente"], Costo = r["Costo"] });
    }
    return View(citasPendientes);
}

[HttpPost]
public IActionResult ProcesarPago(int idCita)
{
    try
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            // Asegúrate de que IdUsuario sea un int válido, si tienes sesión, usa el de la sesión
            int idUsuario = HttpContext.Session.GetInt32("UserId") ?? 1;

            string query = @"UPDATE Cita SET Estado = 2 WHERE IdCita = @IdCita;
                             INSERT INTO Bitacora (IdCita, IdUsuario, Fecha, Estado, Observacion) 
                             VALUES (@IdCita, @IdUsuario, GETDATE(), 'Confirmada', 'Pago recibido para cita folio ' + CAST(@IdCita AS VARCHAR));";
            
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@IdCita", idCita);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmd.ExecuteNonQuery();
            }
        }
        return RedirectToAction("Caja"); // Esto debe recargar la página
    }
    catch (Exception ex)
    {
        // Si hay error, en lugar de pantalla en blanco, veremos el error exacto
        return Content("Error crítico en el cobro: " + ex.Message);
    }
}
    }
}