using SwagSharp.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using SwagSharp.Api.Contracts.Services;

namespace SwagSharp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenerationController(ICodeGenerationService codeGenerationService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadSwaggerFile(GenerateServiceRequest request)
    {
        if (request.File is null || (request.File?.Length ?? 0) == 0)
            return BadRequest("فایل Swagger ارسال نشده است.");

        await codeGenerationService.GenerateAsync(request);
        return Ok();
    }
}