using System.Data;
using FileStorage.DataAccess;
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
// todo разобраться с DisableAntiforgery()

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAntiforgery();

builder.Services.AddHealthChecks();

var connectionString = GetConnectionString();
builder.Services.AddScoped<IDbConnection, NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));
builder.Services.AddScoped<FileSqlRepository>();
builder.Services.AddScoped<FileService>();
AddApplicationConfig();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAntiforgery();


//////////////////////////////////////// config endpoints mapping
app.MapHealthChecks("ping");

app.MapGet("/api/files/get-id-by-hash/{hash:required}", ([FromRoute] string hash, FileService service) =>
{
    var id = service.GetIdByHash(hash);
    return id is not null
        ? Results.Ok(id)
        : Results.NotFound();
});

app.MapGet("/api/files/{id:guid:required}/download", ([FromRoute] Guid id, FileService service) =>
{
    var file = service.GetFileDetails(id);
    return Results.File(file.Stream, "application/octet-stream", file.Name);
});

app.MapPost("/api/files/upload-single", (IFormFile file, FileService service) =>
{
    try
    {
        var id = service.Upload(file.OpenReadStream(), file.FileName);
        return Results.Ok(id);
    }
    catch (Exception err)
    {
        Log.Logger.Error(err, 500, "Загрузка файла в хранилище");
        return Results.Problem();
    }
}).DisableAntiforgery();

app.MapPost("/api/files/upload-multiple", (IFormFileCollection files, FileService service) =>
{
    try
    {
        // При загрузке нескольких файлов клиент может передать несколько одинаковых файлов, которые будут
        // сохранены под одним id. Чтобы не возвращать несколько одинаковых id, используется HashSet.
        var ids = new HashSet<Guid>();
        foreach (var file in files)
            ids.Add(service.Upload(file.OpenReadStream(), file.FileName));

        return Results.Ok(ids);
    }
    catch (Exception err)
    {
        Log.Logger.Error(err, 500, "Загрузка файла в хранилище");
        return Results.Problem();
    }
}).DisableAntiforgery();

app.MapPost("/api/files/decrease-links-count", ([FromBody] Guid id, FileService service) =>
{
    try
    {
        service.DecreaseLinksCount(id);
        return Results.Ok();
    }
    catch (FileDoesNotExistException err)
    {
        Log.Logger.Warning(404, "Уменьшение количества ссылок на файл", err.Message);
        return Results.NotFound();
    }
    catch (LinksCountCannotBeNegativeException err)
    {
        Log.Logger.Warning(409, "Уменьшение количества ссылок на файл", err.Message);
        return Results.Conflict();
    }
    catch (Exception err)
    {
        Log.Logger.Error(err, 500, "Уменьшение количества ссылок на файл");
        return Results.Problem();
    }
});

app.Run();
return;


string GetConnectionString()
{
    if (builder.Environment.IsProduction())
        return ConstructConnectionStringFromEnvironment();

    return builder.Configuration.GetConnectionString("FileStorageDatabase") ??
           throw new ApplicationException("Строка подключения к базе данных отсутствует в файле конфигурации");
}

string ConstructConnectionStringFromEnvironment()
{
    var vars = Environment.GetEnvironmentVariables();
    var host = Environment.GetEnvironmentVariable("DATABASE_HOST") ??
               throw new ApplicationException($"Переменная окружения DATABASE_HOST отсутствует");
    var port = Environment.GetEnvironmentVariable("DATABASE_PORT") ??
               throw new ApplicationException($"Переменная окружения DATABASE_PORT отсутствует");
    var database = Environment.GetEnvironmentVariable("DATABASE_NAME") ??
                   throw new ApplicationException($"Переменная окружения DATABASE_NAME отсутствует");
    var username = Environment.GetEnvironmentVariable("DATABASE_USERNAME") ??
                   throw new ApplicationException($"Переменная окружения DATABASE_USERNAME отсутствует");
    var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ??
                   throw new ApplicationException($"Переменная окружения DATABASE_PASSWORD отсутствует");

    var connectionString_ =
        $"Host = {host}; Port = {port}; Database = {database}; Username = {username}; Password = {password};";
    return connectionString_;
}

void AddApplicationConfig()
{
    var storagePath = GetStoragePath();
    if (!Directory.Exists(storagePath))
        Directory.CreateDirectory(storagePath);
    builder.Services.AddSingleton<ApplicationConfig>(_ => new ApplicationConfig(storagePath));
}

string GetStoragePath()
{
    if (builder.Environment.IsProduction())
        return Environment.GetEnvironmentVariable("STORAGE_DIRECTORY_PATH") ??
               throw new ApplicationException($"Переменная окружения STORAGE_DIRECTORY_PATH отсутствует");
    
    return builder.Configuration.GetValue<string>("AbsoluteStoragePath") ??
           throw new ApplicationException(
               "Путь директории файлового хранилища отсутствует в файле конфигурации");
}