using Microsoft.EntityFrameworkCore;

namespace Truco.Net.Entidades
{
    public class JuegoContext : DbContext
    {
        public DbSet<Jugador> Jugadores { get; set; }
        public DbSet<Partida> Partidas { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=tcp:truconetdbserver.database.windows.net,1433;Initial Catalog=Truco.NET_db;Encrypt=True;TrustServerCertificate=False;Authentication=ActiveDirectoryInteractive;");
        }
    }
}
