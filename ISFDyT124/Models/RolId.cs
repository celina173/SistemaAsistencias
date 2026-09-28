namespace ISFDyT124.Models
{
    // Ids fijos de Rol, sembrados al arrancar la app (ver el seed en Program.cs) y con
    // ValueGeneratedNever en el DbContext, así que nunca cambian. Reemplaza los números
    // mágicos (1/2/3/4) que estaban repetidos a mano en varios controllers.
    public static class RolId
    {
        public const int Admin = 1;
        public const int Docente = 2;
        public const int Estudiante = 3;
        public const int Direccion = 4;
    }
}
