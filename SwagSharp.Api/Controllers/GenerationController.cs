using Microsoft.AspNetCore.Mvc;
using SwagSharp.Api.Extensions;
using SwagSharp.Application.Contracts.Services;

namespace SwagSharp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenerationController(ICodeGenerationService codeGenerationService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadSwaggerFile(IFormFile file)
    {
        if (file is null || (file?.Length ?? 0) == 0)
            return BadRequest("فایل Swagger ارسال نشده است.");

        string swaggerJson = await file!.ReadToEndAsync();

        await codeGenerationService.GenerateAsync(swaggerJson);
        return Ok(swaggerJson);
    }
}