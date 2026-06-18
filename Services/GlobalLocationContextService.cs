using Microsoft.AspNetCore.Identity;
using NetTopologySuite.Geometries;
using PsikologProje_Void.Models;
using PsikologProje_Void.ViewModels;
using System.Text.Json;
using System.Security.Claims;

namespace PsikologProje_Void.Services
{
    public class GlobalLocationContextService : IGlobalLocationContextService
    {
        private const string SessionKey = "mentora.global-location-context";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<User> _userManager;
        private readonly JsonSerializerOptions _serializerOptions;

        public GlobalLocationContextService(IHttpContextAccessor httpContextAccessor, UserManager<User> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public async Task<GlobalLocationContextViewModel> GetContextAsync(ClaimsPrincipal principal)
        {
            var context = await ReadFromSessionAsync();
            if (context != null && context.HasCoordinates)
            {
                return context;
            }

            return await BuildProfileDefaultAsync(principal);
        }

        public async Task<GlobalLocationContextViewModel> UpdateContextAsync(ClaimsPrincipal principal, GlobalLocationContextUpdateViewModel input)
        {
            var context = input.Source switch
            {
                GlobalLocationSource.Profile => await BuildProfileDefaultAsync(principal),
                GlobalLocationSource.DeviceGps => BuildManualLikeContext(input, GlobalLocationSource.DeviceGps, "Cihaz konumu"),
                GlobalLocationSource.ManualMap => BuildManualLikeContext(input, GlobalLocationSource.ManualMap, "Manuel konum"),
                _ => await BuildProfileDefaultAsync(principal)
            };

            await WriteToSessionAsync(context);
            return context;
        }

        public async Task<GlobalLocationContextViewModel> SaveContextToProfileAsync(ClaimsPrincipal principal)
        {
            var user = await _userManager.GetUserAsync(principal);
            if (user == null)
            {
                return new GlobalLocationContextViewModel();
            }

            var context = await GetContextAsync(principal);
            if (context.HasCoordinates)
            {
                user.Location = new Point(context.Longitude!.Value, context.Latitude!.Value)
                {
                    SRID = 4326
                };
                await _userManager.UpdateAsync(user);
            }

            return await BuildProfileDefaultAsync(principal);
        }

        private Task<GlobalLocationContextViewModel?> ReadFromSessionAsync()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return Task.FromResult<GlobalLocationContextViewModel?>(null);
            }

            var raw = session.GetString(SessionKey);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return Task.FromResult<GlobalLocationContextViewModel?>(null);
            }

            try
            {
                var model = JsonSerializer.Deserialize<GlobalLocationContextViewModel>(raw, _serializerOptions);
                return Task.FromResult(model);
            }
            catch
            {
                session.Remove(SessionKey);
                return Task.FromResult<GlobalLocationContextViewModel?>(null);
            }
        }

        private Task WriteToSessionAsync(GlobalLocationContextViewModel model)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return Task.CompletedTask;
            }

            var payload = JsonSerializer.Serialize(model, _serializerOptions);
            session.SetString(SessionKey, payload);
            return Task.CompletedTask;
        }

        private async Task<GlobalLocationContextViewModel> BuildProfileDefaultAsync(ClaimsPrincipal principal)
        {
            var user = await _userManager.GetUserAsync(principal);
            var model = new GlobalLocationContextViewModel
            {
                Source = GlobalLocationSource.Profile,
                UpdatedAtUtc = DateTime.UtcNow,
                Label = "Profil konumu"
            };

            if (user?.Location != null)
            {
                model.Latitude = Math.Round(user.Location.Y, 6);
                model.Longitude = Math.Round(user.Location.X, 6);
                model.Label = "Profil";
            }
            else
            {
                model.Label = "Profil konumu tanimsiz";
            }

            return model;
        }

        private static GlobalLocationContextViewModel BuildManualLikeContext(GlobalLocationContextUpdateViewModel input, GlobalLocationSource source, string defaultLabel)
        {
            return new GlobalLocationContextViewModel
            {
                Source = source,
                Latitude = input.Latitude.HasValue ? Math.Round(input.Latitude.Value, 6) : null,
                Longitude = input.Longitude.HasValue ? Math.Round(input.Longitude.Value, 6) : null,
                Label = string.IsNullOrWhiteSpace(input.Label)
                    ? defaultLabel
                    : input.Label.Trim(),
                UpdatedAtUtc = DateTime.UtcNow
            };
        }
    }
}
