using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Truco.Net.Logica;
using Truco.Net.Models;

namespace Truco.Net.Controllers
{
    public class PartidaController : Controller
    {
        private readonly PartidaService _partidaService;

        // Inyectamos el servicio de partidas
        public PartidaController(PartidaService partidaService)
        {
            _partidaService = partidaService;
        }

        public IActionResult Index(string modo)
        {
            // Comprobar si el usuario inició sesión
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioLogueado")))
            {
                TempData["ErrorLogin"] = "Debes iniciar sesión para buscar o unirte a una partida.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ModoFiltro = modo;

            // Obtenemos las partidas reales desde el servicio y se las pasamos a la vista
            var partidas = _partidaService.ObtenerPartidasActivas(modo);
            return View(partidas);
        }

        [HttpPost]
        public IActionResult Crear(string nombrePartida, string modo)
        {
            var usuarioLogueado = HttpContext.Session.GetString("UsuarioLogueado");

            if (string.IsNullOrEmpty(usuarioLogueado))
            {
                TempData["ErrorLogin"] = "Debes iniciar sesión para crear una partida.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(nombrePartida))
            {
                TempData["ErrorCrear"] = "Debes ingresar un nombre para la partida.";
                return RedirectToAction("Index", new { modo = modo });
            }

            // Creamos la partida guardando al creador obtenido de la sesión
            var nuevaPartida = _partidaService.CrearPartida(nombrePartida, modo, usuarioLogueado);

            // Redirigimos a la vista de la mesa (Escoba) usando el ID real generado
            return RedirectToAction("Escoba", new { id = nuevaPartida.Id });
        }

        public async Task<IActionResult> Escoba(int id)
        {
            var usuarioLogueado = HttpContext.Session.GetString("UsuarioLogueado");

            if (string.IsNullOrEmpty(usuarioLogueado))
            {
                TempData["ErrorLogin"] = "Debes iniciar sesión para entrar a una mesa.";
                return RedirectToAction("Index", "Home");
            }

            // Buscamos la partida actual en el servicio
            var partida = _partidaService.ObtenerPartidasActivas(null)
                                         .Find(p => p.Id == id);

            if (partida == null)
            {
                TempData["ErrorCrear"] = "La partida solicitada no existe o ya finalizó.";
                return RedirectToAction("Index");
            }

            // Si el jugador no es el creador ni está aún en la lista, se unirá automáticamente
            bool yaEstaEnPartida = partida.Jugadores.Exists(j => j.Nombre == usuarioLogueado);
            if (!yaEstaEnPartida)
            {
                await _partidaService.UnirseAPartidaAsync(id, usuarioLogueado);
            }

            // Retornamos la vista Escoba.cshtml pasando el objeto partida completo
            return View("Escoba", partida);
        }
    }
}