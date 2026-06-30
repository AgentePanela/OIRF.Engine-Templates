using System.Reflection;
using Engine.Shared.IoC;

namespace MyGame.Shared;

/// <summary>
/// Dummy content entry point to replic things that must be inited in both sides.
/// </summary>
public static class SharedEntryPoint
{
    public static void Init()
    {
        IoCManager.AutoRegister(Assembly.GetExecutingAssembly());
    }
}