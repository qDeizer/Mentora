using Microsoft.AspNetCore.Identity;
using PsikologProje_Void.Models;
using System.ComponentModel.DataAnnotations;
namespace PsikologProje_Void.Models
{
    public class Patient : User
    {
        // Patient sınıfı User'dan inherit ediyor, ek özellik yok
        // Gelecekte hasta-specific özellikler eklenebilir
    }
}