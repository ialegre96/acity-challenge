using MediatR;
using Microsoft.AspNetCore.Mvc;
using Publisher.Application.Queries;
using Publisher.Application.Commands;

namespace Publisher.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExcelController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExcelController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadExcel(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No se ha enviado ningún archivo o el archivo está vacío.");
        }

        var command = new UploadExcelCommand
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileContent = file.OpenReadStream()
        };

        // Aquí es donde MediatR hace su trabajo
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpGet("status/{fileId}")]
    public async Task<IActionResult> GetUploadStatus(Guid fileId, CancellationToken cancellationToken)
    {
        var query = new GetUploadStatusQuery { FileId = fileId };
        var response = await _mediator.Send(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var query = new GetAllUploadsQuery();
        var response = await _mediator.Send(query, cancellationToken);
        return Ok(response);
    }
}
