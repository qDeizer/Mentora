using PsikologProje_Void.Models;
using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Services
{
    public interface IAppointmentRequestService
    {
        Task<ServiceResult> CreateAppointmentRequestAsync(AppointmentRequest request);
        Task<RequestsViewModel> GetRequestsAsync(AppointmentRequestFilterModel filter);
        Task<ServiceResult> ApproveRequestAsync(int requestId, string doctorId, string? responseMessage = null, string? autoRejectMessage = null);
        Task<ServiceResult> RejectRequestAsync(int requestId, string responseMessage, string doctorId);
        Task<ServiceResult> CancelRequestAsync(int requestId, string patientId);
    }
}
