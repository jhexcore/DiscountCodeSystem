using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DiscountServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            int port = 6001;
            if (args.Length > 0 && int.TryParse(args[0], out var p)) port = p;

            var manager = new DiscountManager();
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Discount Server started on port {port}...");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client, manager));
            }
        }

        private static async Task HandleClient(TcpClient client, DiscountManager manager)
        {
            Console.WriteLine("Client connected.");
            using var _ = client;
            using var stream = client.GetStream();
            var buffer = new byte[4096];
            var sb = new StringBuilder();

            await WriteAsync(stream, "Connected to Discount Server. Commands: GENERATE <count> [7|8] | USE <code> | EXIT\n");

            while (true)
            {
                int bytesRead = 0;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0) break;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                if (!sb.ToString().EndsWith("\n")) continue;

                var input = sb.ToString().Trim();
                sb.Clear();

                string? response = null;
                try
                {
                    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var cmd = parts[0].ToUpperInvariant();

                    switch (cmd)
                    {
                        case "GENERATE":
                            if (parts.Length < 2 || !int.TryParse(parts[1], out var count))
                            {
                                response = "ERROR: Usage GENERATE <count> [7|8]";
                            }
                            else
                            {
                                int? len = null;
                                if (parts.Length >= 3)
                                {
                                    if (int.TryParse(parts[2], out var l) && (l == 7 || l == 8))
                                        len = l;
                                    else
                                    {
                                        response = "ERROR: Length must be 7 or 8";
                                    }
                                }

                                if (response == null)
                                {
                                    try
                                    {
                                        var codes = manager.GenerateCodes(count, len);
                                        response = codes != null ? string.Concat("true ",string.Join(",", codes)): "false";
                                    }
                                    catch
                                    {
                                        response = "false";
                                    }
                                }
                            }
                            break;
                        case "USE":
                            if (parts.Length < 2) response = "ERROR: Usage USE <code>";
                            else response = manager.UseCode(parts[1]);
                            break;
                        case "EXIT":
                            await WriteAsync(stream, "Goodbye!\n");
                            return;
                        default:
                            response = "ERROR: Unknown command";
                            break;
                    }
                }
                catch (Exception ex)
                {
                    response = $"ERROR: {ex.Message}";
                }

                await WriteAsync(stream, response + "\n");
            }

            Console.WriteLine("Client disconnected.");
        }

        private static ValueTask WriteAsync(NetworkStream stream, string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            return stream.WriteAsync(data);
        }
    }
}
