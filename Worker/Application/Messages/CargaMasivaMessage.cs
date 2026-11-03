namespace Worker.Application.Messages;

public class CargaMasivaMessage
{
    public Guid FileId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
