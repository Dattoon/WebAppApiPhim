namespace WebAppApiPhim.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public object RouteValues { get; set; }
    }
}
