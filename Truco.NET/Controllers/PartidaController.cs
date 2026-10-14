using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Truco.Net.Logica.Servicios;
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

        [VerificacionSesion]
        public IActionResult Index(string modo)
        {


            ViewBag.ModoFiltro = modo;

            // Obtenemos las partidas desde el servicio y se las pasamos a la vista
            var partidas = _partidaService.ObtenerPartidasActivas(modo);
            return View(partidas);
        }

        [HttpPost]
        [VerificacionSesion]
        public IActionResult Crear(string nombrePartida, string modo)
        {
            var usuarioLogueado = HttpContext.Session.GetString("UsuarioLogueado");

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

        [VerificacionSesion]
        public async Task<IActionResult> Escoba(int id)
        {
            var usuarioLogueado = HttpContext.Session.GetString("UsuarioLogueado");

            // Buscamos la partida actual en el servicio
            var partida = _partidaService.ObtenerPartidasActivas(null)
                                         .Find(p => p.Id == id);

            if (partida == null)
            {
                TempData["ErrorCrear"] = "La partida solicitada no existe o ya finalizó.";
                return RedirectToAction("Index");
            }

            // Si el jugador no es el creador ni está en la lista, se une
            bool yaEstaEnPartida = partida.Jugadores.Exists(j => j.Nombre == usuarioLogueado);
            if (!yaEstaEnPartida)
            {
                await _partidaService.UnirseAPartidaAsync(id, usuarioLogueado, null);
            }

            
            return View("Escoba", partida);
        }


        [HttpGet]
        [VerificacionSesion]
        public async Task<IActionResult> Salir(int id)
        {
            var usuarioLogueado = HttpContext.Session.GetString("UsuarioLogueado");

            await _partidaService.RemoverJugadorDePartida(id, usuarioLogueado);

            return RedirectToAction("Index", "Partida");
        }
    }


    //se llama con [verificar sesion[
    public class VerificacionSesionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var usuarioLogueado = context.HttpContext.Session.GetString("UsuarioLogueado");

            if (string.IsNullOrEmpty(usuarioLogueado))
            {
                context.HttpContext.Session.SetString("ErrorLogin", "Debes iniciar sesión para realizar esta acción.");
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }

            base.OnActionExecuting(context);
        }
    }


}