using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;

namespace PsikologProje_Void.Controllers
{
    [Authorize]
    public class PeopleController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IPeopleService _peopleService;

        public PeopleController(UserManager<User> userManager, IPeopleService peopleService)
        {
            _userManager = userManager;
            _peopleService = peopleService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var viewerUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(viewerUserId))
            {
                return Unauthorized();
            }

            var model = await _peopleService.GetPeopleIndexAsync(
                viewerUserId,
                viewerIsDoctor: User.IsInRole("Doctor"),
                searchTerm: search);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profile(string id)
        {
            var viewerUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(viewerUserId))
            {
                return Unauthorized();
            }

            var model = await _peopleService.GetPersonProfileAsync(
                viewerUserId,
                viewerIsDoctor: User.IsInRole("Doctor"),
                targetUserId: id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Bu profili görüntüleme yetkiniz yok veya ilişki süresi doldu.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Disconnect(string targetUserId)
        {
            var viewerUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(viewerUserId))
            {
                return Unauthorized();
            }

            var result = await _peopleService.DisconnectAsync(
                viewerUserId,
                actorIsDoctor: User.IsInRole("Doctor"),
                targetUserId: targetUserId);

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded
                    ? "İlişki kesildi. Profiller ve hızlı aksiyon yetkileri kaldırıldı."
                    : result.ErrorMessage ?? "İlişki kesme işlemi başarısız.";

            return RedirectToAction(nameof(Index));
        }
    }
}
