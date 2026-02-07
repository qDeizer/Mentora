// FILE: \Tools\FutureDateAttribute.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace PsikologProje_Void.Tools
{
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            // Eğer değer null ise, bu doğrulama adımını geç. 
            // [Required] attribute'ü null olup olmadığını kontrol edecektir.
            if (value == null)
            {
                return true;
            }

            if (value is DateTime dateTime)
            {
                return dateTime > DateTime.Now;
            }
            return false;
        }
    }
}
