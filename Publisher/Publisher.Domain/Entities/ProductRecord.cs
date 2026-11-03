namespace Publisher.Domain.Entities;

public class ProductRecord
{
    public Guid Id { get; set; }
    public Guid ExcelFileId { get; set; }
    public DateOnly Periodo { get; set; }
    public string CodigoProducto { get; set; }
    public string NombreProducto { get; set; }
    public Decimal Precio { get; set; }
}
