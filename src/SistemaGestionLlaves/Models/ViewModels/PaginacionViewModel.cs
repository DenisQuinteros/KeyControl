namespace SistemaGestionLlaves.Models.ViewModels
{
    public class PaginacionViewModel<T> where T : class
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public string? TerminoBusqueda { get; set; }
        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
    }
}
