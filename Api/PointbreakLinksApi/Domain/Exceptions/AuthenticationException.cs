namespace Domain.Exceptions;

public class AuthenticationException(string message)
    : Exception(message);
