
class Program
{
    static async Task<int> Main(string[] args)
    {

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
    }

    static async Task<int> RunShareCreation(string filePath)
    {
        Console.WriteLine($"Creating share for file: {filePath}");
        return 0;
    }


}