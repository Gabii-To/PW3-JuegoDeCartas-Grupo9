using System;
using System.Collections.Generic;
using System.Text;

namespace Truco.Net.Entidades
{
    public class Jugador
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public bool EsBot { get; set; } = false;
    }
}