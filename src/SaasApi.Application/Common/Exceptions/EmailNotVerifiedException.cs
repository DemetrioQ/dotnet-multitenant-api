namespace SaasApi.Application.Common.Exceptions;

public class EmailNotVerifiedException(DateTime canResendAt)
    : Exception("Email address has not been verified.")
{
    public DateTime CanResendAt { get; } = canResendAt;
}
