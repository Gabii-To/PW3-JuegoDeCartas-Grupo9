using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection.Emit;
using Escoba.Entidades;

namespace Escoba.Repositorio
{
    public class EscobaDbContext : DbContext
    {
        public EscobaDbContext(DbContextOptions<EscobaDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Partida> Partidas { get; set; } = null!;
        public DbSet<Jugador> Jugadores { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relación: Una Partida tiene un Jugador como Creador
            modelBuilder.Entity<Partida>()
                .HasOne(p => p.Creador)
                .WithMany()
                .HasForeignKey(p => p.UsuarioIdCreador)
                .OnDelete(DeleteBehavior.Restrict);

            //  Relación: Una Partida tiene una lista de Jugadores 
            modelBuilder.Entity<Partida>()
                .HasMany(p => p.Jugadores)
                .WithOne(j => j.Partida)
                .HasForeignKey(j => j.PartidaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación: Un Jugador está asociado a un Usuario
            modelBuilder.Entity<Jugador>()
                .HasOne(j => j.UsuarioAsociado)
                .WithMany()
                .HasForeignKey(j => j.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}