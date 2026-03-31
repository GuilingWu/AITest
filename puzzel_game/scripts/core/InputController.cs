using Godot;

public partial class InputController : Node
{
    [Signal] public delegate void InteractionCommittedEventHandler();

    [Export] public NodePath CameraPath { get; set; } = new("SafeArea/RootColumn/PuzzleViewportContainer/PuzzleViewport/PuzzleRoot/Camera3D");
    [Export] public NodePath AreaManagerPath { get; set; } = new("../AreaManager");
    [Export] public NodePath MergeSystemPath { get; set; } = new("../MergeSystem");

    private Camera3D? _camera;
    private AreaManager? _areaManager;
    private MergeSystem? _mergeSystem;
    private Node3D? _selectedNode;
    private Vector2 _pressPosition;
    private ulong _pressTime;
    private bool _dragging;
    private Vector3 _dragOffset = Vector3.Zero;

    public override void _Ready()
    {
        ResolveDependencies();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventScreenTouch touch:
                HandleTouch(touch.Position, touch.Pressed);
                break;
            case InputEventMouseButton mouseButton when mouseButton.ButtonIndex == MouseButton.Left:
                HandleTouch(mouseButton.Position, mouseButton.Pressed);
                break;
            case InputEventScreenDrag drag:
                HandleDrag(drag.Position);
                break;
            case InputEventMouseMotion mouseMotion when Input.IsMouseButtonPressed(MouseButton.Left):
                HandleDrag(mouseMotion.Position);
                break;
        }
    }

    private void ResolveDependencies()
    {
        _camera ??= GetNodeOrNull<Camera3D>(CameraPath);
        _areaManager ??= GetNodeOrNull<AreaManager>(AreaManagerPath);
        _mergeSystem ??= GetNodeOrNull<MergeSystem>(MergeSystemPath);
    }

    private void HandleTouch(Vector2 position, bool pressed)
    {
        ResolveDependencies();

        if (pressed)
        {
            _pressPosition = position;
            _pressTime = Time.GetTicksMsec();
            _selectedNode = PickNode(position);
            _dragging = false;

            if (_selectedNode != null && TryProject(position, out var worldPoint))
            {
                _dragOffset = _selectedNode.GlobalPosition - worldPoint;
                SetDragState(_selectedNode, true);
            }

            return;
        }

        if (_selectedNode == null)
        {
            return;
        }

        var selected = _selectedNode;
        var duration = Time.GetTicksMsec() - _pressTime;
        var moved = position.DistanceTo(_pressPosition);

        if (!_dragging && duration < 180 && moved < 12.0f)
        {
            RotateNode(selected);
        }
        else
        {
            FinalizeDrop(selected);
        }

        SetDragState(selected, false);
        _selectedNode = null;
        _dragging = false;
        EmitSignal(SignalName.InteractionCommitted);
    }

    private void HandleDrag(Vector2 position)
    {
        if (_selectedNode == null)
        {
            return;
        }

        if (!TryProject(position, out var worldPoint))
        {
            return;
        }

        _dragging = true;
        _selectedNode.GlobalPosition = worldPoint + _dragOffset;
    }

    private bool TryProject(Vector2 screenPosition, out Vector3 worldPoint)
    {
        ResolveDependencies();
        if (_areaManager == null)
        {
            worldPoint = Vector3.Zero;
            return false;
        }

        return _areaManager.TryProjectPointerToBoard(screenPosition, out worldPoint);
    }

    private Node3D? PickNode(Vector2 screenPosition)
    {
        ResolveDependencies();
        if (_camera == null || _camera.GetWorld3D() == null)
        {
            return null;
        }

        var origin = _camera.ProjectRayOrigin(screenPosition);
        var end = origin + _camera.ProjectRayNormal(screenPosition) * 1000.0f;
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        var result = _camera.GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (result.Count == 0 || !result.ContainsKey("collider"))
        {
            return null;
        }

        var collider = result["collider"].AsGodotObject() as Node;
        return FindSelectableNode(collider);
    }

    private static Node3D? FindSelectableNode(Node? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is CombinedGroup group)
            {
                return group;
            }

            if (current is Piece piece)
            {
                if (piece.GetParent() is CombinedGroup parentGroup)
                {
                    return parentGroup;
                }

                return piece;
            }

            current = current.GetParent();
        }

        return null;
    }

    private void RotateNode(Node3D node)
    {
        if (node is Piece piece)
        {
            piece.RotateClockwise();
            _mergeSystem?.TryMerge(piece);
            return;
        }

        if (node is CombinedGroup group)
        {
            group.RotateClockwise();
            _mergeSystem?.TryMerge(group);
        }
    }

    private void FinalizeDrop(Node3D node)
    {
        ResolveDependencies();
        if (_areaManager == null)
        {
            return;
        }

        if (_areaManager.IsInsidePuzzleArea(node.GlobalPosition))
        {
            node.GlobalPosition = _areaManager.ClampToPuzzleArea(node.GlobalPosition);
            TrySnapNode(node);
            ApplyArea(node, PieceArea.Puzzle);
        }
        else
        {
            ApplyArea(node, PieceArea.Storage);
        }

        _mergeSystem?.TryMerge(node);
    }


    private static void TrySnapNode(Node3D node)
    {
        if (node is Piece piece)
        {
            var solved = piece.GetSolvedWorldPosition();
            if (piece.GlobalPosition.DistanceTo(solved) <= 0.75f)
            {
                piece.GlobalPosition = solved;
            }

            return;
        }

        if (node is CombinedGroup group)
        {
            foreach (var childPiece in group.Pieces)
            {
                var solved = childPiece.GetSolvedWorldPosition();
                if (childPiece.GlobalPosition.DistanceTo(solved) <= 0.75f)
                {
                    childPiece.GlobalPosition = solved;
                }
            }
        }
    }
    private static void ApplyArea(Node3D node, PieceArea area)
    {
        if (node is Piece piece)
        {
            piece.SetArea(area);
            return;
        }

        if (node is CombinedGroup group)
        {
            foreach (var childPiece in group.Pieces)
            {
                childPiece.SetArea(area);
            }
        }
    }

    private static void SetDragState(Node3D node, bool dragging)
    {
        if (node is Piece piece)
        {
            piece.SetDragState(dragging);
            return;
        }

        if (node is CombinedGroup group)
        {
            foreach (var childPiece in group.Pieces)
            {
                childPiece.SetDragState(dragging);
            }
        }
    }
}