namespace PsikologProje_Void.Models
{
    public enum GlobalLocationSource
    {
        Profile = 0,
        DeviceGps = 1,
        ManualMap = 2
    }

    public class GlobalLocationContext
    {
        public GlobalLocationSource Source { get; set; } = GlobalLocationSource.Profile;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Label { get; set; }
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    }
}
