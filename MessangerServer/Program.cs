using MessengerServer;
using MessengerServer.Data;

try
{
    Console.WriteLine("═══════════════════════════════════════");
    Console.WriteLine("  MessengerServer - Starting...");
    Console.WriteLine("═══════════════════════════════════════\n");

    var dbService = new DbService();

    Console.WriteLine("📦 Initializing database...");
    await dbService.InitializeDatabaseAsync();
    Console.WriteLine("✅ Database initialized successfully!\n");

    var server = new Server(port: 5000);
    Console.WriteLine("🚀 Starting server on localhost:5000...\n");
    await server.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ FATAL ERROR: {ex.Message}");
    Console.WriteLine($"Details: {ex.InnerException?.Message}");
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
    Environment.Exit(1);
}