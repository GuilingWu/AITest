using Godot;

public partial class AreaManager : Node
{
    [Export] public Rect2 PuzzleRectWorld { get; set; } = new(new Vector2(-5.0f, -4.0f), new Vector2(10.0f, 6.0f));
    [Export] public Rect2 StorageRectWorld { get; set; } = new(new Vector2(-4.0f, 1.5f), new Vector2(8.0f, 4.0f));
    [Export] public NodePath CameraPath { get; set; } = new("../PuzzleViewportContainer/PuzzleViewport/PuzzleRoot/Camera3D");

    public Camera3D? Camera3D { get; private set; }

    public override void _Ready()
    {
        Camera3D = GetNodeOrNull<Camera3D>(CameraPath);
    }

    public bool TryProjectPointerToBoard(Vector2 screenPoint, out Vector3 worldPoint)
    {
        worldPoint = Vector3.Zero;
        Camera3D ??= GetNodeOrNull<Camera3D>(CameraPath);
        if (Camera3D == null)
        {
            return false;
        }

        var origin = Camera3D.ProjectRayOrigin(screenPoint);
        var normal = Camera3D.ProjectRayNormal(screenPoint);
        var plane = new Plane(Vector3.Up, 0.0f);
        var hit = plane.IntersectsRay(origin, normal);
        if (hit == null)
        {
            return false;
        }

        worldPoint = hit.Value;
        return true;
    }

    public bool IsInsidePuzzleArea(Vector3 worldPoint)
    {
        return PuzzleRectWorld.HasPoint(new Vector2(worldPoint.X, worldPoint.Z));
    }

    public bool IsInsideStorageArea(Vector3 worldPoint)
    {
        return StorageRectWorld.HasPoint(new Vector2(worldPoint.X, worldPoint.Z));
    }

    public Vector3 GetRandomStoragePosition(int index, StorageMode mode)
    {
        if (mode == StorageMode.HorizontalList)
        {
            var x = StorageRectWorld.Position.X + 0.8f + index * 1.15f;
            var z = StorageRectWorld.Position.Y + StorageRectWorld.Size.Y * 0.5f;
            return new Vector3(x, 0.0f, z);
        }

        var columns = 4;
        var column = index % columns;
        var row = index / columns;
        var spacingX = 1.55f;
        var spacingZ = 1.1f;
        var startX = StorageRectWorld.Position.X + 0.9f;
        var startZ = StorageRectWorld.Position.Y + 0.7f;
        var xPos = Mathf.Min(startX + column * spacingX, StorageRectWorld.End.X - 0.6f);
        var zPos = Mathf.Min(startZ + row * spacingZ, StorageRectWorld.End.Y - 0.6f);
        var yPos = 0.02f * (index % 3);
        return new Vector3(xPos, yPos, zPos);
    }

    public Vector3 ClampToPuzzleArea(Vector3 worldPoint)
    {
        var x = Mathf.Clamp(worldPoint.X, PuzzleRectWorld.Position.X, PuzzleRectWorld.End.X);
        var z = Mathf.Clamp(worldPoint.Z, PuzzleRectWorld.Position.Y, PuzzleRectWorld.End.Y);
        return new Vector3(x, worldPoint.Y, z);
    }
}
