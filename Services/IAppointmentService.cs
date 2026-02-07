using PsikologProje_Void.ViewModels;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace PsikologProje_Void.Services
{
    public interface IAppointmentService
    {
        Task<bool> CreateAppointmentAsync(CreateAppointmentViewModel model, ClaimsPrincipal user);
        Task<IEnumerable<AppointmentViewModel>> GetAppointmentsAsync(AppointmentFilterModel filter, ClaimsPrincipal requester);
        Task<bool> DeleteAppointmentAsync(int appointmentId, string doctorId);
        Task UpdateExpiredAppointmentsAsync();
    }
}