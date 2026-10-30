using System;
using System.Collections.Generic;
using System.Text;

namespace Truco.Net.Entidades
{
    public class PartidaViewModel
    {
        public int PartidaId { get; set; }
        public string NombrePartida { get; set; } = string.Empty;
        public string Modo { get; set; } = string.Empty;
        public EnumPartida Estado { get; set; }

        // Turno
        public int TurnoJugadorUsuarioId { get; set; }
        public bool EsMiTurno { get; set; }

        // Cartas
        public List<Carta> MisCartas { get; set; } = new();
        public List<Carta> CartasEnMesa { get; set; } = new();

        // Información de todos los participantes
        public List<JugadorResumenDto> Jugadores { get; set; } = new();
    }

    public class JugadorResumenDto
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Puntos { get; set; }
        public int CantidadCartasMano { get; set; }
        public bool EsTurnoActual { get; set; }
    }
}
