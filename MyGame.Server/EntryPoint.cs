using MyGame.Shared;
using Engine.Server;
using Engine.Shared.IoC;
using System;
using System.Reflection;

namespace MyGame.Server;

public class EntryPoint : GameServer
{
    public EntryPoint(ServerOptions options) : base(options)
    {
        // register [RegisterIoC] attributes from this project
        SharedEntryPoint.Init();
        IoCManager.AutoRegister(Assembly.GetExecutingAssembly());
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        // TODO: Add your initialization logic here
    }

    protected override void PrototypesToIgnore()
    {
        base.PrototypesToIgnore();
        Prototypes.IgnorePrototypes(["tile"]);
    }

    protected override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (State != ServerState.Running)
            return;

        // TODO: Add your update logic here
    }

    protected override void OnShutdown()
    {
        base.OnShutdown();

        // TODO: Add your cleanup logic here
    }
}
