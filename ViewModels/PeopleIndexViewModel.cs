namespace PsikologProje_Void.ViewModels
{
    public class PeopleIndexViewModel
    {
        public bool IsDoctorView { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public List<PersonCardViewModel> People { get; set; } = new();
    }
}
