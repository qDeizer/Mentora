using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Controllers
{
    [Authorize]
    [Route("Location")]
    public class LocationController : Controller
    {
        private readonly IGlobalLocationContextService _globalLocationContextService;

        public LocationController(IGlobalLocationContextService globalLocationContextService)
        {
            _globalLocationContextService = globalLocationContextService;
        }

        [HttpGet("Context")]
        public async Task<IActionResult> GetContext()
        {
            var model = await _globalLocationContextService.GetContextAsync(User);
            return Json(model);
        }

        [HttpPost("Context")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> SetContext([FromBody] GlobalLocationContextUpdateViewModel model)
        {
            var updated = await _globalLocationContextService.UpdateContextAsync(User, model);
            return Json(updated);
        }

        [HttpPost("Context/SaveProfile")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> SaveContextToProfile()
        {
            var updated = await _globalLocationContextService.SaveContextToProfileAsync(User);
            return Json(updated);
        }
    }
}
