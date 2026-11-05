using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Escoba.Servicios;
using Escoba.Entidades;

namespace Escoba.Web.Controllers
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

            // La vista recibe un ViewModel: solo trae las cartas del usuario que
            // mira, las de la mesa y la cantidad de cartas del rival (no cuáles son).
            var viewModel = _partidaService.ActualizarDatos(id, usuarioId);

            if (viewModel == null)
            {
                TempData["ErrorCrear"] = "No pudiste ingresar a la partida (la sala está llena o ya no existe).";
                return RedirectToAction("Index");
            }

            return View("Escoba", viewModel);
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
