using Engine.Client;
using Engine.Client.Assets.Atlas;
using MyGame.Scenes.MainMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.Reflection;

var options = new EntryPointOptions()
{
    // Basic game info (REQUIRED)
    Width = 854,
    Height = 480,
    Title = "MyGame",
    Version = "v1.0.0",

    // Pass the Program init args (REQUIRED)
    Args = args,

    // All my game content assemblies (REQUIRED)
    Assemblies = [Assembly.GetExecutingAssembly()],

    // The first scene the engine will change when the game is loaded (REQUIRED)
    InitialScene = typeof(MainMenuScene),


    // The default background color (scenes can have their own background)
    BackgroundColor = Color.Black,

    // This is the default sampling used by the renderer
    // Linear = Non-pixel art games
    // Point = Pixel art games (no blurry effect)
    Samplimg = SamplerState.PointClamp,

    // The AppData folder name
    DataPath = "MyGame",

    // This will make the engine save all modified CVars on exit
    SaveConfigOnExit = false,

    // This will set the default texture atlas size (change to 4096 if your game use big sprites)
    TextureAtlasSize = AtlasSize.Size2048,
    

    // If the game will be in fullscreen by default
    FullScreen = false,

    // This will make the game pauses on unfocous (EXPERIMENTAL)
    PauseOnUnfocus = false,

    // This will enable or disable the game window resizing
    WindowResizing = true,

    // More options also available
};

using var game = new MyGame.EntryPoint(options);
game.Run();
