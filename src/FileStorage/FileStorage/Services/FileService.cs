using System.Data;
using Dapper;
using FileStorage.Services.Exceptions;

namespace FileStorage.Services;

public class FileService(IDbConnection connection)
{
    public Guid? GetId(string hash)
    {
        connection.Open();
        var id = connection.ExecuteScalar<Guid?>(
            "select id from files where hash = @hash", 
            new { hash });
        connection.Close();
        
        return id;
    }

    public void IncreaseLinksCount(Guid fileId)
    {
        connection.Open();
        var updated = connection.Execute(
            "update files set links_count = links_count + 1 where id = @fileId",
            new { fileId });
        connection.Close();
        
        if (updated < 1)
            throw new FileDoesNotExistExistException();
    }

    public void DecreaseLinksCount(Guid fileId)
    {
        connection.Open();
        var updated = connection.Execute(
            "update files set links_count = links_count - 1 where id = @fileId",
            new { fileId });
        connection.Close();
        
        // todo обработка исключения субд, возникающее при попытке уменьшить нулевое количество ссылок
        
        if (updated < 1)
            throw new FileDoesNotExistExistException();
    }
}