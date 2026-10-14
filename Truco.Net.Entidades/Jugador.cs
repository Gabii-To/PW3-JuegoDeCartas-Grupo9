using System;
using System.Collections.Generic;
using System.Text;

namespace Truco.Net.Models
{
    public class Jugador
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        // para que el Hub pueda detectar la caida de conexion y futura reconexion
        public string ConnectionId { get; set; } = string.Empty;
    }
}