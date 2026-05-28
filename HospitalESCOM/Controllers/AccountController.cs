using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using HospitalApp.Models;

namespace HospitalApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connectionString;

        // El constructor inyecta la configuración del appsettings.json
        public AccountController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ConexionHospital");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_AutenticarUsuario", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@NombreUsuario", model.NombreUsuario);
                        cmd.Parameters.AddWithValue("@Password", model.Password); // Idealmente usar hash aquí

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var usuario = new UsuarioSesion
                                {
                                    IdUsuario = Convert.ToInt32(reader["IdUsuario"]),
                                    IdPersona = Convert.ToInt32(reader["IdPersona"]),
                                    TipoUsuario = reader["TipoUsuario"].ToString()
                                };

                                // Guardar el rol en una sesión o Cookie para usarlo en la app
                                HttpContext.Session.SetString("UserRole", usuario.TipoUsuario);
                                HttpContext.Session.SetInt32("UserId", usuario.IdUsuario);

                                // Redirección según su rol (Regla de negocio de usuarios)
                                return usuario.TipoUsuario switch
                                {
                                    "Paciente" => RedirectToAction("Index", "Paciente"),
                                    "Doctor" => RedirectToAction("Index", "Doctor"),
                                    _ => RedirectToAction("Index", "Recepcionista") // Administrador o Recepcionista
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Atrapa el RAISERROR que configuramos en SQL Server si los datos están mal
                ModelState.AddModelError("", ex.Message);
            }

            return View(model);
        }
    }
}