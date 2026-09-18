import { HttpErrorResponse } from '@angular/common/http';
import { extractErrorMessage } from './api-error';

describe('extractErrorMessage', () => {
  it('returns the ProblemDetails `detail` field when present', () => {
    const error = new HttpErrorResponse({ error: { detail: 'Эта площадка уже добавлена в систему.' } });

    expect(extractErrorMessage(error, 'fallback')).toBe('Эта площадка уже добавлена в систему.');
  });

  it('falls back when the HttpErrorResponse body has no `detail` field', () => {
    const error = new HttpErrorResponse({ error: { title: 'Something else entirely' } });

    expect(extractErrorMessage(error, 'fallback')).toBe('fallback');
  });

  it('falls back when `detail` is present but not a string', () => {
    const error = new HttpErrorResponse({ error: { detail: 42 } });

    expect(extractErrorMessage(error, 'fallback')).toBe('fallback');
  });

  it('falls back for a non-HttpErrorResponse error (e.g. a thrown JS Error)', () => {
    expect(extractErrorMessage(new Error('boom'), 'fallback')).toBe('fallback');
  });

  it('falls back for null/undefined', () => {
    expect(extractErrorMessage(null, 'fallback')).toBe('fallback');
    expect(extractErrorMessage(undefined, 'fallback')).toBe('fallback');
  });
});
