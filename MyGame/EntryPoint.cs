using Engine.Client;
using Engine.Client.Debug.Diagnostics;
using Engine.Client.Inputs;
using Engine.Shared.IoC;
using Engine.Client.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MyGame.UI.Windows;

namespace MyGame;

public class EntryPoint : GameClient
{
    //! This code block is used to enable cmd console if the game is opened with --console
    #if !DEBUG
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern bool AllocConsole();
    #endif

    //! This code is for the entity spawn window available in this template
    private EntitySpawnWindow? spawnWindow = null;

    public EntryPoint(ClientOptions options) : base(options)
    {
        //! This code block is used to enable cmd console if the game is opened with --console
        #if !DEBUG
        if (OperatingSystem.IsWindows() && Options.Args.Contains("--console"))
        {
            AllocConsole(); // activate local console

            var stdOut = Console.OpenStandardOutput();
            var writer = new StreamWriter(stdOut)
            {
                AutoFlush = true
            };

            Console.SetOut(writer);
            Console.SetError(writer);
        }
        #endif

        // register [RegisterIoC] attributes from this project
        IoCManager.AutoRegister(Assembly.GetExecutingAssembly());
    }

    protected override void Initialize()
    {
        base.Initialize();

        // TODO: Add your initialization logic here
        UITheme.ApplyFlatColors();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (GameState != GameState.Running)
            return;

        // TODO: Add your update logic here

        //! This code is for the entity spawn window available in this template
        if (InputManager.KeyPressed(Keys.F5))
            ToggleSpawnWindow();
    }

    //! This code is for the entity spawn window available in this template
    private void ToggleSpawnWindow()
    {
        if (spawnWindow is not null)
        {
            WindowManager.Close(spawnWindow);
            spawnWindow = null;
        }
        else
            spawnWindow = WindowManager.OpenWindow<EntitySpawnWindow>();
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        if (GameState != GameState.Running)
            return;

        // TODO: Add your drawing code here
    }
}
