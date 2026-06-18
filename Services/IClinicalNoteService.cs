using PsikologProje_Void.Models;
using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Services
{
    public interface IClinicalNoteService
    {
        Task<ClinicalNotesDoctorDashboardViewModel> GetDoctorDashboardAsync(
            string doctorId,
            List<string>? patientIds = null,
            string? query = null,
            string? sortBy = null,
            string? sortDirection = null);
        Task<ServiceResult> CreateNoteAsync(string doctorId, ClinicalNoteCreateViewModel model);
        Task<ServiceResult> UpdateNoteAsync(string doctorId, int noteId, string content);
        Task<ServiceResult> ToggleLockAsync(string doctorId, int noteId, bool isLockedForPatient);
        Task<ClinicalNotesPatientDashboardViewModel> GetPatientDashboardAsync(string patientId, ClinicalNotesMyNotesFilterViewModel? filter = null);
        Task<ServiceResult> ShareNotesAsync(string patientId, string targetDoctorId, List<int> noteIds);
        Task<ServiceResult> RevokeShareAsync(string patientId, string targetDoctorId, List<int> noteIds);
        Task<ServiceResult> UpdateNoteVisibilityAsync(string patientId, int noteId, ClinicalNoteVisibility visibility);
        Task<ServiceResult> BulkUpdateVisibilityAsync(string patientId, List<int> noteIds, ClinicalNoteVisibility visibility);
        Task<ServiceResult> UpdateAccessRuleAsync(string patientId, ClinicalNoteAccessRuleCommandViewModel model);
        Task<ServiceResult> ApplyBulkActionsAsync(string patientId, ClinicalNoteBulkActionInputViewModel model);
        Task<ServiceResult> AddCommentAsync(string actorUserId, bool actorIsDoctor, int noteId, string content);
        Task<ServiceResult> UpdateCommentAsync(string actorUserId, bool actorIsDoctor, int commentId, string content);
        Task<ServiceResult> DeleteCommentAsync(string actorUserId, bool actorIsDoctor, int commentId);
        Task<ServiceResult> ToggleCommentLockAsync(string doctorId, int commentId, bool isLockedForPatient);
    }
}
