class Program
{
    static void Main(string[] args)
    {
        int port = 8080;
        string webRoot = "webroot";

        WebServer server = new WebServer(port, webRoot);
        server.Start();
    }
}
