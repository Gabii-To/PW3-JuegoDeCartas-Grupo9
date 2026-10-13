using System;
using System.Collections.Generic;
using System.Text;

namespace Truco.Net.Entidades
{
    public class Partida
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Modo { get; set; } = "1v1"; // "1v1" o "4p"

        public int JugadoresActuales { get; set; } = 1;

        public Jugador Creador { get; set; } = new Jugador();

        public List<Jugador> Jugadores { get; set; } = new List<Jugador>();
    }
}