using SwagSharp.Application.Services;
using SwagSharp.Application.Services.CodeGen;
using SwagSharp.Application.Contracts.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
builder.Services.AddScoped<IModelGeneratorService, ModelGeneratorService>();
builder.Services.AddScoped<IServiceGeneratorService, ServiceGeneratorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

await app.RunAsync();