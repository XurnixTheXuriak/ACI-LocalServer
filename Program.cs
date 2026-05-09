using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LocalServer
{
    class Program
    {
        // SETTINGS

        static readonly string IP_ADDRESS = "127.0.0.1";
        static readonly int[] HTTP_PORTS = { 80, 443 };
        static readonly string BASE_PATH = AppContext.BaseDirectory;
        static string LOG_FILEPATH = Path.Combine(BASE_PATH, "Logs", "Server.log");
        
        // OTHER

        static readonly object _logLock = new();

        // PROGRAM START

        static async Task Main(string[] args)
        {
            RotateLogs();
            
            File.AppendAllText(LOG_FILEPATH, $"Ace Combat Infinity: Local Server started at {DateTime.Now}\nRuntime version {RuntimeInformation.FrameworkDescription}\nApplication Directory: {BASE_PATH}\n\n");
            
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.ClearProviders();

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;

                foreach (int port in HTTP_PORTS)
                {
                    options.Listen(System.Net.IPAddress.Parse(IP_ADDRESS), port);
                }
            });

            var app = builder.Build();

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                foreach (int port in HTTP_PORTS)
                {
                    Log($"[HTTP {IP_ADDRESS}:{port}] Started listening.", "Info");
                }
                Log("All listeners started. Server online.\n", "Info");
            });

        app.Use(async (context, next) =>
            {
                await LogRequestAsync(context);
                await next();
            });

            // method-specific catch-all routes.
            // GET  -> aircraft data payload
            // POST -> aircraft data payload (same envelope; PS3 client expects
            //         "data" field and crashes with a null deref otherwise)
            app.MapGet("/{**catchAll}", HandleHttpGetRequest);
            app.MapPost("/{**catchAll}", HandleHttpPostRequest);
            
            try
            {
                await app.RunAsync();
            }
            catch (Exception e)
            {
                HandleCrash(e);
            }

            Log("All listeners stopped.", "Info");
            Log("Server shutting down...", "Info");
            File.AppendAllText(LOG_FILEPATH, $"Server shutdown time: {DateTime.Now}, log file closed.");
            Environment.Exit(0);
        }

        public static void RotateLogs()
        {
            if (!Directory.Exists(Path.Combine (BASE_PATH, "Logs")))
            {
                Directory.CreateDirectory(Path.Combine(BASE_PATH, "Logs"));
            }

            if (!Directory.Exists(Path.Combine(BASE_PATH, "Crashes")))
            {
                Directory.CreateDirectory(Path.Combine(BASE_PATH, "Crashes"));
            }
            // Pretty simple implementation of a log rotation system. Stores them in LOG_FILEPATH, then once a new session starts. The old log is copied and numbered.
            // The old one is deleted and a new Server.log is generated in it's place.
            if (!File.Exists(LOG_FILEPATH))
                return;
            
            int LOG_INDEX = 0;
            string LOG_ARCHIVEPATH;
            do
            {
                LOG_ARCHIVEPATH = Path.Combine (BASE_PATH, "Logs", $"Server_{LOG_INDEX}.log");
                LOG_INDEX++;
            } 
            while (File.Exists(LOG_ARCHIVEPATH));
            
            File.Copy(LOG_FILEPATH, LOG_ARCHIVEPATH);
            File.Delete(LOG_FILEPATH);

        }
        
        // Generate useful crash data and diagnostics.
        public static void HandleCrash(Exception ex)
        {
            var CrashDumpPath = Path.Combine(BASE_PATH, "Crashes", $"LocalServerCrash-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            var OSName = RuntimeInformation.OSDescription;
            
            if (ex.Source == "System.Net.Sockets" && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Log($"Port Binding Failed: Unable to bind to Port 80 (HTTP), Port 443 (HTTPS)", "Error");
                Log(@"Note to Linux Users:
On Unix & Unix-Like Operating Systems. Applications not running as root (sudo) cannot bind to Port 80 (HTTP) or Port 443 (HTTPS), these ports are required for Local Server to function.
Please restart the Ace Combat Infinity Local Server as root (sudo), eg: sudo ./LocalServer" + '\n', "Info");
            }
            else if (ex.Source == "System.Net.Sockets")
            {
                Log($"Port Binding Failed: Unable to bind to Port 80 (HTTP), Port 443 (HTTPS)", "Error");
            }
            Log("Exception occured in main process: "+ ex.Message + " (" + ex.Source + ")", "Error");
            
            Console.Write("Writing crashlog to file: " + CrashDumpPath + '\n');
            
            var DumpText =
$@"Crash log generated on {DateTime.Now:yyyy-MM-dd_HH-mm-ss}
Crash caused by: {ex.Source}, with message: {ex.Message}

Operating System: {OSName}
Architecture: {RuntimeInformation.OSArchitecture}
Application Directory: {BASE_PATH}
Runtime Version: {RuntimeInformation.FrameworkDescription}

Call stack trace:
{ex.ToString()}";
            
            File.AppendAllText(CrashDumpPath, DumpText);
        }
        
        static async Task LogRequestAsync(HttpContext context)
        {
            var request = context.Request;
            var local = context.Connection.LocalIpAddress?.ToString() ?? "?";
            var localPort = context.Connection.LocalPort;

            // buffer body so request handler can still read
            request.EnableBuffering();

            string body = "";
            if (request.ContentLength > 0 || request.Headers.ContainsKey("Transfer-Encoding"))
            {
                using var reader = new StreamReader(
                    request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            // build header block similar to original http log
            var sb = new StringBuilder();
            sb.Append($"[{local}:{localPort}] Received:\n\n");
            sb.Append($"{request.Method} {request.GetEncodedPathAndQuery()} {request.Protocol}\n");
            foreach (var header in request.Headers)
            {
                sb.Append($"{header.Key}: {header.Value}\n");
            }

            // format json printing to console
            if (!string.IsNullOrWhiteSpace(body))
            {
                sb.Append('\n');
                try
                {
                    var formatted = JToken.Parse(body).ToString(Formatting.Indented);
                    sb.Append(formatted);
                }
                catch (JsonException)
                {
                    sb.Append(body);
                }
                sb.Append('\n');
            }

            Log(sb.ToString(), "Info");
        }

        static async Task HandleHttpGetRequest(HttpContext context)
        {
            Log(
                "##########################################################\n" + 
                "# HTTP GET REQUEST CALLED - PLEASE REPORT TO OPTIMUS1200 #\n" +
                "##########################################################\n"
            , "Warn");

            var response = new JObject
            {
                ["status"] = 0,
                ["data"] = new JObject
                {
                    ["aircraft"] = new JArray()
                }
            };

            await WriteJsonResponseAsync(context, response);
        }

        static async Task HandleHttpPostRequest(HttpContext context)
        {
            var response = new JObject
            {
                ["status"] = 0,
                ["data"] = new JObject
                {
                    ["aircraft"] = new JArray()
                }
            };

            await WriteJsonResponseAsync(context, response);
        }

        static async Task WriteJsonResponseAsync(HttpContext context, JObject body)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body.ToString(Formatting.None));

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json;charset=utf-8";
            context.Response.ContentLength = bodyBytes.Length;
            context.Response.Headers["Connection"] = "close";

            await context.Response.Body.WriteAsync(bodyBytes, 0, bodyBytes.Length);
        }

        static class LogColors
        {
            public const string Reset = "\u001b[0m";

            public const string Info = "\u001b[36m"; 
            public const string Warn = "\u001b[33m";
            public const string Error = "\u001b[31m";
        }
        static void Log(string data, string level)
        {
            lock (_logLock)
            {
                switch (level)
                {
                    case "Info":
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd_HH-mm-ss}] {LogColors.Info}[Info] {LogColors.Reset}{data}");
                        File.AppendAllText(LOG_FILEPATH, $"[{DateTime.Now:yyyy-MM-dd_HH-mm-ss}] [Info] " + data + '\n');
                        break;
                    case "Warn":
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd_HH-mm-ss}] {LogColors.Warn}[Warning] {LogColors.Reset}{data}");
                        File.AppendAllText(LOG_FILEPATH, $"[{DateTime.Now:yyyy-MM-dd_HH-mm-ss}] [Warn] " + data + '\n');
                        break;
                    case "Error":
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd_HH-mm-ss}] {LogColors.Error}[Error] {LogColors.Reset}{data}");
                        File.AppendAllText(LOG_FILEPATH, $"[{DateTime.Now:yyyy-MM-dd_HH-mm-ss}] [Error] " + data + '\n');
                        break;
                }
            }
        }
    }
}

