using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Truco.Net.Logica;
using Truco.NET.Models;
using Truco.Net.Entidades;

namespace Truco.NET.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly HomeService _homeService;


        public HomeController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IHomeService homeService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _homeService = (HomeService?)homeService;
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
        public IActionResult Login(Usuario usuario)
        {
            // Verifica si la propiedad Nombre o Password están vacías según [Required] de la entidad
            if (string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Password))
            {
                TempData["ErrorLogin"] = "Debes ingresar usuario y contraseña.";
                return RedirectToAction("Index");
            }

            if (_homeService.ValidarUsuario(usuario.Nombre, usuario.Password))
            {
                HttpContext.Session.SetString("UsuarioLogueado", usuario.Nombre);
                TempData["MensajeSuccess"] = $"¡Bienvenido {usuario.Nombre}!";
                return RedirectToAction("Index");
            }

            TempData["ErrorLogin"] = "Usuario o contraseña incorrectos.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Register(Usuario usuario) // 👈 Recibes el objeto Usuario directo
        {
            // 1. Verificamos que cumpla las Data Annotations (si ModelState es válido)
            if (ModelState.IsValid)
            {
                var (exito, mensaje) = _homeService.RegistrarUsuario(usuario.Nombre, usuario.Password);

                if (exito)
                {
                    TempData["MensajeSuccess"] = "Cuenta creada con éxito. Ya puedes iniciar sesión.";
                    return RedirectToAction("Index");
                }

                // Si el usuario ya existe en el array
                TempData["ErrorRegister"] = mensaje;
                return RedirectToAction("Index");
            }

            // 2. Si las Data Annotations fallaron (ej: clave muy corta o campos vacíos)
            TempData["ErrorRegister"] = "Por favor, completa correctamente todos los campos.";
            return RedirectToAction("Index");
        }

        public IActionResult Logout()
        {
            // Limpia todas las variables guardadas en la sesión
            HttpContext.Session.Clear();
            TempData["MensajeSuccess"] = "Has cerrado sesión correctamente.";
            return RedirectToAction("Index");
        }


        /*
         * cuando se Cree DBcontext se usaran estos login/register, que hacen la query para validar el usuario
         * ASP.NET Core Identity es el estandar para verificar usuarios y sus roles en .Net
                [HttpPost]
                [ValidateAntiForgeryToken]
                public async Task<IActionResult> Register(string Usuario, string Password)
                {
                    if (string.IsNullOrEmpty(Usuario) || string.IsNullOrEmpty(Password))
                    {
                        TempData["ErrorRegister"] = "Por favor, completa todos los campos.";
                        return RedirectToAction("Index");
                    }

                    var user = new IdentityUser { UserName = Usuario };
                    var result = await _userManager.CreateAsync(user, Password);

                    if (result.Succeeded)
                    {
                        // Iniciar sesión automáticamente tras registrarse
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Index");
                    }

                    // Capturar errores de Identity (ej. contraseña muy corta)
                    TempData["ErrorRegister"] = string.Join(" ", result.Errors.Select(e => e.Description));
                    return RedirectToAction("Index");
                }

                [HttpPost]
                public async Task<IActionResult> Logout()
                {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction("Index");
                }
            }
                [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
                public IActionResult Error()
                {
                    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
                }
        */
    }

}
