using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Truco.Net.Logica.Servicios
{
    public class PartidaHub : Hub
    {
        private readonly PartidaService _partidaService;

        // Solo guardamos la relación: ConnectionId de la pestaña -> ID de la partida
        private static readonly ConcurrentDictionary<string, int> _conexionPartida = new();

        public PartidaHub(PartidaService partidaService)
        {
            _partidaService = partidaService;
        }

        public async Task UnirseAMesa(string partidaIdStr, int usuarioId)
        {
            if (int.TryParse(partidaIdStr, out int partidaId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, partidaIdStr);

                // Registramos la conexión en la partida. Si la partida ya no existe
                // o el usuario no puede unirse, quitamos la conexión del grupo.
                var seUnio = await _partidaService.UnirseAPartidaAsync(partidaId, usuarioId, Context.ConnectionId);

                if (seUnio)
                {
                    _conexionPartida[Context.ConnectionId] = partidaId;
                }
                else
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, partidaIdStr);
                }
            }
        }

        public async Task SalirDeMesa(string partidaIdStr, int usuarioId)
        {
            if (int.TryParse(partidaIdStr, out int partidaId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, partidaIdStr);

                // Llamamos al servicio pasando el ID numérico
                await _partidaService.RemoverJugadorDePartida(partidaId, usuarioId);

                _conexionPartida.TryRemove(Context.ConnectionId, out _);
            }
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            // Si esta conexión pertenecía a una partida, la recuperamos con el ID
            if (_conexionPartida.TryRemove(Context.ConnectionId, out int partidaId))
            {
                var jugador = _partidaService.BuscarJugadorPorConnectionId(Context.ConnectionId).jugador;

                // Esperamos unos segundos para diferenciar una caída momentánea
                // de una salida real antes de eliminar al jugador de la mesa.
                await Task.Delay(5000);

                if (jugador != null)
                {
                    await _partidaService.RemoverJugadorPorConexionAsync(
                        partidaId,
                        jugador.UsuarioId,
                        Context.ConnectionId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
