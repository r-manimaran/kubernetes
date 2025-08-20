using Serilog;

class Program
{
    private static readonly Random _random = new();
    private static readonly string[] _operations = { "ProcessOrder", "UpdateInventory", "SendNotification", "ValidatePayment" };
    private static readonly string[] _customers = { "CustomerA", "CustomerB", "CustomerC", "CustomerD" };

    static async Task Main(string[] args)
    {
        var logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "/var/logs/app.log";
        var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "unknown-pod";  

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .Enrich.WithProperty("PodName", podName)
            .CreateLogger();

        Log.Information("Application started on pad {podName}. Logging to: {LogPath}", logFilePath);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await ProcessDataLoop(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Application shutdown requested");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static async Task ProcessDataLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var operation = _operations[_random.Next(_operations.Length)];
                var customer = _customers[_random.Next(_customers.Length)];
                var amount = _random.Next(10, 1000);
                var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "unknown-pod";

                Log.Information("[{PodName}] Processing {Operation} for {Customer} with amount {Amount}", 
                    podName, operation, customer, amount);

                // Simulate random errors (20% chance)
                if (_random.Next(1, 6) == 1)
                {
                    var errorMsg = $"Failed to process {operation} for {customer}";
                    Log.Error("[{PodName}] Error occurred: {ErrorMessage}", podName,errorMsg);
                }
                else
                {
                    Log.Information("[{PodName}] Successfully completed {Operation} for {Customer}", 
                        podName, operation, customer);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unexpected error in processing loop");
            }

            await Task.Delay(30000, cancellationToken);
        }
    }
}