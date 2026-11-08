
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Escoba.Servicios;
using Escoba.Web.Models;
using Escoba.Entidades;

namespace Escoba.Web.Controllers
{
    public class HomeController : Controller
    {


        private readonly HomeService _homeService;
        private readonly PartidaService _partidaService;


        public HomeController(

            IHomeService homeService,
            PartidaService partidaService)
        {
            _homeService = (HomeService?)homeService;
            _partidaService = partidaService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }


        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Login(string Nombre, string Password)
        {
            var usuarioEnDb = await _homeService.ValidarUsuarioAsync(Nombre, Password);

            if (usuarioEnDb != null)
            {
                HttpContext.Session.SetInt32("UsuarioId", usuarioEnDb.Id);
                HttpContext.Session.SetString("UsuarioLogueado", usuarioEnDb.Nombre);

                return RedirectToAction("Index", "Partida");
            }

            TempData["ErrorLogin"] = "Usuario o contraseña incorrectos.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Register(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                var (exito, mensaje) = await _homeService.RegistrarUsuarioAsync(usuario.Nombre, usuario.Password);

                if (exito)
                {
                    TempData["MensajeSuccess"] = "Cuenta creada con éxito. Ya puedes iniciar sesión.";
                    return RedirectToAction("Index");
                }

                // Si el usuario ya existe en el array
                TempData["ErrorRegister"] = mensaje;
                return RedirectToAction("Index");
            }

            // Si las Data Annotations fallaron (ej: clave muy corta o campos vacíos)
            var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorRegister"] = string.Join(" ", errores);

            return RedirectToAction("Index");

        }


        public async Task<IActionResult> Logout()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId.HasValue)
            {
                // La sesión y las partidas se almacenan por separado. Por eso,
                // cerrar la sesión también debe retirar al usuario de las mesas
                // antes de borrar sus datos de sesión.
                await _partidaService.RemoverJugadorDeTodasLasPartidas(usuarioId.Value);
            }

            // Limpia todas las variables guardadas en la sesión
            HttpContext.Session.Clear();
            TempData["MensajeSuccess"] = "Has cerrado sesión correctamente.";
            return RedirectToAction("Index");
        }

    }

}
