using System.Net.Sockets;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        string host = args.Length > 0 ? args[0] : "127.0.0.1";
        int port = args.Length > 1 && int.TryParse(args[1], out var p) ? p : 6001;

        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        using var stream = client.GetStream();

        var buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer);
        Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, bytesRead));

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            await stream.WriteAsync(Encoding.UTF8.GetBytes(input + "\n"));

            bytesRead = await stream.ReadAsync(buffer);
            Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, bytesRead));

            if (input.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase)) break;
        }
    }
}
