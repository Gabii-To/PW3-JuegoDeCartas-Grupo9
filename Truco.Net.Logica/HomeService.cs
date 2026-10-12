namespace Truco.Net.Logica
{
    public interface IHomeService
    {
        bool ValidarUsuario(string usuario, string password);
        (bool Exito, string Mensaje) RegistrarUsuario(string usuario, string password);
    }
    public class HomeService : IHomeService
    {
        // Array para simular DB
        private readonly List<(string Usuario, string Password)> _usuarios = new()
        {
            ("admin", "1234"),
            ("jugador1", "truco2026"),
            ("juan", "secret")
        };

        public bool ValidarUsuario(string usuario, string password)
        {
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
                return false;

            
            return _usuarios.Any(u => u.Usuario.Equals(usuario, StringComparison.OrdinalIgnoreCase)
                                   && u.Password == password);
        }

        public (bool Exito, string Mensaje) RegistrarUsuario(string usuario, string password)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                return (false, "Todos los campos son obligatorios.");

            // Verifica si el nombre de usuario ya existe en la lista
            bool existe = _usuarios.Any(u => u.Usuario.Equals(usuario, StringComparison.OrdinalIgnoreCase));
            if (existe)
            {
                return (false, "El nombre de usuario ya se encuentra registrado.");
            }

            // Agrega el nuevo usuario a la lista en memoria
            _usuarios.Add((usuario, password));
            return (true, "Usuario registrado con éxito.");
        }
    }
}
