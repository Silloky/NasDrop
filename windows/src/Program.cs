using Spectre.Console;
using Newtonsoft.Json;

class Program
{
    public static Utils.Config config = new Utils.Config();
    public static Api? api;

    static async Task<int> Main(string[] args)
    {

        // BANNER, that is shown whatever the use
        Utils.Clear(true);

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
        if (config.Auth.Expiry < DateTime.Now)
        {
            Utils.WriteMessage("Token has expired, running setup...", false);
            Thread.Sleep(3000);
            Setup.Run();
        }

        if (args.Length == 0)
        {
            while (true)
            {
                Dashboard.Show();
                return 0;
            }
        }
        else if (args.Length == 1)
        {
            NewShare.Show(args[0]);
            Thread.Sleep(Timeout.Infinite);
            return 0;
        }
        else
        {
            Utils.WriteMessage("Invalid arguments. Please provide a valid file path or run without arguments.", false);
            return 1;
        }
    }

}