using PsikologProje_Void.Models;
using PsikologProje_Void.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PsikologProje_Void.Services
{
    public interface IAppointmentRequestService
    {
        Task<bool> CreateAppointmentRequestAsync(AppointmentRequest request);
        Task<RequestsViewModel> GetRequestsAsync(AppointmentRequestFilterModel filter);
        Task<bool> ApproveRequestAsync(int requestId, string doctorId);
        Task<bool> RejectRequestAsync(int requestId, string responseMessage, string doctorId);
        Task<bool> CancelRequestAsync(int requestId, string patientId);
    }
}