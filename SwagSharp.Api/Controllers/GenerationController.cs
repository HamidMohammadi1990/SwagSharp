using Microsoft.AspNetCore.Mvc;
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
        if ((file?.Length ?? 0) == 0)
            return BadRequest("فایل Swagger ارسال نشده است.");

        string swaggerJson;
        using (var reader = new StreamReader(file!.OpenReadStream()))
        {
            swaggerJson = await reader.ReadToEndAsync();
        }

        try
        {
            var zipPath = await codeGenerationService.GenerateAndReturnZipAsync(swaggerJson);
            var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath);
            var fileName = Path.GetFileName(zipPath);

            return File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"خطا در تولید کد: {ex.Message}");
        }
    }
}