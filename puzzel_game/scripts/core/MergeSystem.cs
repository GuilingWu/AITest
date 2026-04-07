using System.Collections.Generic;
using Godot;

public partial class MergeSystem : Node
{
    [Export] public float MergeDistanceThreshold { get; set; } = 0.2f;
    [Export] public NodePath GroupsRootPath { get; set; } = new("SafeArea/RootColumn/PuzzleViewportContainer/PuzzleViewport/PuzzleRoot/GroupsRoot");

    private int _nextGroupId = 1;

    public CombinedGroup? TryMerge(Node3D candidate)
    {
        if (candidate is Piece piece)
        {
            return TryMergePiece(piece);
        }

        if (candidate is CombinedGroup group)
        {
            foreach (var member in group.Pieces)
            {
                var merged = TryMergePiece(member);
                if (merged != null)
                {
                    return merged;
                }
            }
        }

        return null;
    }

    public List<Piece> FindMergeTargets(Node3D candidate)
    {
        var targets = new List<Piece>();
        var tree = candidate.GetTree();
        if (tree?.CurrentScene == null)
        {
            return targets;
        }

        CollectPieces(tree.CurrentScene, targets);
        if (candidate is Piece piece)
        {
            targets.Remove(piece);
        }

        return targets;
    }

    public bool CanMerge(Piece a, Piece b)
    {
        return a.CanMergeWith(b, MergeDistanceThreshold);
    }

    public CombinedGroup ExecuteMerge(Piece a, Piece b)
    {
        CombinedGroup? targetGroup = null;
        var groupA = a.GetParent() as CombinedGroup;
        var groupB = b.GetParent() as CombinedGroup;

        if (groupA != null)
        {
            targetGroup = groupA;
        }
        else if (groupB != null)
        {
            targetGroup = groupB;
        }

        if (targetGroup == null)
        {
            targetGroup = new CombinedGroup();
            targetGroup.Initialize(_nextGroupId++);
            var groupsRoot = GetNodeOrNull<Node>(GroupsRootPath);
            (groupsRoot ?? a.GetParent())?.AddChild(targetGroup);
        }

        if (groupA != null && groupB != null && groupA != groupB)
        {
            targetGroup = groupA;
            targetGroup.AbsorbGroup(groupB);
        }

        targetGroup.AddPiece(a);
        targetGroup.AddPiece(b);
        return targetGroup;
    }

    private CombinedGroup? TryMergePiece(Piece piece)
    {
        foreach (var target in FindMergeTargets(piece))
        {
            if (!CanMerge(piece, target))
            {
                continue;
            }

            return ExecuteMerge(piece, target);
        }

        return null;
    }

    private void CollectPieces(Node node, List<Piece> pieces)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Piece piece)
            {
                pieces.Add(piece);
            }

            CollectPieces(child, pieces);
        }
    }
}
