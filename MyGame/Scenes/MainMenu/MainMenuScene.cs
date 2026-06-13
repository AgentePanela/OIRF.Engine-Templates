using Engine.Client.Scenes;
using Engine.Client.UI;
using Microsoft.Xna.Framework;

namespace MyGame.Scenes.MainMenu;

public sealed class MainMenuScene : Scene
{
    public override UICanvas? DefaultCanvas { get; protected set; } = new MainMenuCanvas();

    public override void OnSceneStart()
    {
        base.OnSceneStart();

        _entManager.CreateEntity("TestPlayer", new Vector2(500, 250));
    }

    public override void Update(float dt)
    {
        base.Update(dt);
    }
}
