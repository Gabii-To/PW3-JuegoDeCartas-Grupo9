using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Truco.Net.Entidades;

namespace Truco.Net.Entidades
{
    public enum EnumPartida
    {
        EnEspera,
        EnCurso,
        Finalizado
    }
    public class Partida
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioIdCreador { get; set; }

        [Required(ErrorMessage = "El nombre de la partida es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Modo { get; set; } = "1v1";

        [Range(1, 2, ErrorMessage = "La cantidad de jugadores debe estar entre 1 y 2.")]
        public int JugadoresActuales { get; set; } = 1;

        
        [Required]
        public EnumPartida Estado { get; set; } = EnumPartida.EnEspera;

        public Jugador Creador { get; set; } = new Jugador();

        public List<Jugador> Jugadores { get; set; } = new List<Jugador>();
        [NotMapped]
        public Mazo MazoCartas { get; set; } = new Mazo();
        [NotMapped]
        public List<Carta> CartasEnMesa { get; set; } = new List<Carta>();



        public void IniciarPartida()
        {
            if (Estado == EnumPartida.EnEspera)
            {
                Estado = EnumPartida.EnCurso;
                MazoCartas.InicializarMazoEspanol();
                MazoCartas.MezclarCartas();
            }
        }

        // Finaliza el juego
        public void FinalizarPartida()
        {
            Estado = EnumPartida.Finalizado;
        }

        
        public void CambiarTurno()
        {
            var jugadorActual = Jugadores.FirstOrDefault(j => j.EsTurnoActual);
            if (jugadorActual != null)
            {
                jugadorActual.EsTurnoActual = false;

                int indexActual = Jugadores.IndexOf(jugadorActual);
                int indexSiguiente = (indexActual + 1) % Jugadores.Count;

                Jugadores[indexSiguiente].EsTurnoActual = true;
            }
            else if (Jugadores.Count > 0)
            {
                // Si nadie tiene el turno asignado, arranca el primero
                Jugadores[0].EsTurnoActual = true;
            }
        }
    }
}