using Publisher.Application;

namespace Publisher.Infrastructure.Repositories;

public class FileRepository : IFileRepository
{
    private readonly string _storagePath;

    public FileRepository(string storagePath)
    {
        _storagePath = storagePath;

        // Crear la carpeta si no existe
        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        // 1. Generar un nombre único para evitar colisiones
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        // 2. Crear el path completo
        var filePath = Path.Combine(_storagePath, uniqueFileName);

        // 3. Copiar el stream al disco de forma asíncrona
        using var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);

        // 4. Retornar la ruta del archivo guardado
        return filePath;
    }
}
