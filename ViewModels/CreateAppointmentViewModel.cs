// FILE: \ViewModels\CreateAppointmentViewModel.cs

using System.ComponentModel.DataAnnotations;
using PsikologProje_Void.Tools;
using System.Collections.Generic;

namespace PsikologProje_Void.ViewModels
{
    [AtLeastOneRequired("IsOnline", "IsInPerson", ErrorMessage = "En az bir randevu türü (Online/Yüz Yüze) seçilmelidir.")]
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

        [Display(Name = "Online")]
        public bool IsOnline { get; set; }

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
    }
}
