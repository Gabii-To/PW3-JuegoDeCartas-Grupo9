using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

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

        public async Task UnirseAMesa(string partidaIdStr, string nombreUsuario)
        {
            if (int.TryParse(partidaIdStr, out int partidaId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, partidaIdStr);

                // Registramos en el servicio al usuario
                await _partidaService.UnirseAPartidaAsync(partidaId, nombreUsuario, Context.ConnectionId);
                // Guardamos a qué partida pertenece esta conexión
                _conexionPartida[Context.ConnectionId] = partidaId;
            }
        }

        public async Task SalirDeMesa(string partidaIdStr, string nombreUsuario)
        {
            if (int.TryParse(partidaIdStr, out int partidaId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, partidaIdStr);
                _conexionPartida.TryRemove(Context.ConnectionId, out _);

                await _partidaService.RemoverJugadorDePartida(partidaId, nombreUsuario);
            }
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            // Si esta conexión pertenecía a una partida, la recuperamos con el ID
            if (_conexionPartida.TryRemove(Context.ConnectionId, out int partidaId))
            {
                // ⏳ Esperamos los 5 segundos del temporizador
                await Task.Delay(5000);

                // Verificamos si el ConnectionId sigue sin estar en el diccionario 
                // (Si se hubiera reconectado, su ConnectionId habría cambiado por uno nuevo)
                bool sigueDesconectado = !_conexionPartida.ContainsKey(Context.ConnectionId);

                if (sigueDesconectado)
                {
                    // Como no tenemos el nombre exacto aquí, podemos pedirle al servicio 
                    // que limpie las conexiones muertas o manejarlo por ID de partida.
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
