using Microsoft.AspNetCore.SignalR;
using Truco.Net.Entidades;

namespace Truco.Net.Logica
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

        // Método de ejemplo para cuando un jugador se une a una partida existente
        public async Task<bool> UnirseAPartidaAsync(int partidaId, string nombreJugador)
        {
            var partida = _partidas.FirstOrDefault(p => p.Id == partidaId);
            if (partida == null) return false;

            var nuevoJugador = new Jugador
            {
                Id = _nextJugadorId++,
                Nombre = nombreJugador
            };

            partida.Jugadores.Add(nuevoJugador);
            partida.JugadoresActuales = partida.Jugadores.Count;

            // Notifica en tiempo real a todos los miembros de este grupo (partida) en SignalR
            await _hubContext.Clients.Group($"partida_{partida.Id}")
                .SendAsync("ActualizarJugadores", partida.JugadoresActuales, nombreJugador);

            return true;
        }
    }
}
