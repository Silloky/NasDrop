using Spectre.Console;
using RestSharp;
class NewShare
{
    public static int Show(string? path = null)
    {
        Utils.Clear(true);
        bool standalone = path != null;
        if (path == null)
        {
            path = AnsiConsole.Ask<string>("Enter the [skyblue2]file path[/] to share (or [skyblue2]back[/] to go back):");
        }
        if (path == "back")
        {
            return 0;
        }

        // Check file existence, and if it's in the network drive configured as config.Drive
        if (!File.Exists(path))
        {
            Utils.WriteMessage("File does not exist. Please check the path and try again.", false);
            Thread.Sleep(3000);
            return 1;
        }
        if (!path.StartsWith(Program.config.Drive, StringComparison.OrdinalIgnoreCase))
        {
            Utils.WriteMessage($"File is not located in the configured network drive: {Program.config.Drive}", false);
            Thread.Sleep(3000);
            return 1;
        }

        int ttl;
        string ttl_choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an expiration for the share:")
                .AddChoices("No Expiry", "1 Hour", "3 Days", "2 Weeks", "Custom date")
        );
        switch (ttl_choice)
        {
            case "No Expiry":
                ttl = -1;
                break;
            case "1 Hour":
                ttl = 3600;
                break;
            case "3 Days":
                ttl = 72 * 3600;
                break;
            case "2 Weeks":
                ttl = 14 * 24 * 3600;
                break;
            case "Custom date":
                while (true)
                {
                    var expiry = AnsiConsole.Ask<string>("Enter the [skyblue2]expiry date[/] (yyyy-MM-dd HH:mm) or leave empty for no expiry:");
                    if (DateTime.TryParse(expiry, out DateTime expiryDate))
                    {
                        ttl = (int)(expiryDate - DateTime.Now).TotalSeconds;
                        break;
                    }
                    else
                    {
                        Utils.WriteMessage("Invalid date format. Please try again.");
                    }
                }
                break;
            default:
                ttl = 0;
                break;
        }

        Api.ShareAuthData auth = new Api.ShareAuthData();
        bool wantsAuth = AnsiConsole.Confirm("Do you want to [skyblue2]require authentication[/] for this share?");
        if (wantsAuth)
        {
            while (true)
            {
                string username = AnsiConsole.Ask<string>("Choose a [skyblue2]username[/] for authentication:");
                string password = AnsiConsole.Ask<string>("Choose a [skyblue2]password[/] for authentication:");

                AnsiConsole.Write(new Table()
                    .AddColumns("Username", "Password")
                    .AddRow(username, password));
                if (AnsiConsole.Confirm("Is this correct?"))
                {
                    auth.Username = username;
                    auth.Password = password;
                    break;
                }
            }
        }
        else
        {
            auth.Username = "";
            auth.Password = "";
        }

        try
        {
            Api api = Program.api!;
            var shareId = api.Request<Api.ShareCreationResBody>(Method.Put, "/api/", new Api.ShareCreationReqBody
            {
                WinPath = path,
                Ttl = ttl,
                Auth = auth
            }).Data.Id;
            Utils.WriteMessage($"Share (ID: {shareId}) created successfully! Please send the following link {(wantsAuth ? "and credentials" : "")} to the recipient(s)", true);

            var panel = new Panel(new Markup(Program.config.PublicEndpoint + shareId));
            var align = Align.Center(panel);
            AnsiConsole.Write(align);
            Thread.Sleep(2000);
            if (!standalone)
            {
                AnsiConsole.WriteLine("Press any key to return to Dashboard main menu");
                Console.ReadKey();
                return 0;
            }
            else
            {
                AnsiConsole.WriteLine("You may now close this window ; make edits to the share using the dashboard.");
                return 0;
            }
        }
        catch (Exception e)
        {
            Utils.WriteMessage($"Failed to create share: {e.Message}", false);
            return 1;
        }
    }
}