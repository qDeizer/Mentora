using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Services
{
    public interface IPeopleService
    {
        Task<PeopleIndexViewModel> GetPeopleIndexAsync(string viewerUserId, bool viewerIsDoctor, string? searchTerm);
        Task<PersonProfileViewModel?> GetPersonProfileAsync(string viewerUserId, bool viewerIsDoctor, string targetUserId);
        Task<ServiceResult> DisconnectAsync(string actorUserId, bool actorIsDoctor, string targetUserId);
    }
}
