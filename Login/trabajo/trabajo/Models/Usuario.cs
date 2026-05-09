namespace trabajo.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = null!;

        public string Apellido { get; set; } = null!;

        public string Dni { get; set; } = null!;

        public string Celular { get; set; } = null!;


        public string Correo { get; set; } = null!;

        public string clave { get; set; } = null!;
    }
}
