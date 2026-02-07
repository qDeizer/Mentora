using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PsikologProje_Void.Services
{
    public class AppointmentRequestService : IAppointmentRequestService
    {
        private readonly ApplicationDbContext _context;
        public AppointmentRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RequestsViewModel> GetRequestsAsync(AppointmentRequestFilterModel filter)
        {
            var query = _context.AppointmentRequests
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .Include(r => r.Appointment)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.DoctorId))
            {
                query = query.Where(r => r.DoctorId == filter.DoctorId);
            }
            if (!string.IsNullOrEmpty(filter.PatientId))
            {
                query = query.Where(r => r.PatientId == filter.PatientId);
            }
            if (filter.AppointmentId.HasValue)
            {
                query = query.Where(r => r.AppointmentId == filter.AppointmentId.Value);
            }
            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }

            // HATA DÜZELTİLDİ: Olmayan bir özelliğe göre filtreleme yapan bu blok kaldırıldı.
            // if (!string.IsNullOrEmpty(filter.PatientName))
            // {
            //     query = query.Where(r => (r.Patient.FirstName + " " + r.Patient.LastName).Contains(filter.PatientName));
            // }

            var results = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new AppointmentRequestViewModel
                {
                    RequestId = r.Id,
                    AppointmentId = r.AppointmentId,
                    PatientName = r.Patient.FirstName + " " + r.Patient.LastName,
                    PatientProfilePhoto = r.Patient.ProfilePhotoPath,
                    DoctorName = r.Doctor.FirstName + " " + r.Doctor.LastName,
                    DoctorProfilePhoto = r.Doctor.ProfilePhotoPath,
                    AppointmentStartTime = r.Appointment.StartTime,
                    RequestDate = r.CreatedAt,
                    RequestMessage = r.RequestMessage,
                    ResponseMessage = r.ResponseMessage,
                    Status = r.Status
                }).ToListAsync();

            return new RequestsViewModel { Requests = results, Filter = filter };
        }

        public async Task<bool> ApproveRequestAsync(int requestId, string doctorId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var requestToApprove = await _context.AppointmentRequests
                    .Include(r => r.Appointment)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (requestToApprove == null || requestToApprove.DoctorId != doctorId || requestToApprove.Status != RequestStatus.Pending)
                    return false;

                var appointment = requestToApprove.Appointment;
                if (appointment.Status != AppointmentStatus.Available)
                    return false; // Randevu artık müsait değil

                appointment.PatientId = requestToApprove.PatientId;
                appointment.Status = AppointmentStatus.Reserved;

                requestToApprove.Status = RequestStatus.Approved;
                requestToApprove.ResponseMessage = "Talebiniz onaylanmıştır.";

                var otherPendingRequests = await _context.AppointmentRequests
                    .Where(r => r.AppointmentId == appointment.Id && r.Status == RequestStatus.Pending && r.Id != requestId)
                    .ToListAsync();

                foreach (var req in otherPendingRequests)
                {
                    req.Status = RequestStatus.Rejected;
                    req.ResponseMessage = "Randevu başka bir hastaya verildiği için talebiniz iptal edilmiştir.";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> RejectRequestAsync(int requestId, string responseMessage, string doctorId)
        {
            var requestToReject = await _context.AppointmentRequests.FirstOrDefaultAsync(r => r.Id == requestId);

            if (requestToReject == null || requestToReject.DoctorId != doctorId || requestToReject.Status != RequestStatus.Pending)
                return false;

            requestToReject.Status = RequestStatus.Rejected;
            requestToReject.ResponseMessage = responseMessage;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelRequestAsync(int requestId, string patientId)
        {
            var requestToCancel = await _context.AppointmentRequests.FirstOrDefaultAsync(r => r.Id == requestId);

            if (requestToCancel == null || requestToCancel.PatientId != patientId || requestToCancel.Status != RequestStatus.Pending)
                return false;

            _context.AppointmentRequests.Remove(requestToCancel);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateAppointmentRequestAsync(AppointmentRequest request)
        {
            _context.AppointmentRequests.Add(request);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}