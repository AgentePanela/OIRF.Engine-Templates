using Engine.Shared.GameObjects;
using Microsoft.Xna.Framework;

namespace MyGame.Player;

[RegisterComponent("InputMover")]
public class InputMoverComponent : Component
{
    public float Speed { get; set; } = 100f; //idk

    public string ActionUp { get; set; } = "MoveUp";
    public string ActionDown { get; set; } = "MoveDown";
    public string ActionLeft { get; set; } = "MoveLeft";
    public string ActionRight { get; set; } = "MoveRight";

    public bool CameraZoomInput { get; set; } = false;
    public float ZoomPerMouse { get; set; } = 2f;
    public float ZoomPerAction { get; set; } = 1f;

    public string ActionZoomOut { get; set; } = "ZoomIn";
    public string ActionZoomIn { get; set; } = "ZoomOut";
}
