namespace Application.CQRS.Messages.DTOs;

// RelativePath is the on-disk name under wwwroot/uploads/messages — WebAPI resolves it to a
// full path (it's the only layer that knows where wwwroot actually lives) and streams the file.
public record MessageAttachmentDto(string RelativePath, string FileName, string ContentType);
