using System.Data;
using FileStorage.Infrastructure;
using FileStorage.Services;
using FileStorage.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// todo add authentication & authorization

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

var connectionString = builder.Configuration.GetConnectionString("FileStorageDatabase") ??
                       throw new ApplicationException("Не удалось подключить строку подключения к базу данных");
builder.Services.AddScoped<IDbConnection, NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));
builder.Services.AddScoped<FileService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



//////////////////////////////////////// config endpoints mapping
app.MapHealthChecks("ping");

app.MapGet("/api/files/get-id-by-hash", async (HttpRequest request, FileService service) =>
{
    var hash = await request.ReadFromJsonAsync<string>();
    if (string.IsNullOrEmpty(hash))
        return Results.BadRequest("Hash is required to be in body");
    
    var id = service.GetId(hash);
    return id is not null
        ? Results.Ok(id)
        : Results.NotFound();
});

app.MapPost("/api/files/increase-links", ([FromBody] Guid id, FileService service) =>
{
    try
    {
        service.IncreaseLinksCount(id);
        return Results.Ok();
    }
    catch (FileDoesNotExistExistException err)
    {
        Log.Logger.Warning(404, "Увеличение количества ссылок на файл", err.Message);
        return Results.NotFound();
    }
    catch (Exception err)
    {
        Log.Logger.Error(err, 500, "Увеличение количества ссылок на файл");
        return Results.Problem();
    }
});

app.MapPost("/api/files/decrease-links", ([FromBody] Guid id, FileService service) =>
{
    try
    {
        service.DecreaseLinksCount(id);
        return Results.Ok();
    }
    catch (FileDoesNotExistExistException err)
    {
        Log.Logger.Warning(404, "Уменьшение количества ссылок на файл", err.Message);
        return Results.NotFound();
    }
    catch (Exception err)
    {
        Log.Logger.Error(err, 500, "Уменьшение количества ссылок на файл");
        return Results.Problem();
    }
});

app.Run();