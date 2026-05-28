namespace HospitalApp.Models
{
    // Datos que el usuario escribe en el formulario
    public class LoginViewModel
    {
        public string NombreUsuario { get; set; }
        public string Password { get; set; }
    }

    // Datos que regresa el SP sp_AutenticarUsuario al iniciar sesión con éxito
    public class UsuarioSesion
    {
        public int IdUsuario { get; set; }
        public int IdPersona { get; set; }
        public string TipoUsuario { get; set; }
    }
}