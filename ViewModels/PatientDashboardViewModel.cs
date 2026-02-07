using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace PsikologProje_Void.ViewModels
{
    public class PatientDashboardViewModel
    {
        // Hata CS8618 düzeltildi: Koleksiyonlar ve nesneler için varsayılan değerler atandı.
        public IEnumerable<AppointmentViewModel> Appointments { get; set; } = new List<AppointmentViewModel>();

        public AppointmentFilterModel Filter { get; set; } = new AppointmentFilterModel();

        public SelectList Doctors { get; set; } = new SelectList(new List<object>());

        public SelectList Specialties { get; set; } = new SelectList(new List<object>());
    }
}
