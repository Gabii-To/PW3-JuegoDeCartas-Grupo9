using Escoba.Entidades;
using Escoba.Repositorio;
using Microsoft.EntityFrameworkCore;

namespace Escoba.Servicios
{
    public interface IHomeService
    {
        Task<Usuario?> ValidarUsuarioAsync(string usuario, string password);
        Task<(bool Exito, string Mensaje)> RegistrarUsuarioAsync(string usuario, string password);
    }


    public class HomeService : IHomeService
    {
        private readonly EscobaDbContext _context;

        public HomeService(EscobaDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ValidarUsuarioAsync(string usuario, string password)
        {
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
                return null;

            // Busca el usuario en la base de datos real
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Nombre.ToLower() == usuario.ToLower() && u.Password == password);
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarUsuarioAsync(string usuario, string password)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                return (false, "Todos los campos son obligatorios.");

            bool existe = await _context.Usuarios
                .AnyAsync(u => u.Nombre.ToLower() == usuario.ToLower());

            if (existe)
            {
                return (false, "El nombre de usuario ya se encuentra registrado.");
            }

            var nuevoUsuario = new Usuario
            {
                Nombre = usuario,
                Password = password
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return (true, "Usuario registrado con éxito.");
        }
    }
}