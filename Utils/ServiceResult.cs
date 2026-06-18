namespace PsikologProje_Void.Utils
{
    public record ServiceResult(bool Succeeded, string? ErrorMessage = null, string? SuccessMessage = null)
    {
        public static ServiceResult Success(string? successMessage = null) => new(true, null, successMessage);

        public static ServiceResult Failure(string errorMessage) => new(false, errorMessage);
    }
}
