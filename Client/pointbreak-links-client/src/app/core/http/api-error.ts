import { HttpErrorResponse } from '@angular/common/http';

// The API's DomainExceptionHandler (WebAPI/Middleware/DomainExceptionHandler.cs) returns a
// ProblemDetails body with a `detail` field carrying a user-facing message (e.g. "Эта площадка
// уже добавлена в систему.") for expected failures like a duplicate URL — surface that instead
// of a generic fallback whenever it's present.
export function extractErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') {
    return error.error.detail;
  }
  return fallback;
}
