using System.Collections.Generic;
using Godot;

public partial class CombinedGroup : Node3D
{
    public int GroupId { get; private set; } = -1;
    public List<Piece> Pieces { get; } = new();
    public int CurrentQuarterTurns { get; private set; }

    public void Initialize(int groupId)
    {
        GroupId = groupId;
        Name = $"CombinedGroup_{groupId}";
    }

    public void AddPiece(Piece piece)
    {
        if (Pieces.Contains(piece))
        {
            return;
        }

        Pieces.Add(piece);
        piece.Reparent(this);
        piece.SetGroup(GroupId);
        RefreshLocalLayout();
    }

    public void AbsorbGroup(CombinedGroup other)
    {
        var absorbedPieces = new List<Piece>(other.Pieces);
        foreach (var piece in absorbedPieces)
        {
            AddPiece(piece);
        }

        other.Pieces.Clear();
        other.QueueFree();
    }

    public void RotateClockwise()
    {
        CurrentQuarterTurns = (CurrentQuarterTurns + 1) % 4;
        RotateY(Mathf.Pi * -0.5f);
    }

    public void RefreshLocalLayout()
    {
        if (Pieces.Count == 0)
        {
            return;
        }

        var anchor = Pieces[0].GetSolvedWorldPosition();
        GlobalPosition = anchor;

        foreach (var piece in Pieces)
        {
            piece.Position = piece.GetSolvedWorldPosition() - anchor;
        }
    }
}
