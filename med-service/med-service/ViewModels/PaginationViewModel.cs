// ViewModels/PaginationViewModel.cs
namespace med_service.ViewModels
{
    public class PaginationViewModel
    {
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentFilter { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
    }
}
