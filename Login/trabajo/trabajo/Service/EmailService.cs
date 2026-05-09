using System.Net;
using System.Net.Mail;

namespace trabajo.Service
{
    public class EmailService
    {
        public void EnviarCodigo(string destino, string codigo)
        {
            var remitente = "rafaelhugorosalespapuico@gmail.com";
            var password = "gvzkraxpqxosvjxk";

            var cliente = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(remitente, password),
                EnableSsl = true
            };

            var mensaje = new MailMessage(remitente, destino)
            {
                Subject = "Código de verificación",
                Body = $"Tu código es: {codigo}"
            };

            cliente.Send(mensaje);
        }
    }
}
