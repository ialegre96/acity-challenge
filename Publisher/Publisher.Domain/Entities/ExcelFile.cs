using Publisher.Domain.Enums;

namespace Publisher.Domain.Entities;

public class ExcelFile
{
    public Guid Id { get; set; }
    public string? FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; } = string.Empty;
    public FileStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
