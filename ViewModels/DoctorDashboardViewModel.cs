namespace PsikologProje_Void.ViewModels
{
    public class DoctorDashboardViewModel
    {
        public IEnumerable<AppointmentViewModel> Appointments { get; set; } = new List<AppointmentViewModel>();
        public CreateAppointmentViewModel CreateAppointment { get; set; } = new CreateAppointmentViewModel();
    }
}