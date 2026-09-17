namespace Identity.Domain.Exceptions;

// A subclass of AuthenticationException (not a standalone type) purely so AuthController's
// existing `catch (AuthenticationException ex)` block around /login already handles it — same
// 401 + ProblemDetails shape the client's extractErrorMessage() already reads `detail` from, no
// controller change needed for a new failure mode.
public class AccountLockedException(string message) : AuthenticationException(message);
