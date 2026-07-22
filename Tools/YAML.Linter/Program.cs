using System.Diagnostics;
using Engine.Client.Assets;
using Engine.Shared.Prototypes;
using Engine.Shared.IoC;
using Engine.Shared.Assets;
using Engine.Shared;
using System.Reflection;
using Engine.Client;
using System.Diagnostics.CodeAnalysis;
using Engine.Server;
using MyGame;
using MyGame.Shared;

Log.Debug("Starting YAML Linter...");
Log.ExceptOnWarn = true;
IoCManager.Register<SharedContentManager>();
if (!GetAssemblies(out var assemblies)) 
{
    Log.Error("Failed on getting engine and client assemblies.");
    return 1;
}

IoCManager.Resolve<SharedContentManager>().InitAsServer(assemblies);
try
{
    var manager = IoCManager.Resolve<IPrototypeManager>();
    manager.Load();

    Log.Debug($"OK - {manager.Count()} prototypes loaded.");
    return 0;
}
catch (PrototypeLoadException ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FATAL: {ex.Message}");
    return 1;
}

bool GetAssemblies([NotNullWhen(true)] out Assembly[]? assemblies)
{
    assemblies = [Assembly.GetExecutingAssembly()];
    var client = Assembly.GetAssembly(typeof(GameClient));
    if (client is null)
        return false;

    var server = Assembly.GetAssembly(typeof(GameServer));
    if (server is null)
        return false;
    
    var cClient = Assembly.GetAssembly(typeof(EntryPoint));
    if (cClient is null)
        return false;

    var cShared = Assembly.GetAssembly(typeof(SharedEntryPoint));
    if (cShared is null)
        return false;

    var cServer = Assembly.GetAssembly(typeof(MyGame.Server.EntryPoint));
    if (cServer is null)
        return false;
    
    assemblies = [Assembly.GetExecutingAssembly(), client, server, cClient];
    return true;
}