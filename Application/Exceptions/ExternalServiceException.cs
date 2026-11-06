namespace Application.Exceptions;

/// <summary>An external service failed; the message is safe to show to the user.</summary>
public class ExternalServiceException(string? message, Exception? innerException = null)
    : Exception(message, innerException);
