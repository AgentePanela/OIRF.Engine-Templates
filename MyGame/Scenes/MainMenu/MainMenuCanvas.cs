using System;
using System.IO;
using System.Reflection;
using Engine.Client;
using Engine.Client.Assets;
using Engine.Client.Assets.Atlas;
using Engine.Shared.Configuration;
using Engine.Shared.Configuration.CVars;
using Engine.Client.Graphics;
using Engine.Client.UI;
using Engine.Client.UI.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace MyGame.Scenes.MainMenu;

public sealed class MainMenuCanvas : UICanvas
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    public override void BuildElements()
    {
        SetRootType<Panel>();
        Root.HorizontalAlignment = HorizontalAlignment.Stretch;
        Root.VerticalAlignment = VerticalAlignment.Stretch;
 
        var leftPanel = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new (35, -50), 
        };
        var logoSpr = Sprite2D.GetFromAtlas("Interface/Logo");
        var logo = new SpriteWidget(logoSpr);
        var fooBtn = CreateMenuButton(Loc.GetString("main-menu-scene-button-foo"));
        var exitBtn = CreateMenuButton(Loc.GetString("main-menu-scene-button-exit"));

        leftPanel.Widgets.Add(logo);
        leftPanel.Widgets.Add(CreateSeparator());
        leftPanel.Widgets.Add(fooBtn);
        leftPanel.Widgets.Add(exitBtn);

        var footer = new VerticalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new (35, 5),
        };

        var FooLabel = new Label() { Text = Loc.GetString("main-menu-scene-foo-label")};

        var path = Assembly.GetExecutingAssembly().Location;
        var builtDate = File.GetLastWriteTime(path);
        var versionTxt = Loc.GetString("main-menu-scene-footer-version", ("version", _cfg.Get(GameCVars.GameVersion)), ("build", builtDate));
        var version = new Label() 
        {
            Text = versionTxt,
            TextColor = new Color(220, 220, 220, 150),
        };
        footer.Widgets.Add(FooLabel);
        footer.Widgets.Add(version);

        AddElement(leftPanel);
        AddElement(footer);

        base.BuildElements();

        fooBtn.Click += (_, _) => Log.Debug("Hello World!");
        exitBtn.Click += (_, _) => GameClient.Instance.Exit();
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float dt)
    {
        base.Update(dt);
    }

    private Panel CreateSeparator(int height = 30)
    {
        return new Panel { Height = height };
    }

    public Button CreateMenuButton(string label, string id = "btn")
    {
        var btn = new Button()
        {
            Id = id,
            Content = new Label() {Text = label},
            Padding = new (15, 15),
            Margin  = new (0, 15),
            Width = 300,
        };

        return btn;
    }

}
