namespace NotificationService.Application.Messages;

public class NotificacionMessage
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime FechaFin { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosExitosos { get; set; }
    public int RegistrosFallidos { get; set; }
}
