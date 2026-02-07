using System.Collections.Generic;

namespace PsikologProje_Void.ViewModels
{
    public class RequestsViewModel
    {
        public IEnumerable<AppointmentRequestViewModel> Requests { get; set; } = new List<AppointmentRequestViewModel>();
        public AppointmentRequestFilterModel Filter { get; set; } = new AppointmentRequestFilterModel();
    }
}