namespace LtbToSmd.Models;

public interface ILogger
{
    void PrintLog(string log);
    string GetString(string key, params object?[] args);
}
