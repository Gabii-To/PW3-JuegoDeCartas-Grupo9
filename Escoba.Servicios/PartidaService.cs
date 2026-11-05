using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Escoba.Entidades;
using Escoba.Repositorio;

namespace Escoba.Servicios;

public class PartidaService
{
    private const string ModoUnico = "1v1";
    private const int MaxJugadores = 2;

    private static readonly List<Partida> _partidas = new();
    private static readonly object _partidasLock = new();
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> _candados = new();

    private static int _nextPartidaId = 1;
    private static int _nextJugadorId = 1;

    private readonly IHubContext<PartidaHub> _hub;
    private readonly IServiceProvider _provider;

    public PartidaService(IHubContext<PartidaHub> hub, IServiceProvider provider)
    {
        _hub = hub;
        _provider = provider;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    public List<Partida> ObtenerPartidasActivas()
    {
        lock (_partidasLock)
            return _partidas.Where(p => p.Modo == ModoUnico).ToList();
    }

    public PartidaViewModel? ActualizarDatos(int id, int usuarioId)
    {
        var partida = Buscar(id);
        var jugador = partida?.Jugadores.FirstOrDefault(j => j.UsuarioId == usuarioId);

        if (partida is null || jugador is null)
            return null;

        var jugadorActual = partida.Jugadores.FirstOrDefault(j => j.EsTurnoActual);

        return new PartidaViewModel
        {
            PartidaId = partida.Id,
            NombrePartida = partida.Nombre,
            Modo = ModoUnico,
            Estado = partida.Estado,
            TurnoJugadorUsuarioId = jugadorActual?.UsuarioId ?? 0,
            EsMiTurno = jugador.EsTurnoActual,
            MisCartas = jugador.CartasEnMano.ToList(),
            CartasEnMesa = partida.CartasEnMesa.ToList(),
            CartasRestantesEnMazo = partida.MazoCartas.Cartas.Count,
            MensajeFinal = partida.MensajeFinal,
            GanadoresUsuarioIds = partida.GanadoresUsuarioIds.ToList(),

            Jugadores = partida.Jugadores.Select(j => new JugadorResumenDto
            {
                UsuarioId = j.UsuarioId,
                Nombre = j.UsuarioAsociado?.Nombre ?? "Jugador",
                Puntos = j.CartasGanadas.Count,
                CantidadCartasCapturadas = j.CartasGanadas.Count,
                Escobas = j.Escobas,
                CantidadCartasMano = j.CartasEnMano.Count,
                EsTurnoActual = j.EsTurnoActual
            }).ToList()
        };
    }

    // =====================================================
    // CREACIÓN DE PARTIDAS
    // =====================================================

    public async Task<Partida> CrearPartidaAsync(int usuarioId, string nombre)
    {
        var usuario = await Usuario(usuarioId)
            ?? throw new InvalidOperationException("El usuario no está registrado.");

        Partida nueva;
        List<Partida> viejas;

        lock (_partidasLock)
        {
            viejas = _partidas.Where(p => p.UsuarioIdCreador == usuarioId).ToList();

            foreach (var partida in viejas)
                _partidas.Remove(partida);

            var creador = new Jugador
            {
                Id = _nextJugadorId++,
                UsuarioId = usuario.Id,
                UsuarioAsociado = usuario,
                EsTurnoActual = true
            };

            nueva = new Partida
            {
                Id = _nextPartidaId++,
                Nombre = nombre.Trim(),
                Modo = ModoUnico,
                UsuarioIdCreador = usuario.Id,
                Creador = creador,
                JugadoresActuales = 1,
                Jugadores = new() { creador }
            };

            _partidas.Add(nueva);
        }

        foreach (var partida in viejas)
            await SalaEliminada(partida);

        await SalaCreada(nueva);

        return nueva;
    }

    // =====================================================
    // UNIÓN DE JUGADORES
    // =====================================================

    public async Task<bool> UnirseAPartidaAsync(int id, int usuarioId, string? connectionId = null)
    {
        var usuario = await Usuario(usuarioId);
        if (usuario is null) return false;

        var sem = Candado(id);
        await sem.WaitAsync();

        Partida? partida;
        bool nuevo = false;
        bool inicio = false;

        try
        {
            partida = Buscar(id);

            if (partida is null || partida.Estado == EnumPartida.Finalizado)
                return false;

            var jugador = partida.Jugadores.FirstOrDefault(x => x.UsuarioId == usuarioId);

            if (jugador is null)
            {
                if (partida.Jugadores.Count >= MaxJugadores ||
                    partida.Estado != EnumPartida.EnEspera)
                    return false;

                jugador = new Jugador
                {
                    Id = _nextJugadorId++,
                    UsuarioId = usuario.Id,
                    UsuarioAsociado = usuario,
                    ConnectionId = connectionId ?? ""
                };

                partida.Jugadores.Add(jugador);
                partida.JugadoresActuales = partida.Jugadores.Count;
                nuevo = true;

                if (partida.Jugadores.Count == MaxJugadores)
                {
                    Iniciar(partida);
                    inicio = true;
                }
            }
            else
            {
                jugador.ConnectionId = connectionId ?? "";
            }
        }
        finally
        {
            sem.Release();
        }

        if (partida is null) return false;

        if (nuevo)
        {
            await _hub.Clients.Group(id.ToString())
                .SendAsync("ActualizarJugadores", partida.JugadoresActuales, usuario.Nombre);

            await SalaActualizada(partida);
        }

        if (inicio)
            await _hub.Clients.Group(id.ToString()).SendAsync("PartidaIniciada");

        return true;
    }

    // =====================================================
    // ELIMINACIÓN Y DESCONEXIÓN
    // =====================================================

    public async Task RemoverJugadorDePartida(int id, int usuarioId)
    {
        var partida = Buscar(id);
        if (partida is null) return;

        var jugador = partida.Jugadores.FirstOrDefault(j => j.UsuarioId == usuarioId);
        if (jugador is null) return;

        partida.Jugadores.Remove(jugador);
        partida.JugadoresActuales = partida.Jugadores.Count;

        if (partida.Jugadores.Count == 0)
        {
            lock (_partidasLock)
                _partidas.Remove(partida);

            await SalaEliminada(partida);
        }
        else
        {
            await _hub.Clients.Group(id.ToString())
                .SendAsync("JugadorAbandono", jugador.UsuarioAsociado?.Nombre ?? "Jugador");

            await SalaActualizada(partida);
        }
    }

    public async Task RemoverJugadorDeTodasLasPartidas(int usuarioId)
    {
        var partidas = ObtenerPartidasActivas()
            .Where(p => p.Jugadores.Any(j => j.UsuarioId == usuarioId))
            .Select(p => p.Id)
            .ToList();

        foreach (var id in partidas)
            await RemoverJugadorDePartida(id, usuarioId);
    }

    public async Task RemoverJugadorPorConexionAsync(int partidaId, int usuarioId, string connectionId)
    {
        var jugador = Buscar(partidaId)?.Jugadores
            .FirstOrDefault(j => j.UsuarioId == usuarioId);

        if (jugador?.ConnectionId == connectionId)
            await RemoverJugadorDePartida(partidaId, usuarioId);
    }

    public (int partidaId, Jugador? jugador) BuscarJugadorPorConnectionId(string connectionId)
    {
        lock (_partidasLock)
        {
            foreach (var partida in _partidas)
            {
                var jugador = partida.Jugadores.FirstOrDefault(x => x.ConnectionId == connectionId);

                if (jugador is not null)
                    return (partida.Id, jugador);
            }
        }

        return (0, null);
    }

    public bool JugadorSigueConectado(int partidaId, int usuarioId, string connectionId)
    {
        return Buscar(partidaId)?.Jugadores
            .FirstOrDefault(j => j.UsuarioId == usuarioId)?
            .ConnectionId != connectionId;
    }

    // =====================================================
    // INICIO DE PARTIDA
    // =====================================================

    public bool IniciarPartida(int id, int usuarioId)
    {
        var partida = Buscar(id);

        if (partida is null ||
            partida.Estado != EnumPartida.EnEspera ||
            partida.Jugadores.Count != MaxJugadores ||
            !partida.Jugadores.Any(j => j.UsuarioId == usuarioId))
            return false;

        Iniciar(partida);
        return true;
    }

    private static void Iniciar(Partida partida)
    {
        partida.MazoCartas = new Mazo();
        partida.MazoCartas.MezclarCartas();

        foreach (var jugador in partida.Jugadores)
        {
            jugador.CartasEnMano.Clear();
            jugador.CartasGanadas.Clear();
            jugador.Escobas = 0;
            jugador.EsTurnoActual = false;
        }

        Repartir(partida);
        partida.CartasEnMesa = partida.MazoCartas.RepartirCartas(4);
        partida.Jugadores[0].EsTurnoActual = true;
        partida.Estado = EnumPartida.EnCurso;
    }

    private static void Repartir(Partida partida)
    {
        foreach (var jugador in partida.Jugadores)
            jugador.CartasEnMano.AddRange(partida.MazoCartas.RepartirCartas(3));
    }

    // =====================================================
    // JUGADAS: CAPTURAS Y DESCARTES
    // =====================================================

    public Task<bool> DejarCartaAsync(int partidaId, int usuarioId, int cartaId)
    {
        return Jugada(partidaId, usuarioId, cartaId, Array.Empty<int>(), false);
    }

    public Task<bool> ValidarJugadaAsync(int partidaId, int usuarioId, int cartaId, List<int> cartasMesa)
    {
        return Jugada(partidaId, usuarioId, cartaId, cartasMesa, true);
    }

    private async Task<bool> Jugada(int id, int usuarioId, int cartaId, IReadOnlyCollection<int> ids, bool captura)
    {
        var sem = Candado(id);
        await sem.WaitAsync();

        bool ok = false;

        try
        {
            var partida = Buscar(id);
            var jugador = partida?.Jugadores.FirstOrDefault(x => x.UsuarioId == usuarioId);

            if (partida is null || jugador is null ||
                partida.Estado != EnumPartida.EnCurso || !jugador.EsTurnoActual)
                return false;

            var carta = jugador.CartasEnMano.FirstOrDefault(c => c.Id == cartaId);
            if (carta is null) return false;

            if (captura)
            {
                if (ids.Count == 0 || ids.Distinct().Count() != ids.Count)
                    return false;

                var mesa = partida.CartasEnMesa.Where(c => ids.Contains(c.Id)).ToList();

                if (mesa.Count != ids.Count ||
                    carta.ValorEscoba + mesa.Sum(c => c.ValorEscoba) != 15)
                    return false;

                jugador.CartasEnMano.Remove(carta);

                foreach (var cartaMesa in mesa)
                    partida.CartasEnMesa.Remove(cartaMesa);

                jugador.CartasGanadas.Add(carta);
                jugador.CartasGanadas.AddRange(mesa);

                partida.UltimoCapturadorUsuarioId = usuarioId;

                if (partida.CartasEnMesa.Count == 0)
                    jugador.Escobas++;
            }
            else
            {
                jugador.CartasEnMano.Remove(carta);
                partida.CartasEnMesa.Add(carta);
            }

            Siguiente(partida);
            ok = true;
        }
        finally
        {
            sem.Release();
        }

        if (ok)
            await _hub.Clients.Group(id.ToString()).SendAsync("ActualizarEstadoPartida");

        return ok;
    }

    // =====================================================
    // TURNOS Y RONDAS
    // =====================================================

    private static void Siguiente(Partida partida)
    {
        partida.CambiarTurno();

        if (partida.Jugadores.All(j => j.CartasEnMano.Count == 0))
        {
            if (partida.MazoCartas.Cartas.Count > 0)
                Repartir(partida);
            else
                Finalizar(partida);
        }

        var actual = partida.Jugadores.FindIndex(j => j.EsTurnoActual);

        if (partida.Estado == EnumPartida.EnCurso &&
            partida.Jugadores[actual].CartasEnMano.Count == 0)
        {
            partida.Jugadores[actual].EsTurnoActual = false;
            partida.Jugadores.First(j => j.CartasEnMano.Count > 0).EsTurnoActual = true;
        }
    }

    // =====================================================
    // FINALIZACIÓN
    // =====================================================

    public bool FinalizarPartida(int id)
    {
        var partida = Buscar(id);

        if (partida is null || partida.Estado != EnumPartida.EnCurso)
            return false;

        Finalizar(partida);
        return true;
    }

    private static void Finalizar(Partida partida)
    {
        var ultimoCapturador = partida.UltimoCapturadorUsuarioId is int usuarioId
            ? partida.Jugadores.FirstOrDefault(j => j.UsuarioId == usuarioId)
            : null;

        if (ultimoCapturador is not null)
        {
            ultimoCapturador.CartasGanadas.AddRange(partida.CartasEnMesa);
            partida.CartasEnMesa.Clear();
        }

        if (partida.CartasEnMesa.Count > 0)
        {
            partida.MensajeFinal = "La partida terminó sin capturas; las cartas de la mesa quedan sin asignar hasta definir esa regla.";
            partida.GanadoresUsuarioIds.Clear();
        }
        else
        {
            var maximo = partida.Jugadores.Max(j => j.CartasGanadas.Count);

            partida.GanadoresUsuarioIds = partida.Jugadores
                .Where(j => j.CartasGanadas.Count == maximo)
                .Select(j => j.UsuarioId)
                .ToList();

            partida.MensajeFinal = partida.GanadoresUsuarioIds.Count > 1
                ? "La partida terminó en empate."
                : "La partida finalizó.";
        }

        foreach (var jugador in partida.Jugadores)
            jugador.EsTurnoActual = false;

        partida.Estado = EnumPartida.Finalizado;
    }

    // =====================================================
    // MÉTODOS AUXILIARES
    // =====================================================

    private static SemaphoreSlim Candado(int id)
    {
        return _candados.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
    }

    private static Partida? Buscar(int id)
    {
        lock (_partidasLock)
            return _partidas.FirstOrDefault(p => p.Id == id);
    }

    private async Task<Usuario?> Usuario(int id)
    {
        using var scope = _provider.CreateScope();

        return await scope.ServiceProvider
            .GetRequiredService<EscobaDbContext>()
            .Usuarios.FindAsync(id);
    }

    // =====================================================
    // NOTIFICACIONES DEL LOBBY
    // =====================================================

    private Task SalaCreada(Partida partida)
    {
        return _hub.Clients.Group(PartidaHub.LobbyGroup)
            .SendAsync("SalaCreada", Resumen(partida));
    }

    private Task SalaEliminada(Partida partida)
    {
        return _hub.Clients.Group(PartidaHub.LobbyGroup)
            .SendAsync("SalaEliminada", new { id = partida.Id });
    }

    private Task SalaActualizada(Partida partida)
    {
        return _hub.Clients.Group(PartidaHub.LobbyGroup)
            .SendAsync("SalaActualizada", Resumen(partida));
    }

    private static object Resumen(Partida partida)
    {
        return new
        {
            id = partida.Id,
            nombre = partida.Nombre,
            modo = ModoUnico,
            creador = partida.Creador?.UsuarioAsociado?.Nombre ?? "No disponible",
            jugadoresActuales = partida.JugadoresActuales
        };
    }
}