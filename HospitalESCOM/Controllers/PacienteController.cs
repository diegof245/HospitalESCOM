using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data; // <-- Faltaba esta librería para los Stored Procedures
using System;
using HospitalApp.Models;

namespace HospitalApp.Controllers
{
    public class PacienteController : Controller
    {
        private readonly string _connectionString;

        public PacienteController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ConexionHospital")!;
        }

        // ==========================================
        // MÓDULO: DASHBOARD DEL PACIENTE
        // ==========================================

        // GET: /Paciente/Index
        [HttpGet]
        public IActionResult Index()
        {
            // Verificamos que sí haya una sesión activa
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) 
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.NombreUsuario = HttpContext.Session.GetString("UserRole") ?? "Paciente";
            return View();
        }

        // ==========================================
        // MÓDULO: AUTO-REGISTRO
        // ==========================================

        // GET: /Paciente/AutoRegistro
        [HttpGet]
        public IActionResult AutoRegistro()
        {
            return View();
        }

        // POST: /Paciente/AutoRegistro
        [HttpPost]
        public IActionResult AutoRegistro(RegistroPacienteViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Usamos transacción porque si falla el insert de Paciente, no debe guardarse la Persona
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Insertar en Persona
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

                    // 2. Insertar en Usuario
                    string queryUsuario = @"INSERT INTO Usuario (IdPersona, NombreUsuario, Password, TipoUsuario, Estado) 
                                           VALUES (@IdPersona, @NombreUsuario, @Password, 'Paciente', 1);";
                    using (SqlCommand cmd = new SqlCommand(queryUsuario, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@IdPersona", idPersona);
                        cmd.Parameters.AddWithValue("@NombreUsuario", model.NombreUsuario);
                        cmd.Parameters.AddWithValue("@Password", model.Password);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Insertar en Paciente
                    // Simulamos un número de expediente único basado en el IdPersona y el año actual
                    int numExpediente = int.Parse($"{DateTime.Now.Year}{idPersona.ToString().PadLeft(4, '0')}");
                    
                    string queryPaciente = @"INSERT INTO Paciente (IdPersona, NumeroExpediente, TipoSangre, Alergias, Peso, Estatura) 
                                            VALUES (@IdPersona, @NumeroExpediente, @TipoSangre, @Alergias, @Peso, @Estatura);";
                    using (SqlCommand cmd = new SqlCommand(queryPaciente, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@IdPersona", idPersona);
                        cmd.Parameters.AddWithValue("@NumeroExpediente", numExpediente);
                        cmd.Parameters.AddWithValue("@TipoSangre", model.TipoSangre);
                        cmd.Parameters.AddWithValue("@Alergias", (object?)model.Alergias ?? "Ninguna");
                        cmd.Parameters.AddWithValue("@Peso", model.Peso);
                        cmd.Parameters.AddWithValue("@Estatura", model.Estatura);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    TempData["SuccessMessage"] = "¡Cuenta creada con éxito! Ya puedes iniciar sesión.";
                    return RedirectToAction("Login", "Account");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError(string.Empty, "Error al registrar: " + ex.Message);
                }
            }

            return View(model);
        }

        // ==========================================
        // MÓDULO: AGENDAR CITA PACIENTE
        // ==========================================

        // GET: /Paciente/AgendarCita
        [HttpGet]
        public IActionResult AgendarCita()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            RecargarCombosCita();

            var model = new CitaViewModel
            {
                Fecha = DateTime.Now.AddDays(3),
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(10, 0, 0)
            };

            return View(model);
        }

        // POST: /Paciente/AgendarCita
        [HttpPost] // <-- ¡Esta etiqueta te faltaba y rompía el servidor!
        public IActionResult AgendarCita(CitaViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            model.IdPaciente = ObtenerIdPacientePorUsuario(userId.Value);
            model.IdRecepcionista = 1; 

            if (!ModelState.IsValid)
            {
                RecargarCombosCita();
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

                        TempData["SuccessMessage"] = "¡Cita reservada exitosamente!";
                        return RedirectToAction("Index");
                    }
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError(string.Empty, "Error de validación: " + ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Error inesperado: " + ex.Message);
                }
            }

            RecargarCombosCita();
            return View(model);
        }

        // ==========================================
        // MÉTODOS AUXILIARES
        // ==========================================

        private void RecargarCombosCita()
        {
            var especialidades = new List<dynamic>();
            var doctores = new List<dynamic>();
            var consultorios = new List<int>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT IdEspecialidad, Nombre, CostoConsulta FROM Especialidad", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) especialidades.Add(new { 
                        Id = Convert.ToInt32(r["IdEspecialidad"]), 
                        Nombre = r["Nombre"].ToString(), 
                        Costo = Convert.ToDecimal(r["CostoConsulta"]) 
                    });

                string queryDocs = @"SELECT d.IdDoctor, d.IdEspecialidad, per.Nombre + ' ' + per.ApellidoPaterno AS NombreDoc 
                                     FROM Doctor d 
                                     JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado 
                                     JOIN Persona per ON e.IdPersona = per.IdPersona";
                using (SqlCommand cmd = new SqlCommand(queryDocs, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) doctores.Add(new { 
                        Id = Convert.ToInt32(r["IdDoctor"]), 
                        IdEspecialidad = Convert.ToInt32(r["IdEspecialidad"]), 
                        Nombre = r["NombreDoc"].ToString() 
                    });

                using (SqlCommand cmd = new SqlCommand("SELECT IdConsultorio FROM Consultorio WHERE Estado = 1", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read()) consultorios.Add(Convert.ToInt32(r["IdConsultorio"]));
            }

            ViewBag.Especialidades = especialidades;
            ViewBag.Doctores = doctores;
            ViewBag.Consultorios = consultorios;
        }

        private int ObtenerIdPacientePorUsuario(int idUsuario)
        {
            int idPaciente = 0;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT p.IdPaciente FROM Paciente p 
                                 JOIN Usuario u ON p.IdPersona = u.IdPersona 
                                 WHERE u.IdUsuario = @IdUsuario";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null) idPaciente = Convert.ToInt32(result);
                }
            }
            return idPaciente;
        }

        [HttpGet]
public IActionResult Historial()
{
    int? userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null) return RedirectToAction("Login", "Account");

    int idPaciente = ObtenerIdPacientePorUsuario(userId.Value);
    var historial = new List<HistorialCitaViewModel>();

    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        string query = @"
            SELECT 
                c.IdCita, c.Fecha, c.HoraInicio, 
                esp.Nombre AS Especialidad, 
                per.Nombre + ' ' + per.ApellidoPaterno AS Doctor, 
                cons.Numero AS Consultorio, 
                c.Costo, c.Estado
            FROM Cita c
            JOIN Doctor d ON c.IdDoctor = d.IdDoctor
            JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado
            JOIN Persona per ON e.IdPersona = per.IdPersona
            JOIN Especialidad esp ON d.IdEspecialidad = esp.IdEspecialidad
            JOIN Consultorio cons ON c.IdConsultorio = cons.IdConsultorio
            WHERE c.IdPaciente = @IdPaciente
            ORDER BY c.Fecha DESC, c.HoraInicio DESC";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@IdPaciente", idPaciente);
            conn.Open();
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int estado = Convert.ToInt32(r["Estado"]);
                    string estadoDesc = estado switch {
                        1 => "Agendada (Pendiente Pago)",
                        2 => "Pagada (Confirmada)",
                        4 => "Cancelada (Paciente)",
                        5 => "Cancelada (Doctor)",
                        _ => "Atendida"
                    };

                    historial.Add(new HistorialCitaViewModel
                    {
                        IdCita = Convert.ToInt32(r["IdCita"]),
                        Fecha = Convert.ToDateTime(r["Fecha"]).ToString("yyyy-MM-dd"),
                        HoraInicio = r["HoraInicio"].ToString()!,
                        Especialidad = r["Especialidad"].ToString()!,
                        NombreDoctor = r["Doctor"].ToString()!,
                        Consultorio = Convert.ToInt32(r["Consultorio"]),
                        Costo = Convert.ToDecimal(r["Costo"]),
                        EstadoCita = estado,
                        EstadoDescripcion = estadoDesc
                    });
                }
            }
        }
    }

    return View(historial);
}

[HttpPost]
public IActionResult CancelarCitaHistorial(int idCita)
{
    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        try
        {
            using (SqlCommand cmd = new SqlCommand("sp_CancelarCita", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdCita", idCita);
                cmd.Parameters.AddWithValue("@QuienCancela", "Paciente");

                conn.Open();
                cmd.ExecuteNonQuery();

                TempData["SuccessMessage"] = "Cita cancelada correctamente. Se aplicó la política de reembolso correspondiente.";
            }
        }
        catch (SqlException ex)
        {
            TempData["ErrorMessage"] = "No se pudo cancelar: " + ex.Message;
        }
    }

    return RedirectToAction("Historial");
}

[HttpGet]
public IActionResult Expediente()
{
    int? userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null) return RedirectToAction("Login", "Account");

    int idPaciente = ObtenerIdPacientePorUsuario(userId.Value);
    var modelo = new ExpedientePacienteViewModel();

    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        conn.Open();

        // 1. Obtener Datos Personales del Paciente
        string queryPaciente = @"
            SELECT p.NumeroExpediente, per.Nombre + ' ' + per.ApellidoPaterno AS NombreCompleto,
                   p.TipoSangre, p.Peso, p.Estatura, p.Alergias
            FROM Paciente p
            JOIN Persona per ON p.IdPersona = per.IdPersona
            WHERE p.IdPaciente = @IdPaciente";

        using (SqlCommand cmd = new SqlCommand(queryPaciente, conn))
        {
            cmd.Parameters.AddWithValue("@IdPaciente", idPaciente);
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                if (r.Read())
                {
                    modelo.NumeroExpediente = Convert.ToInt32(r["NumeroExpediente"]);
                    modelo.NombreCompleto = r["NombreCompleto"].ToString()!;
                    modelo.TipoSangre = r["TipoSangre"].ToString()!;
                    modelo.Peso = Convert.ToDecimal(r["Peso"]);
                    modelo.Estatura = Convert.ToDecimal(r["Estatura"]);
                    modelo.Alergias = r["Alergias"].ToString()!;
                }
            }
        }

        // 2. Obtener las Recetas
        string queryRecetas = @"
            SELECT r.IdReceta, r.FechaReceta, r.Diagnostico, r.Observaciones,
                   perDoc.Nombre + ' ' + perDoc.ApellidoPaterno AS Doctor
            FROM Receta r
            JOIN Cita c ON r.IdCita = c.IdCita
            JOIN Doctor d ON c.IdDoctor = d.IdDoctor
            JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado
            JOIN Persona perDoc ON e.IdPersona = perDoc.IdPersona
            WHERE c.IdPaciente = @IdPaciente
            ORDER BY r.FechaReceta DESC";

        using (SqlCommand cmd = new SqlCommand(queryRecetas, conn))
        {
            cmd.Parameters.AddWithValue("@IdPaciente", idPaciente);
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    modelo.Recetas.Add(new RecetaViewItem
                    {
                        FolioReceta = Convert.ToInt32(r["IdReceta"]),
                        Fecha = Convert.ToDateTime(r["FechaReceta"]).ToString("dd/MM/yyyy"),
                        Doctor = r["Doctor"].ToString()!,
                        Diagnostico = r["Diagnostico"].ToString()!,
                        Observaciones = r["Observaciones"].ToString()!
                    });
                }
            }
        }

        // 3. Obtener el detalle de los medicamentos de todas sus recetas
        string queryMedicamentos = @"
            SELECT dr.IdReceta, m.Nombre AS Medicamento, dr.Dosis, dr.Frecuencia, dr.Duracion, dr.Indicaciones
            FROM DetalleReceta dr
            JOIN Medicamento m ON dr.IdMedicamento = m.IdMedicamento
            JOIN Receta r ON dr.IdReceta = r.IdReceta
            JOIN Cita c ON r.IdCita = c.IdCita
            WHERE c.IdPaciente = @IdPaciente";

        using (SqlCommand cmd = new SqlCommand(queryMedicamentos, conn))
        {
            cmd.Parameters.AddWithValue("@IdPaciente", idPaciente);
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int idRec = Convert.ToInt32(r["IdReceta"]);
                    var recetaCorrespondiente = modelo.Recetas.Find(x => x.FolioReceta == idRec);
                    
                    if (recetaCorrespondiente != null)
                    {
                        recetaCorrespondiente.Medicamentos.Add(new DetalleMedicamentoItem
                        {
                            NombreMedicamento = r["Medicamento"].ToString()!,
                            Dosis = r["Dosis"].ToString()!,
                            Frecuencia = r["Frecuencia"].ToString()!,
                            DuracionDias = Convert.ToInt32(r["Duracion"]),
                            Indicaciones = r["Indicaciones"].ToString()!
                        });
                    }
                }
            }
        }
    }

    return View(modelo);
}


[HttpGet]
public IActionResult Comprobante(int idCita)
{
    int? userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null) return RedirectToAction("Login", "Account");

    var modelo = new ComprobanteViewModel();

    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        string query = @"
            SELECT c.IdCita, perPac.Nombre + ' ' + perPac.ApellidoPaterno AS Paciente,
                   c.Fecha, c.HoraInicio, c.HoraFin, c.IdConsultorio,
                   esp.Nombre AS Especialidad,
                   perDoc.Nombre + ' ' + perDoc.ApellidoPaterno AS Doctor,
                   c.Costo
            FROM Cita c
            JOIN Paciente p ON c.IdPaciente = p.IdPaciente
            JOIN Persona perPac ON p.IdPersona = perPac.IdPersona
            JOIN Doctor d ON c.IdDoctor = d.IdDoctor
            JOIN Empleado e ON d.IdEmpleado = e.IdEmpleado
            JOIN Persona perDoc ON e.IdPersona = perDoc.IdPersona
            JOIN Especialidad esp ON d.IdEspecialidad = esp.IdEspecialidad
            WHERE c.IdCita = @IdCita";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@IdCita", idCita);
            conn.Open();
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                if (r.Read())
                {
                    modelo.FolioCita = Convert.ToInt32(r["IdCita"]);
                    modelo.NombrePaciente = r["Paciente"].ToString()!;
                    modelo.Fecha = Convert.ToDateTime(r["Fecha"]).ToString("dd/MM/yyyy");
                    
                    // Formatear el horario para quitar los milisegundos de SQL
                    TimeSpan inicio = (TimeSpan)r["HoraInicio"];
                    TimeSpan fin = (TimeSpan)r["HoraFin"];
                    modelo.Horario = $"{inicio:hh\\:mm} - {fin:hh\\:mm}";
                    
                    modelo.Consultorio = Convert.ToInt32(r["IdConsultorio"]);
                    modelo.Especialidad = r["Especialidad"].ToString()!;
                    modelo.Doctor = r["Doctor"].ToString()!;
                    modelo.Costo = Convert.ToDecimal(r["Costo"]);
                    
                    // Generar una línea de pago aleatoria basada en el folio y el año
                    modelo.LineaPago = $"01-HOSP-{DateTime.Now.Year}{modelo.FolioCita:D4}-{new Random().Next(1000,9999)}";
                }
            }
        }
    }

    return View(modelo);
}

    }
}