namespace PsikologProje_Void.ViewModels
{
    public class RequestsViewModel
    {
        public IEnumerable<AppointmentRequestViewModel> Requests { get; set; } = new List<AppointmentRequestViewModel>();
        public AppointmentRequestFilterModel Filter { get; set; } = new AppointmentRequestFilterModel();
        public List<RequestPartyOptionViewModel> PatientOptions { get; set; } = new();
        public List<RequestPartyOptionViewModel> DoctorOptions { get; set; } = new();
    }
}
