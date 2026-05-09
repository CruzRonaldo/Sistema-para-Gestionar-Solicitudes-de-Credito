using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using trabajo.Models;
using trabajo.Service;
using System.Text.RegularExpressions;

namespace trabajo.Controllers
{

    public class LoginController : Controller
    {
        private readonly IusuarioServices _UsuarioService;
        private readonly UsuarioContext _Context;
        private static string codigoGlobal = "";
        private readonly EmailService _emailService = new EmailService();

        public LoginController(IusuarioServices usuarioService, UsuarioContext context)
        {
            _UsuarioService = usuarioService;
            _Context = context;
        }

        public IActionResult Registro()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Registro(Usuario usuario, string confirmarClave, string codigoVerificacion)
        {
            if (!Regex.IsMatch(usuario.Dni, @"^\d{8}$"))
            {
                ViewData["mensaje"] = "El DNI debe tener exactamente 8 números.";
                return View(usuario);
            }

            if (!Regex.IsMatch(usuario.Celular, @"^9\d{8}$"))
            {
                ViewData["mensaje"] = "El celular debe tener 9 números y empezar con 9.";
                return View(usuario);
            }

            if (!Regex.IsMatch(usuario.Correo, @"^[A-Za-z0-9._%+-]+@gmail\.com$"))
            {
                ViewData["mensaje"] = "El correo debe ser Gmail.";
                return View(usuario);
            }

            if (!Regex.IsMatch(usuario.clave, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
            {
                ViewData["mensaje"] = "La contraseña debe tener mínimo 6 caracteres, una mayúscula, una minúscula y un número.";
                return View(usuario);
            }

            if (usuario.clave != confirmarClave)
            {
                ViewData["mensaje"] = "Las contraseñas no coinciden.";
                return View(usuario);
            }

            bool existe = _Context.Usuario.Any(x =>
                x.Dni == usuario.Dni ||
                x.Celular == usuario.Celular ||
                x.Correo == usuario.Correo
            );

            if (existe)
            {
                ViewData["mensaje"] = "El DNI, celular o correo ya está registrado.";
                return View(usuario);
            }

            if (codigoVerificacion != codigoGlobal)
            {
                ViewData["mensaje"] = "El código de verificación es incorrecto.";
                return View(usuario);
            }

            usuario.clave = utilidades.EncriptarClave(usuario.clave);

            Usuario usuarioCreado = await _UsuarioService.SaveUsuario(usuario);

            if (usuarioCreado.Id > 0)
            {
                TempData["Mensaje"] = "Usuario registrado exitosamente";
                return RedirectToAction("IniciarSeccion", "Login");
            }

            ViewData["mensaje"] = "No se pudo crear el usuario.";
            return View(usuario);
        }
        [HttpGet]
        public IActionResult IniciarSeccion()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> IniciarSeccion(string dni, string clave)
        {
            if (string.IsNullOrWhiteSpace(dni) || !Regex.IsMatch(dni, @"^\d{8}$"))
            {
                ViewData["Mensaje"] = "El DNI debe tener exactamente 8 números.";
                return View();
            }

            bool dniExiste = _Context.Usuario.Any(x => x.Dni == dni);

            if (!dniExiste)
            {
                ViewData["Mensaje"] = "No estás registrado. Primero debes crear una cuenta.";
                return View();
            }

            Usuario usuarioEncontrado = _Context.Usuario.FirstOrDefault(x =>
                x.Dni == dni &&
                x.clave == utilidades.EncriptarClave(clave)
            );

            if (usuarioEncontrado == null)
            {
                ViewData["Mensaje"] = "La contraseña es incorrecta.";
                return View();
            }

            List<Claim> claims = new List<Claim>()
            {
             new Claim(ClaimTypes.Name, usuarioEncontrado.Nombre),
             new Claim("Apellido", usuarioEncontrado.Apellido),
             new Claim("Dni", usuarioEncontrado.Dni),
             new Claim("Celular", usuarioEncontrado.Celular),
             new Claim("Correo", usuarioEncontrado.Correo)
            };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            AuthenticationProperties properties = new AuthenticationProperties()
            {
                AllowRefresh = true,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                properties
            );

            return RedirectToAction("SolicitarCredito", "Login");

        }

        [HttpGet]
        public IActionResult OlvideContrasena()
        {
            return View();
        }

        [HttpPost]
        public IActionResult OlvideContrasena(string dni, string nuevaClave, string confirmarClave)
        {
            if (string.IsNullOrWhiteSpace(dni) || !Regex.IsMatch(dni, @"^\d{8}$"))
            {
                ViewData["Mensaje"] = "El DNI debe tener exactamente 8 números.";
                return View();
            }

            if (!Regex.IsMatch(nuevaClave, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
            {
                ViewData["Mensaje"] = "La contraseña debe tener mínimo 6 caracteres, una mayúscula, una minúscula y un número.";
                return View();
            }

            if (nuevaClave != confirmarClave)
            {
                ViewData["Mensaje"] = "Las contraseñas no coinciden.";
                return View();
            }

            Usuario usuario = _Context.Usuario.FirstOrDefault(x => x.Dni == dni);

            if (usuario == null)
            {
                ViewData["Mensaje"] = "No existe un usuario con ese DNI.";
                return View();
            }
            if (usuario.clave == utilidades.EncriptarClave(nuevaClave))
            {
                ViewData["Mensaje"] = "La nueva contraseña no puede ser igual a la contraseña actual.";
                return View();
            }

            usuario.clave = utilidades.EncriptarClave(nuevaClave);
            _Context.SaveChanges();

            TempData["Mensaje"] = "Contraseña actualizada correctamente";
            return RedirectToAction("IniciarSeccion", "Login");
        }

            [HttpPost]
        public IActionResult EnviarCodigo(string correo)
        {
            try
            {
                string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                Random random = new Random();

                codigoGlobal = new string(Enumerable.Repeat(caracteres, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                _emailService.EnviarCodigo(correo, codigoGlobal);

                return Json(new { ok = true, mensaje = "Código enviado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message });
            }
        }
        public IActionResult SolicitarCredito()
        {
            return View();
        }
    }
}
