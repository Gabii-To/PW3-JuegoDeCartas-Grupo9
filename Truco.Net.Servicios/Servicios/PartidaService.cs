using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Truco.Net.Models;
using Truco.Net.Logica.Servicios;


namespace Truco.Net.Logica.Servicios
{
    public class PartidaService
    {
        private static readonly List<Partida> _partidas = new();
        private static int _nextPartidaId = 1;
        private static int _nextJugadorId = 1;

        // Inyectamos el contexto de SignalR para hablar con los grupos
        private readonly IHubContext<PartidaHub> _hubContext;

        public PartidaService(IHubContext<PartidaHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public List<Partida> ObtenerPartidasActivas(string modo)
        {
            if (string.IsNullOrEmpty(modo))
            {
                return _partidas.ToList();
            }

            return _partidas
                .Where(p => p.Modo.Equals(modo, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public Partida CrearPartida(string nombrePartida, string modo, string nombreCreador)
        {
            // Se busca si hay una partida activa con el mismo creador, para no hacer mesas fantasmas
            var partidasAnteriores = _partidas.Where(p => p.Creador != null && p.Creador.Nombre == nombreCreador).ToList();
            foreach (var partidaAntigua in partidasAnteriores)
            {
                _partidas.Remove(partidaAntigua);
            }

            
            var creador = new Jugador
            {
                Id = _nextJugadorId++,
                Nombre = nombreCreador
            };

            var nuevaPartida = new Partida
            {
                Id = _nextPartidaId++,
                Nombre = nombrePartida,
                Modo = string.IsNullOrEmpty(modo) ? "1v1" : modo,
                Creador = creador,
                JugadoresActuales = 1,
                Jugadores = new List<Jugador> { creador }
            };

            _partidas.Add(nuevaPartida);
            return nuevaPartida;
        }

        public async Task<bool> UnirseAPartidaAsync(int partidaId, string nombreJugador)
        {
            var partida = _partidas.FirstOrDefault(p => p.Id == partidaId);
            if (partida == null) return false;

            // Evitar duplicar si ya está en la lista de jugadores de la memoria
            if (partida.Jugadores.Any(j => j.Nombre == nombreJugador))
            {
                return true;
            }

            var nuevoJugador = new Jugador
            {
                Id = _nextJugadorId++,
                Nombre = nombreJugador
            };

            partida.Jugadores.Add(nuevoJugador);
            partida.JugadoresActuales = partida.Jugadores.Count;

            // CORRECCIÓN: Usar exactamente el mismo formato de grupo que en el Hub (ej: partida.Id.ToString())
            await _hubContext.Clients.Group(partida.Id.ToString())
                .SendAsync("ActualizarJugadores", partida.JugadoresActuales, nombreJugador);

            return true;
        }

        public async Task RemoverJugadorDePartida(int partidaId, string nombreUsuario)
        {
            var partida = _partidas.FirstOrDefault(p => p.Id == partidaId);
            if (partida != null)
            {
                var jugadorAEliminar = partida.Jugadores.FirstOrDefault(j => j.Nombre == nombreUsuario);
                if (jugadorAEliminar != null)
                {
                    partida.Jugadores.Remove(jugadorAEliminar);
                    partida.JugadoresActuales = partida.Jugadores.Count;
                }

                // Si la partida se queda sin jugadores, la borramos
                if (partida.Jugadores.Count == 0)
                {
                    _partidas.Remove(partida);
                }
                else
                {
                    // NOTIFICACIÓN EN TIEMPO REAL: Avisamos a los demás miembros del grupo
                    await _hubContext.Clients.Group(partidaId.ToString())
                        .SendAsync("JugadorAbandono", nombreUsuario);
                }
            }
        }
    }
}
