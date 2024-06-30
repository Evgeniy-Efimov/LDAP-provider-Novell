namespace LDAP_Connector.Helpers
{
    public static class ExceptionHelper
    {
        public static string GetFullErrorMessage(this Exception ex, string delimiter = "; ")
        {
            var errorMessage = ex.Message;
            var innerException = ex.InnerException;

            while (!string.IsNullOrWhiteSpace(innerException?.Message))
            {
                errorMessage += $"{delimiter}{innerException.Message}";
                innerException = innerException.InnerException;
            }

            return errorMessage;
        }
    }
}
