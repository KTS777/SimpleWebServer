using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class WebServer
{
    private readonly int port;
    private readonly string webRoot;

    public WebServer(int port, string webRoot)
    {
        this.port = port;
        this.webRoot = webRoot;
    }

    public void Start()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Server started on port {port}...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread thread = new Thread(() => HandleClient(client));
            thread.Start();
        }
    }

    private void HandleClient(TcpClient client)
    {
        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new StreamReader(stream);
        using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        string requestLine = reader.ReadLine();
        if (string.IsNullOrEmpty(requestLine)) return;

        string[] tokens = requestLine.Split(' ');
        string method = tokens[0];
        string url = Uri.UnescapeDataString(tokens[1]);

        if (method != "GET")
        {
            SendError(writer, 405, "Method Not Allowed");
            return;
        }

        if (url.Contains(".."))
        {
            SendError(writer, 403, "Forbidden");
            return;
        }

        if (url == "/") url = "/index.html";

        string filePath = Path.Combine(webRoot, url.TrimStart('/'));
        string extension = Path.GetExtension(filePath);

        if (!File.Exists(filePath))
        {
            SendError(writer, 404, "Not Found");
            return;
        }

        if (!IsSupportedExtension(extension))
        {
            SendError(writer, 403, "Forbidden");
            return;
        }

        byte[] content = File.ReadAllBytes(filePath);
        string contentType = GetContentType(extension);

        writer.WriteLine("HTTP/1.1 200 OK");
        writer.WriteLine($"Content-Type: {contentType}");
        writer.WriteLine($"Content-Length: {content.Length}");
        writer.WriteLine();
        stream.Write(content, 0, content.Length);
    }

    private void LogRequest(string method, string path, int statusCode)
    {
        string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {method} {path} => {statusCode}";
        File.AppendAllText("requests.log", logLine + Environment.NewLine);
    }


    private void SendError(StreamWriter writer, int statusCode, string message, string method = "GET", string path = "/")
    {
        string errorFilePath = Path.Combine(webRoot, "error.html");
        string html;

        if (File.Exists(errorFilePath))
        {
            html = File.ReadAllText(errorFilePath)
                .Replace("{{statusCode}}", statusCode.ToString())
                .Replace("{{message}}", message);
        }
        else
        {
            html = $"<html><head><title>{statusCode} {message}</title></head><body><h1>Error {statusCode}: {message}</h1></body></html>";
        }

        writer.WriteLine($"HTTP/1.1 {statusCode} {message}");
        writer.WriteLine("Content-Type: text/html");
        writer.WriteLine($"Content-Length: {Encoding.UTF8.GetByteCount(html)}");
        writer.WriteLine();
        writer.Write(html);

        LogRequest(method, path, statusCode);
    }


    private bool IsSupportedExtension(string ext) =>
        ext == ".html" || ext == ".css" || ext == ".js";

    private string GetContentType(string ext) => ext switch
    {
        ".html" => "text/html",
        ".css" => "text/css",
        ".js" => "application/javascript",
        _ => "application/octet-stream"
    };
}
