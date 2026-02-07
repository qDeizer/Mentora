// FILE: \Tools\AtLeastOneRequiredAttribute.cs

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace PsikologProje_Void.Tools
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AtLeastOneRequiredAttribute : ValidationAttribute
    {
        private string[] PropertyNames { get; }

        public AtLeastOneRequiredAttribute(params string[] propertyNames)
        {
            PropertyNames = propertyNames;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (PropertyNames == null || PropertyNames.Length == 0)
            {
                return ValidationResult.Success;
            }

            foreach (var propertyName in PropertyNames)
            {
                var property = validationContext.ObjectType.GetProperty(propertyName);
                if (property == null)
                {
                    return new ValidationResult($"Bilinmeyen özellik: {propertyName}");
                }

                var propertyValue = property.GetValue(validationContext.ObjectInstance);
                if (propertyValue is bool boolValue && boolValue)
                {
                    return ValidationResult.Success; // En az bir tane 'true' bulundu, doğrulama başarılı.
                }
            }

            return new ValidationResult(ErrorMessage ?? "En az bir seçenek işaretlenmelidir.");
        }
    }
}