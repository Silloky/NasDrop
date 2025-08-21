using System.Net;
using Spectre.Console;
using RestSharp;

class Setup
{
    public static int Run()
    {

        Utils.Clear(true);
        string serverUrl, username, password, token;
        DateTime expiry;
        Api api;

        AnsiConsole.Write(new Rule("[yellow]Setup[/]").Justify(Justify.Left));

        while (true)
        {
            serverUrl = AnsiConsole.Ask<string>("Enter server [skyblue2]URL[/]:");
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                Utils.WriteMessage("Server URL cannot be empty!", false);
            }
            api = new Api(serverUrl);
            if (!api.canConnect() && !api.canConnectAfterWait(6))
            {
                Utils.WriteMessage("Failed to connect to server. Please check your network connection and server URL.", false);
                Thread.Sleep(2000);
                Utils.Clear(true);
            }
            else
            {
                //Utils.WriteMessage("Connection successful", true);
                break;
            }
        }

        while (true)
        {
            // Username loop
            while (true)
            {
                username = AnsiConsole.Ask<string>("Enter your [skyblue2]username[/]:");
                if (string.IsNullOrWhiteSpace(username))
                {
                    Utils.WriteMessage("Username cannot be empty!", false);
                }
                else
                {
                    break;
                }
            }

            // Password loop
            while (true)
            {
                var password1 = AnsiConsole.Prompt(new TextPrompt<string>("Enter your [skyblue2]password[/]:").Secret());
                var password2 = AnsiConsole.Prompt(new TextPrompt<string>("Confirm your [skyblue2]password[/]:").Secret());
                if (password1 != password2)
                {
                    Utils.WriteMessage("Passwords do not match! Please try again.", false);
                    Thread.Sleep(1000);
                    continue;
                }
                else { password = password1; break; }
            }

            var authResponse = api.Request<Api.AuthRes>(Method.Post, "/api/auth", new { username, password });
            if (authResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return 1;
            }
            else if (authResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                Utils.WriteMessage("Authentication failed! Check your credentials.", false);
                Thread.Sleep(2000);
                continue;
            }
            else if (authResponse.StatusCode != HttpStatusCode.OK)
            {
                Utils.WriteMessage("An unknown error has occurred", false);
                Thread.Sleep(2000);
                continue;
            }
            else
            {
                token = authResponse.Data!.token;
                expiry = authResponse.Data!.expiry;
                Utils.WriteMessage("Authentication successful", true);
                break;
            }
        }

        DriveInfo[] allDrives = DriveInfo.GetDrives();
        var driveSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [skyblue2]drive[/]:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more drives)[/]")
                .AddChoices(allDrives.Select(d => d.Name).ToList()));
        var driveLetter = driveSelection.TrimEnd(Path.DirectorySeparatorChar);

        Program.config.ServerUrl = serverUrl;
        Program.config.Auth = new Utils.AuthData
        {
            Username = username,
            Token = token,
            Expiry = expiry
        };
        Program.config.Drive = driveLetter;
        Program.config.Save();

        AnsiConsole.Write(new Rule("[yellow]Setup Complete, returning to menu[/]").Justify(Justify.Left));
        Thread.Sleep(3000);

        return 0;
    }
}