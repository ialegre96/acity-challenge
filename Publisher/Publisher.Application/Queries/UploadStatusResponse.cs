using Publisher.Domain.Enums;

namespace Publisher.Application.Queries;

public class UploadStatusResponse
{
    public Guid FileId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public FileStatus Status { get; set; } // Usar el enum en lugar de int
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Message { get; set; } // Para errores o detalles
}
