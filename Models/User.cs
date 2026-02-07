using Microsoft.AspNetCore.Identity;
using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
namespace PsikologProje_Void.Models
{
    public enum UserType { Admin, Doctor, Patient }
    public enum Gender { Male, Female, Other }

    public class User : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = default!; // Hata CS8618 düzeltildi

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = default!; // Hata CS8618 düzeltildi

        public UserType UserType { get; set; }

        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        public Gender Gender { get; set; }

        public Point? Location { get; set; }

        [StringLength(1000)]
        public string? About { get; set; }

        public string? ProfilePhotoPath { get; set; }

        public bool IsApproved { get; set; }
    }
}