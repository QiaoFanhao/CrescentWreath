using CrescentWreath.ServerPrototype;

var configuredPort = 0;
if (args.Length > 0 && int.TryParse(args[0], out var parsedPort))
{
    configuredPort = parsedPort;
}

await using var hostRuntime = new ServerPrototypeWebSocketHostRuntime();
var wsUri = await hostRuntime.startAsync(configuredPort);
Console.WriteLine($"CrescentWreath.ServerPrototype.Host started. WebSocket endpoint: {wsUri}");
Console.WriteLine("Press Ctrl+C to stop.");

var shutdownSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdownSignal.TrySetResult();
};

await shutdownSignal.Task;
