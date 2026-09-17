namespace Domain.Exceptions;

// "This already exists" — e.g. a Site URL that's already listed. FOXLinks surfaced this as a
// Laravel validation error ('url.unique' => 'Эта площадка уже добавлена в систему.'); we check
// for it explicitly before insert (same pattern Auth's RegisterCommandHandler already uses for
// duplicate emails) rather than catching the resulting DbUpdateException after the fact.
public class ConflictException(string message) : Exception(message);
