using MyGame.Shared;
using Engine.Server;
using System;
using System.Reflection;

var options = new ServerOptions()
{
    ServerName = "OIRF Dev 01",
    Version = "v1.0.0",
    TickRate = 60,
    Port = 1212,
    DataPath = "OIRF_Server",
    SaveConfigOnExit = false,
    Assemblies = [Assembly.GetExecutingAssembly(), Assembly.GetAssembly(typeof(SharedEntryPoint))!],
};

try
{
    using var game = new MyGame.Server.EntryPoint(options);
    game.Run();
}
catch (System.Reflection.ReflectionTypeLoadException ex)
{
    Console.WriteLine("ReflectionTypeLoadException caught!");
    foreach (var loaderEx in ex.LoaderExceptions)
    {
        Console.WriteLine(loaderEx?.Message);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}
Console.WriteLine("Goodbye!");
