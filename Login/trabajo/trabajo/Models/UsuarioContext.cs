using Microsoft.EntityFrameworkCore;

namespace trabajo.Models
{
    public class UsuarioContext :DbContext
    {
        public UsuarioContext(DbContextOptions<UsuarioContext> options) : base(options)
        {

        }
        public DbSet<Usuario> Usuario { get; set; }
    }
}
