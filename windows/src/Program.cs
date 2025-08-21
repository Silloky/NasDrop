using Spectre.Console;
using Newtonsoft.Json;

class Program
{
    public static Utils.Config config = new Utils.Config();
    public static Api? api;

    static async Task<int> Main(string[] args)
    {

        // BANNER, that is shown whatever the use
        Utils.WriteBanner();

        // Get and parse config file
        string configPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NasDrop"), "config.json");
        if (!File.Exists(configPath))
        {
            Utils.WriteMessage("Config file not found. Please run installer.", false);
            return 1;
        }
        string configContent = await File.ReadAllTextAsync(configPath);
        config = JsonConvert.DeserializeObject<Utils.Config>(configContent) ?? new Utils.Config();

        if (config.ServerUrl == "")
        {
            int result = 1;
            while (result == 1)
            {
                result = Setup.Run();
                Utils.Clear(true);
            }
        }
        else
        {
            api = new Api();
            if (!api.canConnectAfterWait())
            {
                Utils.WriteMessage("Failed to connect to server. Please check your network and VPN connection", false);
                AnsiConsole.WriteLine("Exiting...");
                Thread.Sleep(3000);
                return 1;
            }
        }

        if (args.Length == 0)
        {
            RunDashboard();
            return 0;
        }

        string filePath = args[0];
        return await RunShareCreation(filePath);
    }

    static void RunDashboard()
    {

        Console.WriteLine("=== NAS Share Dashboard ===");
        Console.WriteLine("1. List active shares");
        Console.WriteLine("2. Delete a share");
        Console.WriteLine("3. Exit");
        Console.Write("> ");
        var input = Console.ReadLine();

        // TODO: implement backend calls here
        Console.WriteLine($"(Not yet implemented: you chose {input})");
        AnsiConsole.MarkupLine($"[skyblue2]{JsonConvert.SerializeObject(config, Formatting.Indented)}[/]");
    }

    static async Task<int> RunShareCreation(string filePath)
    {
        Console.WriteLine($"Creating share for file: {filePath}");
        Thread.Sleep(2000);
        return 0;
    }


}