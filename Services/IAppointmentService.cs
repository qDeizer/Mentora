using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;
using System.Security.Claims;

namespace PsikologProje_Void.Services
{
    public interface IAppointmentService
    {
        Task<ServiceResult> CreateAppointmentAsync(CreateAppointmentViewModel model, ClaimsPrincipal user);
        Task<IEnumerable<AppointmentViewModel>> GetAppointmentsAsync(AppointmentFilterModel filter, ClaimsPrincipal requester);
        Task<bool> DeleteAppointmentAsync(int appointmentId, string doctorId);
        Task UpdateExpiredAppointmentsAsync();
        Task<bool> HasScheduleConflictForPatientAsync(string patientId, DateTime startTime, DateTime endTime, int? ignoredAppointmentId = null);
        Task<IEnumerable<AppointmentViewModel>> GetPrivateOffersForPatientAsync(string patientId);
        Task<int> CountPendingPrivateOffersAsync(string patientId);
        Task<ServiceResult> RespondPrivateOfferAsync(int appointmentId, string patientId, bool accept, string? responseMessage);
        Task<int> ExpirePendingPrivateOffersAsync(CancellationToken cancellationToken = default);
    }
}
