using Godot;

public partial class Piece : Node3D
{
    public PieceDescriptor? Descriptor { get; private set; }
    public int CurrentQuarterTurns { get; private set; }
    public PieceArea CurrentArea { get; private set; } = PieceArea.Unknown;
    public int GroupId { get; private set; } = -1;

    private MeshInstance3D? _meshInstance;
    private Area3D? _area3D;

    public override void _Ready()
    {
        EnsureChildNodes();
    }

    public void Initialize(PieceDescriptor descriptor, Material material)
    {
        Descriptor = descriptor;
        EnsureChildNodes();

        if (_meshInstance != null)
        {
            _meshInstance.Mesh = new BoxMesh
            {
                Size = new Vector3(1.0f, 0.2f, 1.0f),
            };
            _meshInstance.MaterialOverride = material;
        }

        Position = descriptor.SolvedLocalPosition;
        CurrentQuarterTurns = 0;
        CurrentArea = PieceArea.Storage;
    }

    public void RotateClockwise()
    {
        CurrentQuarterTurns = (CurrentQuarterTurns + 1) % 4;
        RotateY(Mathf.Pi * -0.5f);
    }

    public void SetDragState(bool dragging)
    {
        Scale = dragging ? Vector3.One * 1.03f : Vector3.One;
    }

    public Vector3 GetSolvedWorldPosition()
    {
        return Descriptor?.SolvedLocalPosition ?? GlobalPosition;
    }

    public bool CanMergeWith(Piece other, float positionThreshold = 0.2f)
    {
        if (Descriptor == null || other.Descriptor == null)
        {
            return false;
        }

        if (CurrentQuarterTurns != other.CurrentQuarterTurns)
        {
            return false;
        }

        var gridDelta = Descriptor.GridIndex - other.Descriptor.GridIndex;
        var adjacent = Mathf.Abs(gridDelta.X) + Mathf.Abs(gridDelta.Y) == 1;
        if (!adjacent)
        {
            return false;
        }

        var expectedDelta = GetSolvedWorldPosition() - other.GetSolvedWorldPosition();
        var currentDelta = GlobalPosition - other.GlobalPosition;
        return currentDelta.DistanceTo(expectedDelta) <= positionThreshold;
    }

    public void SetArea(PieceArea area)
    {
        CurrentArea = area;
    }

    public void SetGroup(int groupId)
    {
        GroupId = groupId;
    }

    private void EnsureChildNodes()
    {
        _meshInstance ??= GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
        _area3D ??= GetNodeOrNull<Area3D>("Area3D");

        if (_meshInstance == null)
        {
            _meshInstance = new MeshInstance3D { Name = "MeshInstance3D" };
            AddChild(_meshInstance);
        }

        if (_area3D == null)
        {
            _area3D = new Area3D { Name = "Area3D" };
            AddChild(_area3D);

            var collisionShape = new CollisionShape3D
            {
                Name = "CollisionShape3D",
                Shape = new BoxShape3D { Size = new Vector3(1.0f, 0.2f, 1.0f) },
            };
            _area3D.AddChild(collisionShape);
        }

        _area3D.CollisionLayer = 1u << 1;
        _area3D.CollisionMask = 1u << 1;
        _area3D.InputRayPickable = true;
        _area3D.Monitoring = false;
        _area3D.Monitorable = true;
    }
}