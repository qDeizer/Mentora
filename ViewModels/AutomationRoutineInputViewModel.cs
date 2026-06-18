using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace PsikologProje_Void.ViewModels
{
    public class AutomationRoutineInputViewModel : IValidatableObject
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Rutin adı zorunludur.")]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Başlangıç saati zorunludur.")]
        [DataType(DataType.Time)]
        public TimeOnly StartTime { get; set; } = new(9, 0);

        [Range(15, 720, ErrorMessage = "Süre 15 ile 720 dakika arasında olmalıdır.")]
        public int DurationInMinutes { get; set; } = 50;

        [Range(1, 90, ErrorMessage = "Randevu oluşturma ufku 1 ile 90 gün arasında olmalıdır.")]
        public int GenerateDaysAhead { get; set; } = 7;

        [DataType(DataType.Date)]
        public DateTime ActiveFrom { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime? ActiveUntil { get; set; }

        public List<int> SelectedDays { get; set; } = new();

        public bool IsOnline { get; set; } = true;
        public bool IsInPerson { get; set; }

        [DataType(DataType.Currency)]
        public decimal? MinPrice { get; set; }

        [DataType(DataType.Currency)]
        public decimal? MaxPrice { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Konum kaynağı seçmelisiniz.")]
        [StringLength(20)]
        public string InPersonLocationMode { get; set; } = "profile";

        public string? InPersonLatitude { get; set; }
        public string? InPersonLongitude { get; set; }

        [StringLength(300)]
        public string? LocationNote { get; set; }

        public List<int> SelectedSpecialtyIds { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var selectedDays = SelectedDays ?? new List<int>();
            if (selectedDays.Count == 0)
            {
                yield return new ValidationResult("En az bir gün seçmelisiniz.", new[] { nameof(SelectedDays) });
            }

            if (!IsOnline && !IsInPerson)
            {
                yield return new ValidationResult("En az bir randevu türü seçmelisiniz.", new[] { nameof(IsOnline), nameof(IsInPerson) });
            }

            if (MinPrice.HasValue && MaxPrice.HasValue && MaxPrice < MinPrice)
            {
                yield return new ValidationResult("Maksimum fiyat minimum fiyattan küçük olamaz.", new[] { nameof(MinPrice), nameof(MaxPrice) });
            }

            if (ActiveUntil.HasValue && ActiveUntil.Value.Date < ActiveFrom.Date)
            {
                yield return new ValidationResult("Bitiş tarihi başlangıç tarihinden önce olamaz.", new[] { nameof(ActiveFrom), nameof(ActiveUntil) });
            }

            if (IsInPerson)
            {
                var mode = (InPersonLocationMode ?? string.Empty).Trim().ToLowerInvariant();
                if (mode != "profile" && mode != "device" && mode != "manual")
                {
                    yield return new ValidationResult("Konum kaynağı geçersiz.", new[] { nameof(InPersonLocationMode) });
                }

                if (mode != "profile")
                {
                    var hasValidCoordinates = !string.IsNullOrWhiteSpace(InPersonLatitude) &&
                                              !string.IsNullOrWhiteSpace(InPersonLongitude) &&
                                              double.TryParse(InPersonLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                                              double.TryParse(InPersonLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out _);

                    if (!hasValidCoordinates)
                    {
                        yield return new ValidationResult("Seçilen konum kaynağı için geçerli enlem/boylam girilmelidir.", new[] { nameof(InPersonLatitude), nameof(InPersonLongitude) });
                    }
                }
            }
        }
    }
}
