// FILE: \Services\IUserService.cs

using PsikologProje_Void.ViewModels;
using System.Security.Claims;

namespace PsikologProje_Void.Services
{
    public interface IUserService
    {
        Task<IEnumerable<DoctorViewModel>> GetDoctorsAsync(DoctorFilterModel filter, ClaimsPrincipal requester);
    }
}