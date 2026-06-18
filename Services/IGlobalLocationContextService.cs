using PsikologProje_Void.Models;
using PsikologProje_Void.ViewModels;
using System.Security.Claims;

namespace PsikologProje_Void.Services
{
    public interface IGlobalLocationContextService
    {
        Task<GlobalLocationContextViewModel> GetContextAsync(ClaimsPrincipal principal);
        Task<GlobalLocationContextViewModel> UpdateContextAsync(ClaimsPrincipal principal, GlobalLocationContextUpdateViewModel input);
        Task<GlobalLocationContextViewModel> SaveContextToProfileAsync(ClaimsPrincipal principal);
    }
}
