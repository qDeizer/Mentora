// FILE: \ViewModels\CreateAppointmentViewModel.cs

using System.ComponentModel.DataAnnotations;
using PsikologProje_Void.Tools;
using System.Collections.Generic;
using System.Globalization;

namespace PsikologProje_Void.ViewModels
{
[AtLeastOneRequired("IsOnline", "IsInPerson", ErrorMessage = "En az bir randevu türü (Çevrim içi/Yüz yüze) seçilmelidir.")]
    public class CreateAppointmentViewModel
    {
        [Required(ErrorMessage = "Başlangıç tarihi ve saati zorunludur.")]
        [Display(Name = "Randevu Başlangıcı")]
        [FutureDate(ErrorMessage = "Geçmiş tarihli randevu oluşturamazsınız.")]
        [DataType(DataType.DateTime)]
        public DateTime? StartTime { get; set; } // Nullable yapıldı ve varsayılan değer kaldırıldı.

        [Display(Name = "Randevu Bitişi")]
        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        [Display(Name = "Randevu Süresi (Dakika)")]
        [Range(1, int.MaxValue, ErrorMessage = "Süre en az 1 dakika olmalıdır.")]
        public int? DurationInMinutes { get; set; }

        [Display(Name = "Çevrim içi")]
        public bool IsOnline { get; set; }

        [Display(Name = "Çevrim İçi Görüşme Linki")]
        [StringLength(300)]
        [Url(ErrorMessage = "Çevrim içi görüşme linki geçerli bir bağlantı olmalıdır.")]
        public string? MeetingLink { get; set; }

        [Display(Name = "Yüz Yüze")]
        public bool IsInPerson { get; set; }

        [Display(Name = "Minimum Fiyat")]
        [DataType(DataType.Currency)]
        public decimal? MinPrice { get; set; }

        [Display(Name = "Maksimum Fiyat")]
        [DataType(DataType.Currency)]
        public decimal? MaxPrice { get; set; }

        [Display(Name = "Randevu Uzmanlıkları")]
        public List<int>? SelectedSpecialtyIds { get; set; }

        [Display(Name = "Randevu Notu")]
        [StringLength(500)]
        public string? Notes { get; set; }

        [Display(Name = "Özel randevu teklifi")]
        public bool IsPrivateOffer { get; set; }

        [Display(Name = "Hedef hasta")]
        public string? TargetPatientId { get; set; }

        [Display(Name = "Doktor notu (teklif mesajı)")]
        [StringLength(500)]
        public string? OfferNoteFromDoctor { get; set; }

        [Display(Name = "Konum Kaynağı")]
        [StringLength(20)]
        public string InPersonLocationMode { get; set; } = "profile";

        [Display(Name = "Seçilen Enlem")]
        public string? InPersonLatitude { get; set; }

        [Display(Name = "Seçilen Boylam")]
        public string? InPersonLongitude { get; set; }

        [Display(Name = "Açık Adres / Konum Notu")]
        [StringLength(300)]
        public string? LocationNote { get; set; }

        public bool TryGetInPersonCoordinates(out double latitude, out double longitude)
        {
            latitude = 0;
            longitude = 0;
            if (string.IsNullOrWhiteSpace(InPersonLatitude) || string.IsNullOrWhiteSpace(InPersonLongitude))
            {
                return false;
            }

            return double.TryParse(InPersonLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out latitude) &&
                   double.TryParse(InPersonLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out longitude);
        }
    }
}
