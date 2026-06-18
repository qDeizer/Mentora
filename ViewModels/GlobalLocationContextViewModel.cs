using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class GlobalLocationContextViewModel
    {
        public GlobalLocationSource Source { get; set; } = GlobalLocationSource.Profile;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Label { get; set; } = "Profil konumu";
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    }

    public class GlobalLocationContextUpdateViewModel
    {
        public GlobalLocationSource Source { get; set; } = GlobalLocationSource.Profile;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Label { get; set; }
    }
}
