using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Truco.Net.Entidades;
using Truco.Net.Logica.Servicios;
using Truco.Net.Repositorio;



namespace Truco.Net.Logica.Servicios
{
    public class PartidaService
    {
        private static readonly List<Partida> _partidas = new();
        private static int _nextPartidaId = 1;
        private static int _nextJugadorId = 1;

        // Inyectamos el contexto de SignalR para hablar con los grupos
        private readonly IHubContext<PartidaHub> _hubContext;
        private readonly IServiceProvider _serviceProvider; // Inyectamos el proveedor de servicios

        public PartidaService(IHubContext<PartidaHub> hubContext, IServiceProvider serviceProvider)
        {
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
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

        public async Task<Partida> CrearPartidaAsync(int usuarioId, string nombrePartida, string modo)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EscobaDbContext>();

            var usuario = await context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                throw new Exception("El usuario no está registrado en la base de datos.");
            }

            EliminarPartidasFantasmas(usuario.Id);

            var creador = new Jugador
            {
                Id = _nextJugadorId++,
                UsuarioId = usuario.Id,
                UsuarioAsociado = usuario,
                Puntos = 0,
                EsTurnoActual = true
            };

            // Instanciamos la partida en memoria
            var nuevaPartida = new Partida
            {
                Id = _nextPartidaId++,
                Nombre = nombrePartida,
                Modo = string.IsNullOrEmpty(modo) ? "1v1" : modo,
                UsuarioIdCreador = usuario.Id,
                JugadoresActuales = 1,
                Jugadores = new List<Jugador> { creador }
            };

            _partidas.Add(nuevaPartida);
            return nuevaPartida;
        }


        public async Task<bool> UnirseAPartidaAsync(int partidaId, int usuarioId, string? connectionId = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EscobaDbContext>();

            var partida = BuscarPartidaPorID(partidaId);
            if (partida == null) return false;

            var usuario = await context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return false;

            var jugador = partida.Jugadores.FirstOrDefault(j => j.UsuarioId == usuarioId);
            if (jugador != null)
            {
                // Reconexión: actualizamos su ConnectionId
                jugador.ConnectionId = connectionId ?? string.Empty;
            }
            else
            {
                // Es un nuevo jugador en la partida
                jugador = new Jugador
                {
                    Id = _nextJugadorId++,
                    UsuarioId = usuario.Id,
                    UsuarioAsociado = usuario,
                    ConnectionId = connectionId ?? string.Empty
                };
                partida.Jugadores.Add(jugador);
                partida.JugadoresActuales = partida.Jugadores.Count;
            }

            await _hubContext.Clients.Group(partida.Id.ToString())
                .SendAsync("ActualizarJugadores", partida.JugadoresActuales, usuario.Nombre);

            return true;
        }



        public async Task RemoverJugadorDePartida(int partidaId, int usuarioId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EscobaDbContext>();
            Partida? partida = BuscarPartidaPorID(partidaId);
            if (partida != null)
            {
                var jugadorAEliminar = partida.Jugadores.FirstOrDefault(j => j.UsuarioId == usuarioId);

                if (jugadorAEliminar != null)
                {

                    string nombreUsuario = jugadorAEliminar.UsuarioAsociado?.Nombre;

                    // Por seguridad, si por algún motivo el objeto relacionado fuera nulo en memoria, lo buscamos en la BD
                    if (string.IsNullOrEmpty(nombreUsuario))
                    {
                        var usuarioDb = await context.Usuarios.FindAsync(usuarioId);
                        nombreUsuario = usuarioDb?.Nombre ?? "Jugador";
                    }

                    partida.Jugadores.Remove(jugadorAEliminar);
                    partida.JugadoresActuales = partida.Jugadores.Count;


                    if (partida.Jugadores.Count == 0)
                    {
                        _partidas.Remove(partida);
                    }
                    else
                    {

                        await _hubContext.Clients.Group(partidaId.ToString())
                            .SendAsync("JugadorAbandono", nombreUsuario);
                    }
                }
            }
        }

        private void EliminarPartidasFantasmas(int usuarioIdCreador)
        {
            // Cambiamos p.UsuarioId por p.UsuarioIdCreador
            var partidasAnteriores = _partidas.Where(p => p.UsuarioIdCreador == usuarioIdCreador).ToList();

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

        public bool JugadorSigueConectado(int partidaId, int usuarioId, string viejoConnectionId)
        {
            var partida = BuscarPartidaPorID(partidaId);
            if (partida == null) return false;

            var jugador = partida.Jugadores.FirstOrDefault(j => j.UsuarioId == usuarioId);

            if (jugador != null && jugador.ConnectionId != viejoConnectionId)
            {
                return true;
            }

            return false;
        }

 // funciones para partida Activa 

        public PartidaViewModel? ActualizarDatos(int partidaId, int usuarioId)
        {
            var partida = BuscarPartidaPorID(partidaId);
            if (partida == null)
            {
                return null;
            }

            // Buscamos al jugador que realiza la petición dentro de la partida
            var jugadorSolicitante = partida.Jugadores.FirstOrDefault(j => j.UsuarioId == usuarioId);
            if (jugadorSolicitante == null)
            {
                return null;
            }

            // Identificamos de quién es el turno actual
            var jugadorEnTurno = partida.Jugadores.FirstOrDefault(j => j.EsTurnoActual);

            var viewModel = new PartidaViewModel
            {
                PartidaId = partida.Id,
                NombrePartida = partida.Nombre,
                Modo = partida.Modo,
                Estado = partida.Estado, 

                // Turno
                TurnoJugadorUsuarioId = jugadorEnTurno?.UsuarioId ?? 0,
                EsMiTurno = jugadorSolicitante.EsTurnoActual,

                // Cartas: solo devolvemos las cartas del jugador actual y las de la mesa
                MisCartas = jugadorSolicitante.CartasEnMano != null
                    ? jugadorSolicitante.CartasEnMano.ToList()
                    : new List<Carta>(),

                CartasEnMesa = partida.CartasEnMesa != null
                    ? partida.CartasEnMesa.ToList()
                    : new List<Carta>(),

                // Mapeamos el resumen de los jugadores para la tabla de posiciones y oponentes
                Jugadores = partida.Jugadores.Select(j => new JugadorResumenDto
                {
                    UsuarioId = j.UsuarioId,
                    Nombre = j.UsuarioAsociado?.Nombre ?? "Jugador",
                    Puntos = j.Puntos,
                    CantidadCartasMano = j.CartasEnMano?.Count ?? 0, // No revelamos sus cartas, solo cuántas le quedan
                    EsTurnoActual = j.EsTurnoActual
                }).ToList()
            };

            return viewModel;
        }

        public bool IniciarPartida(int partidaId, int usuarioIdPeticion)
        {
            var partida = BuscarPartidaPorID(partidaId);
            if (partida == null)
            {
                return false; 
            }

            if (partida.Estado != EnumPartida.EnEspera)
            {
                return false; 
            }

            partida.MazoCartas = new Mazo(); 
            partida.MazoCartas.MezclarCartas();   

            // Repartir 3 cartas a cada jugador participante
            foreach (var jugador in partida.Jugadores)
            {
                jugador.CartasEnMano = partida.MazoCartas.RepartirCartas(3);
            }

            // Colocar 4 cartas iniciales sobre la mesa
            partida.CartasEnMesa = partida.MazoCartas.RepartirCartas(4);

            // Asigna el turno inicial
            if (partida.Jugadores.Any())
            {

                foreach (var j in partida.Jugadores)
                {
                    j.EsTurnoActual = false;
                }

                partida.Jugadores.First().EsTurnoActual = true;
            }
            partida.Estado = EnumPartida.EnCurso;

            return true;
        }
    }
}
