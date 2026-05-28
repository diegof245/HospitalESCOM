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

        // GET: /Recepcionista/RegistrarDoctor
        [HttpGet]
        public IActionResult RegistrarDoctor()
        {
            // Aquí idealmente cargarías la lista de especialidades de la BD para el ComboBox
            // Por ahora, pasaremos una simulación simple o puedes dejar el campo numérico directo
            return View();
        }

        // POST: /Recepcionista/RegistrarDoctor
        [HttpPost]
        public IActionResult RegistrarDoctor(DoctorViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Iniciamos una transacción para asegurar consistencia en la BD
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Insertar en Persona y obtener el IdPersona generado
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

                    // 2. Insertar en Usuario (Para que el doctor pueda loguearse al sistema)
                    string queryUsuario = @"INSERT INTO Usuario (IdPersona, NombreUsuario, Password, TipoUsuario, Estado) 
                                           VALUES (@IdPersona, @NombreUsuario, @Password, 'Doctor', 1);";
                    using (SqlCommand cmd = new SqlCommand(queryUsuario, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@IdPersona", idPersona);
                        cmd.Parameters.AddWithValue("@NombreUsuario", model.NombreUsuario);
                        cmd.Parameters.AddWithValue("@Password", model.Password);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Insertar en Empleado (Puesto 1 representa Doctor en tu Bit del diccionario)
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

                    // 4. Insertar en la tabla Doctor final
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

                    // Si todo sale bien, guardamos cambios en SQL Server
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
    }
}