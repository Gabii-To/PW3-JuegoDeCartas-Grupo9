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
            EliminarPartidasFantasmas(nombreCreador);

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


        public async Task<bool> UnirseAPartidaAsync(int partidaId, string nombreJugador, string? connectionId = null)
        {
            var partida = BuscarPartidaPorID(partidaId);
            if (partida == null) return false;

            var jugador = partida.Jugadores.FirstOrDefault(j => j.Nombre == nombreJugador);
            if (jugador != null)
            {
                // Si ya estaba en la lista (reconexión), solo actualizamos su ConnectionId
                jugador.ConnectionId = connectionId ?? string.Empty;
            }
            else
            {
                // Si es nuevo dentro de la partida, lo creamos con su ConnectionId
                jugador = new Jugador
                {
                    Id = _nextJugadorId++,
                    Nombre = nombreJugador,
                    ConnectionId = connectionId ?? string.Empty
                };
                partida.Jugadores.Add(jugador);
                partida.JugadoresActuales = partida.Jugadores.Count;
            }

            await _hubContext.Clients.Group(partida.Id.ToString())
                .SendAsync("ActualizarJugadores", partida.JugadoresActuales, nombreJugador);

            return true;
        }



        public async Task RemoverJugadorDePartida(int partidaId, string nombreUsuario)
        {
            Partida? partida = BuscarPartidaPorID(partidaId);
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
                    // Avisamos al grupo
                    await _hubContext.Clients.Group(partidaId.ToString())
                        .SendAsync("JugadorAbandono", nombreUsuario);
                }
            }
        }

        private void EliminarPartidasFantasmas(string nombreCreador)
        {
            var partidasAnteriores = _partidas.Where(p => p.Creador != null && p.Creador.Nombre == nombreCreador).ToList();
            foreach (var partidaAntigua in partidasAnteriores)
            {
                _partidas.Remove(partidaAntigua);
            }
        }

        private static Partida? BuscarPartidaPorID(int partidaId)
        {
            return _partidas.FirstOrDefault(p => p.Id == partidaId);
        }

        
        public (int partidaId, Jugador? jugador) BuscarJugadorPorConnectionId(string connectionId)
        {
            foreach (var partida in _partidas)
            {
                var jugador = partida.Jugadores.FirstOrDefault(j => j.ConnectionId == connectionId);
                if (jugador != null)
                {
                    return (partida.Id, jugador);
                }
            }
            return (0, null);
        }

       public bool JugadorSigueConectado(int partidaId, string nombreUsuario, string viejoConnectionId)
        {
            var partida = BuscarPartidaPorID(partidaId);
            if (partida == null) return false;

            var jugador = partida.Jugadores.FirstOrDefault(j => j.Nombre == nombreUsuario);

            // Si el jugador existe y su ConnectionId actual es DIFERENTE al viejo, significa que se reconectó con éxito
            if (jugador != null && jugador.ConnectionId != viejoConnectionId)
            {
                return true;
            }

            return false;
        }
    }
}
