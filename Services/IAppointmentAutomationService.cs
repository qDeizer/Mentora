using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Services
{
    public interface IAppointmentAutomationService
    {
        Task<List<AutomationRoutineListItemViewModel>> GetDoctorRoutinesAsync(string doctorId);
        Task<AutomationRoutineInputViewModel?> GetRoutineForEditAsync(string doctorId, int routineId);
        Task<ServiceResult> CreateRoutineAsync(string doctorId, AutomationRoutineInputViewModel model);
        Task<ServiceResult> UpdateRoutineAsync(string doctorId, AutomationRoutineInputViewModel model);
        Task<ServiceResult> PauseRoutineAsync(string doctorId, int routineId, int? pauseDays, DateTime? pauseUntilLocal);
        Task<ServiceResult> ResumeRoutineAsync(string doctorId, int routineId);
        Task<ServiceResult> DeleteRoutineAsync(string doctorId, int routineId);
        Task<int> GenerateAppointmentsAsync(CancellationToken cancellationToken = default);
        Task<int> GenerateAppointmentsForRoutineAsync(int routineId, CancellationToken cancellationToken = default);
    }
}
