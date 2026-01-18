using System;
using System.IO;

namespace OrderBookMonitor.Logging;

public class FileLogger
{
    private readonly string _path = "app.log";

    public void Info(string message)
    {
        File.AppendAllText(
            _path,
            $"[INFO] {DateTime.Now} {message}\n"
        );
    }

    public void Error(Exception ex)
    {
        File.AppendAllText(
            _path,
            $"[ERROR] {DateTime.Now} {ex}\n"
        );
    }

    public void Error(string message)
    {
        File.AppendAllText(
            _path,
            $"[ERROR] {DateTime.Now} {message}\n"
        );
    }
}
