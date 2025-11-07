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
        if (!request.IsValid())
            return BadRequest("مقادیر ارسالی معتبر نمی باشند");

        await codeGenerationService.GenerateAsync(request);
        return Ok();
    }
}