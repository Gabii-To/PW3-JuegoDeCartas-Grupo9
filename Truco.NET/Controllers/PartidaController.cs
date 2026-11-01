using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Truco.Net.Logica.Servicios;
using Truco.Net.Entidades;

namespace Truco.Net.Controllers
{
    public class PartidaController : Controller
    {
        private readonly PartidaService _partidaService;

        public PartidaController(PartidaService partidaService)
        {
            _partidaService = partidaService;
        }

        [VerificacionSesion]
        public IActionResult Index()
        {
            var partidas = _partidaService.ObtenerPartidasActivas();
            return View(partidas);
        }

        [HttpPost]
        [VerificacionSesion]
        public async Task<IActionResult> Crear(string nombrePartida)
        {
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            if (string.IsNullOrWhiteSpace(nombrePartida))
            {
                TempData["ErrorCrear"] = "Debes ingresar un nombre para la partida.";
                return RedirectToAction("Index");
            }

            var nuevaPartida = await _partidaService.CrearPartidaAsync(usuarioId, nombrePartida);

            return RedirectToAction("Escoba", new { id = nuevaPartida.Id });
        }

        [VerificacionSesion]
        public async Task<IActionResult> Escoba(int id)
        {
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            var partida = _partidaService.ObtenerPartidasActivas()
                                         .Find(p => p.Id == id);

            if (partida == null)
            {
                TempData["ErrorCrear"] = "La partida solicitada no existe o ya finalizó.";
                return RedirectToAction("Index");
            }

            bool yaEstaEnPartida = partida.Jugadores.Exists(j => j.UsuarioId == usuarioId);

            if (!yaEstaEnPartida)
            {
                await _partidaService.UnirseAPartidaAsync(id, usuarioId, null);
            }

            return View("Escoba", partida);
        }

        [HttpGet]
        [VerificacionSesion]
        public async Task<IActionResult> Salir(int id)
        {
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            await _partidaService.RemoverJugadorDePartida(id, usuarioId);

            return RedirectToAction("Index", "Partida");
        }
    }


    //se llama con [verificar sesion[
    public class VerificacionSesionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var usuarioId = context.HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
            {
                context.HttpContext.Session.SetString("ErrorLogin", "Debes iniciar sesión para realizar esta acción.");
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }

            base.OnActionExecuting(context);
        }
    }


}