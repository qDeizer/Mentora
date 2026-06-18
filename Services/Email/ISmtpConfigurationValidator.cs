namespace PsikologProje_Void.Services.Email
{
    public interface ISmtpConfigurationValidator
    {
        (bool IsValid, string? ErrorMessage) Validate();
    }
}
