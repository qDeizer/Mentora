namespace PsikologProje_Void.ViewModels
{
    public class PatientDashboardViewModel
    {
        public IEnumerable<AppointmentViewModel> Appointments { get; set; } = new List<AppointmentViewModel>();
        public AppointmentFilterModel Filter { get; set; } = new AppointmentFilterModel();
        public List<DoctorFilterOptionViewModel> Doctors { get; set; } = new();
        public List<SpecialtyFilterOptionViewModel> Specialties { get; set; } = new();
        public double? ProfileLatitude { get; set; }
        public double? ProfileLongitude { get; set; }
        public string GlobalLocationLabel { get; set; } = "Konum secilmedi";
        public string GlobalLocationSource { get; set; } = "Profile";
        public double? GlobalLatitude { get; set; }
        public double? GlobalLongitude { get; set; }
    }
}
