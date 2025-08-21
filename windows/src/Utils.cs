using Spectre.Console;
using System.Security.Principal;
using Newtonsoft.Json;

class Utils
{
    public static bool IsAdministrator()
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public static void WriteBanner()
    {
        AnsiConsole.Write(
            new FigletText("NasDrop")
                .Centered()
                .Color(Color.Green));
    }

    public static void Clear(bool showBanner)
    {
        AnsiConsole.Clear();
        if (showBanner)
        {
            WriteBanner();
        }
    }

    public static void WriteMessage(string message, bool success = true)
    {
        if (!success)
        {
            AnsiConsole.MarkupLine($"[red][[X]] {message}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green][[✓]] {message}[/]");
        }
    }

    public class Config
    {
        public AuthData Auth { get; set; } = new AuthData();
        public string ServerUrl { get; set; } = "";
        public string Drive { get; set; } = "";
        public string PublicEndpoint { get; set; } = "";

        public int Save(string? path = null)
        {
            if (path == null)
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NasDrop", "config.json");
            }

            var json = JsonConvert.SerializeObject(Program.config, Formatting.Indented);
            File.WriteAllText(path, json);
            return 0;
        }
    }
    public class AuthData
    {
        public string Token { get; set; } = "";
        public DateTime Expiry { get; set; } = DateTime.MinValue;
        public string Username { get; set; } = "";
    }
}