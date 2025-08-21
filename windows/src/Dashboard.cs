using Spectre.Console;
using Newtonsoft.Json;
using RestSharp;

class Dashboard
{
    public static void Show()
    {
        while (true)
        {
            Utils.Clear(true);
            var verb = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Please choose an action below")
                    .PageSize(10)
                    .AddChoices(new[] { "View and Modify Shares", "Create New Share", "Settings", "Exit" }));

            if (verb == "View and Modify Shares")
            {
                ShareManagement();
            }
            else if (verb == "Create New Share")
            {
                NewShare.Show();
            }
            else if (verb == "Settings")
            {
                Settings();
            }
            else if (verb == "Exit")
            {
                AnsiConsole.WriteLine("Exiting...");
                Thread.Sleep(1000);
                Environment.Exit(0);
            }
        }
    }

    private static int ShareManagement()
    {
        while (true)
        {
            
            Utils.Clear(true);

            Api.GetSharesRes shares;
            try
            {
                shares = Program.api!.Request<Api.GetSharesRes>(Method.Get, "/api/shares", null).Data;
            }
            catch (Exception)
            {
                return 1;
            }

            var table = new Table();
            table.AddColumns(new[] { "ID", "Path", "Created At", "Expires At", "Access Count", "Is Protected" });
            for (int i = 0; i < table.Columns.Count; i++)
            {
                table.Columns[i].Alignment = Justify.Center;
            }
            table.Border = TableBorder.Rounded;

            foreach (var share in shares)
            {
                table.AddRow(
                    share.Id,
                    share.Path,
                    share.Creation.Timestamp.ToLocalTime().ToString("g"),
                    share.Expiry.ToLocalTime().ToString("g"),
                    share.AccessCount.ToString(),
                    share.Auth.Username != "" ? "Yes" : "No"
                );
            }
            table.Expand().Centered();

            AnsiConsole.Write(table);

            string matchingId = "";
            while (matchingId == "")
            {
                var query = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter first characters of share ID to view details or [skyblue2]exit[/] to return to menu:"));
                if (query == "exit")
                {
                    break;
                }

                var allIds = shares.Select(s => s.Id).ToList();
                var matchingIds = allIds.Where(id => id.StartsWith(query, StringComparison.OrdinalIgnoreCase)).ToList();

                if (matchingIds.Count == 0)
                {
                    Utils.WriteMessage("No shares found with that ID prefix. Please try again.", false);
                    continue;
                }
                else if (matchingIds.Count == 1)
                {
                    matchingId = matchingIds.First();
                }
                else
                {
                    matchingId = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Multiple shares found. Please select one:")
                        .PageSize(10)
                        .AddChoices(matchingIds));
                }
            }

            if (matchingId == "")
            {
                break;
            }
            ShareDetails(shares.Find(share => share.Id == matchingId)!);
        }

        return 0;
    }

    private static Table makeShareInfoTable(Api.Share share)
    {
        TimeSpan difference = share.Expiry.ToLocalTime() - DateTime.Now;

        var table = new Table();
        table.AddColumns(new[] { "Sharing link", "Path", "Created At", "Expires At", "Username", "Password", "Access Count" });
        for (int i = 0; i < table.Columns.Count; i++)
        {
            table.Columns[i].Alignment = Justify.Center;
        }
        table.Border = TableBorder.Rounded;
        table.AddRow(
            "[link]http://localhost:3000/" + share.Id + "[/]",
            share.Path,
            share.Creation.Timestamp.ToLocalTime().ToString("g"),
            share.Expiry.ToLocalTime().ToString("g") + " (in " + difference.Days + "d " + difference.Hours + "h " + difference.Minutes + "m)",
            share.Auth.Username! == "" ? "No protection" : share.Auth.Username!,
            share.Auth.Password! == "" ? "No protection" : share.Auth.Password!,
            share.AccessCount.ToString()
        );
        return table;
    }

    private static void ShareDetails(Api.Share share)
    {
        var refresh = false;
        while (true)
        {

            Utils.Clear(true);

            if (refresh)
            {
                try
                {
                    var shares = Program.api!.Request<Api.GetSharesRes>(Method.Get, "/api/shares", null).Data;
                    share = shares.Find(newShare => newShare.Id == share.Id)!;
                }
                catch (Exception)
                {
                    break;
                }
            }

            var table = makeShareInfoTable(share);
            AnsiConsole.Write(table);

            var verb = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an action:")
                .AddChoices("Edit", "Delete", "Go back")
            );
            if (verb == "Go back")
            {
                break;
            }
            else if (verb == "Edit")
            {
                var toEdit = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("What would you like to edit?")
                        .AddChoices(new[] { "Path", "Expiry", "Username and Password", "Go back" }));
                Api.Share newShare = new Api.Share();
                if (toEdit == "Path")
                {
                    newShare.Path = AnsiConsole.Prompt(new TextPrompt<string>("Enter new path:"));
                }
                else if (toEdit == "Expiry")
                {
                    while (true)
                    {
                        var newExpiry = AnsiConsole.Prompt(new TextPrompt<string>("Enter new expiry (yyyy-MM-dd HH:mm):"));
                        if (DateTime.TryParse(newExpiry, out var expiry))
                        {
                            newShare.Expiry = expiry.ToUniversalTime();
                            break;
                        }
                        else
                        {
                            Utils.WriteMessage("Invalid date format. Please try again.", false);
                        }
                    }
                }
                else if (toEdit == "Username and Password")
                {
                    var newUsername = AnsiConsole.Prompt(new TextPrompt<string>("Enter new username:").AllowEmpty());
                    var newPassword = AnsiConsole.Prompt(new TextPrompt<string>("Enter new password:").AllowEmpty());
                    newShare.Auth.Username = newUsername;
                    newShare.Auth.Password = newPassword;
                }
                if (verb != "Go back")
                {
                    if (AnsiConsole.Confirm("Confirm change?"))
                    {
                        var body = new Api.ShareCreationReqBody();
                        body.WinPath = newShare.Path == null ? share.Path : newShare.Path;
                        body.Ttl = newShare.Expiry == DateTime.MinValue ? null : (int)(newShare.Expiry - share.Creation.Timestamp).TotalSeconds;
                        body.Auth = newShare.Auth.Username == null ? null : newShare.Auth;
                        try
                        {
                            Program.api!.Request<object>(Method.Patch, "/api/" + share.Id, body);
                            refresh = true;
                        }
                        catch (Exception e)
                        {
                            Console.Write(e);
                            break;
                        }
                    }
                }
            }
            else if (verb == "Delete")
            {
                var confirmation = AnsiConsole.Confirm("Confirm deletion?");
                if (confirmation)
                {
                    try
                    {
                        Program.api!.Request<object>(Method.Delete, "/api/" + share.Id, null);
                    }
                    catch (Exception e)
                    {
                        Console.Write(e);
                    }
                    break;
                }
            }

        }
    }

    private static void Settings()
    {
        Utils.Clear(false);
        Thread.Sleep(3000);
    }
}