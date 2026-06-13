using System.Runtime.CompilerServices;
using Engine.Client.GameObjects;
using Engine.Client.Graphics;
using Engine.Client.Inputs;
using Engine.Client.Physics;
using Engine.Client.UI;
using Engine.Shared.GameObjects;
using Engine.Shared.Physics;
using Microsoft.Xna.Framework;

namespace MyGame.Player;

public sealed class InputMoverSystem : EntitySystem
{
    [Dependency] private readonly UIManager _ui = default!;
    [Dependency] private readonly Camera2D _camera = default!;
    [Dependency] private readonly InputManager _input = default!;

    public override void Update(float dt)
    {
        base.Update(dt);

        var query = GetEntitiesWithComp<InputMoverComponent>();
        foreach (var (uid, comp) in query)
        {
            if (!TryComp<PhysicsComponent>(uid, out var physics))
                continue;

            var dir = Vector2.Zero;

            if (_input.ActionDown(comp.ActionUp)) dir.Y -= 1;
            if (_input.ActionDown(comp.ActionDown)) dir.Y += 1;
            if (_input.ActionDown(comp.ActionLeft)) dir.X -= 1;
            if (_input.ActionDown(comp.ActionRight)) dir.X += 1;

            var stick = _input.GetThumbStickPosition(0);
            if (stick.LengthSquared() > 0.1f * 0.1f)
                dir = new Vector2(stick.X, -stick.Y);

            if (dir.LengthSquared() > 0f)
                dir = Vector2.Normalize(dir);

            physics.Velocity = dir * comp.Speed;

            if (comp.CameraZoomInput)
            {
                float wheel = _input.MouseWheelDeltaChanged().Delta;
                if (wheel != 0.0f && !_ui.IsMouseOverUI)
                {
                    if (wheel > 0.0f) _camera.ZoomIn(comp.ZoomPerMouse * dt);
                    else _camera.ZoomOut(comp.ZoomPerMouse * dt);
                }

                if (_input.ActionDown(comp.ActionZoomIn)) _camera.ZoomIn(comp.ZoomPerAction * dt);
                if (_input.ActionDown(comp.ActionZoomOut)) _camera.ZoomOut(comp.ZoomPerAction * dt);
            }
        }
    }
}
