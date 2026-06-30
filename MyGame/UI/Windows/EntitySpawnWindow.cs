using System;
using System.Diagnostics.CodeAnalysis;
using Engine.Client.Graphics;
using Engine.Client.Inputs;
using Engine.Client.UI;
using Engine.Client.UI.Widgets;
using Engine.Shared.GameObjects;
using Engine.Shared.GameObjects.Factories;
using Engine.Shared.IoC;
using Engine.Shared.Physics.Fixtures;
using Engine.Shared.Prototypes;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace MyGame.UI.Windows;

/// <summary>
/// A debugging window that shows all the available entity prototypes for spawning.
/// </summary>
public sealed class EntitySpawnWindow : DefaultWindow
{
    [Dependency] private readonly InputManager _input = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly UIManager _ui = default!;
    [Dependency] private readonly CollisionSystem _collSys = default!;
    [Dependency] private readonly TransformSystem _tranSys = default!;

    public ListView ListBox = new();
    public TextBox SearchBox = new();
    private WindowState _state = WindowState.None;
    private Label _stateLabel = new();

    public override void BuildElements()
    {
        Title = Loc.GetString("ent-spawn-window-title");
        Padding = new(10);

        SetRootType<VerticalStackPanel>();
        Root.HorizontalAlignment = HorizontalAlignment.Stretch;
        Root.VerticalAlignment = VerticalAlignment.Stretch;

        SearchBox.HintText = Loc.GetString("ent-spawn-window-text-box-tooltip");
        SearchBox.Margin = new(0, 0, 0, 10);
        AddElement(SearchBox);

        ListBox.MinWidth = 300;
        ListBox.MaxWidth = 300;
        ListBox.MinHeight = 350;
        ListBox.MaxHeight = 350;
        ListBox.SelectedIndex = null;
        AddElement(ListBox);

        var btn = CreateButton(Loc.GetString("ent-spawn-window-deleting-btn"));
        AddElement(btn);
        AddElement(_stateLabel);

        RefreshEntityList();
        SearchBox.TextChanged += (_, _) => RefreshEntityList();
        ListBox.SelectedIndexChanged += (_, _) => _state = WindowState.Placing;
        btn.Click += (_, _) => OnDeleteBtnClick();

        base.BuildElements();
    }

    private void OnDeleteBtnClick()
    {
        if (_state == WindowState.Deleting)
            _state = WindowState.None;
        _state = WindowState.Deleting;
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        if (_input.MouseClicked(MouseButton.Right))
        {
            if (_state == WindowState.Placing)
                RefreshEntityList(); // clear the list, this will make the selection disapper 
                                     // (idk how to make it disappear without deleting everything)
            _state = WindowState.None;
        }

        if (_input.MouseClicked(MouseButton.Left) && !_ui.IsMouseOverUI &&
            _state == WindowState.Placing)
        {
            var pos = _input.MouseWorldPosition;
            if (ListBox.SelectedIndex is not null)
            {
                var proto = ListBox.Widgets[ListBox.SelectedIndex.Value] as EntityListItem;
                if (proto is not null)
                    _entMan.CreateEntity(proto.ProtoId, pos);
            }
        }

        if (_input.MouseClicked(MouseButton.Left) && !_ui.IsMouseOverUI &&
            _state == WindowState.Deleting)
        {
            var pos = _input.MouseWorldPosition;
            if (_collSys.TryGetEntityAtPosition(pos, out var uid)) // try to delete a entity using collision
                _entMan.DeleteEntity(uid);
            else if (_tranSys.TryGetEntityAtWorld(pos, out uid, 16f, false)) // try to delete the entity using transform coordinates
                _entMan.DeleteEntity(uid);

        }

        _stateLabel.Text = Loc.GetString($"ent-spawn-window-state-{_state.ToString()}");
    }

    private void RefreshEntityList()
    {
        int maxValues = int.MaxValue;
        var currentIndex = 0;
        var filter = SearchBox.Text?.Trim().ToLower() ?? "";
        ListBox.SelectedIndex = null;
        ListBox.Widgets.Clear();

        var protos = _protoMan.GetAll<EntityPrototype>();
        foreach ((var id, var proto) in protos)
        {
            if (currentIndex >= maxValues)
                return; // make sure that theres nothing below this foreach block

            if (proto.Abstract)
                continue;

            if (!string.IsNullOrEmpty(filter) && !id.ToLower().Contains(filter))
                continue;

            string? spriteKey = TryGetBaseSpriteKey(proto);
            var isOdd = currentIndex % 2 != 0;
            var entItem = new EntityListItem(id, isOdd, spriteKey);

            ListBox.Widgets.Add(entItem);
            currentIndex++;
        }
    }

    private Button CreateButton(string label, string id = "btn")
    {
        var btn = new Button()
        {
            Id = id,
            Content = new Label() { Text = label },
            Padding = new(15, 15),
            Margin = new(0, 15),
            MinWidth = 300,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        return btn;
    }

    private class EntityListItem : HorizontalStackPanel
    {
        public SpriteWidget? Sprite;
        public Label Label = new();
        public ProtoId<EntityPrototype> ProtoId;
        public EntityListItem(string id, bool odd = false, string? spr = null)
        {
            ProtoId = new(id);
            Label.Text = id;
            if (spr is not null)
            {
                Sprite = new(spr);
                Sprite.HorizontalAlignment = HorizontalAlignment.Left;
                Sprite.VerticalAlignment = VerticalAlignment.Center;
                Sprite.MaxWidth = 32;
                Sprite.MaxHeight = 32;
                Sprite.Width = 32;
                Sprite.Height = 32;
                Label.Margin = new(8, 16, 0, 16);
                Widgets.Add(Sprite);
            }
            else
                Label.Margin = new(40, 16, 0, 16); // simulates a sprite

            Widgets.Add(Label);

            if (odd)
                Background = new SolidBrush(new Color(0, 0, 0, 100));
        }
    }

    /// <summary>
    /// This tries to get the prototype base sprite based on if it has SpriteComponent or any other similar visual component.
    /// </summary>
    public bool TryGetBaseSprite(EntityPrototype proto, [NotNullWhen(true)]out Sprite2D? spr)
    {
        spr = default;
        var key = TryGetBaseSpriteKey(proto);
        if (key is null)
            return false;
        
        spr = Sprite2D.GetFromAtlas(key);
        return true;
    }

    /// <summary>
    /// This tries to get the prototype base sprite key based on if it has SpriteComponent or any other similar visual component.
    /// </summary>
    public string? TryGetBaseSpriteKey(EntityPrototype proto)
    {
        var sprType = IoCManager.Resolve<ComponentFactory>().GetSanitizedByType<SpriteComponent>();
        if (sprType is null || !proto.TryGetComponentEntry(sprType, out var compEntry))
            return null;
        
        if (!compEntry.TryGet<string>("Key", out var key))
            return null;
        
        return key;
    }

    private enum WindowState
    {
        Placing,
        Deleting,
        None
    }
}