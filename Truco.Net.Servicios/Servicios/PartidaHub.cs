using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace Truco.Net.Logica.Servicios
{
    public class PartidaHub : Hub
    {
       
        public async Task UnirseAMesa(string partidaId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"partida_{partidaId}");
        }

        public async Task SalirDeMesa(string partidaId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"partida_{partidaId}");
        }


    }
}
