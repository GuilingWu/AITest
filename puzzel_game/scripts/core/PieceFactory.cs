using System.Collections.Generic;
using Godot;

public partial class PieceFactory : Node
{
    public List<PieceDescriptor> BuildDescriptors(Texture2D texture, PuzzleConfig config)
    {
        var descriptors = new List<PieceDescriptor>();
        var cellWidth = 1.0f;
        var cellHeight = 1.0f;
        var startX = -((config.Columns - 1) * cellWidth) * 0.5f;
        var startZ = -((config.Rows - 1) * cellHeight) * 0.5f;

        var pieceId = 0;
        for (var row = 0; row < config.Rows; row++)
        {
            for (var col = 0; col < config.Columns; col++)
            {
                descriptors.Add(new PieceDescriptor
                {
                    PieceId = pieceId++,
                    GridIndex = new Vector2I(col, row),
                    UvRect = new Rect2(
                        (float)col / config.Columns,
                        (float)row / config.Rows,
                        1.0f / config.Columns,
                        1.0f / config.Rows),
                    SolvedLocalPosition = new Vector3(startX + col * cellWidth, 0.0f, startZ + row * cellHeight),
                });
            }
        }

        return descriptors;
    }

    public Piece CreatePieceNode(PieceDescriptor descriptor, Texture2D texture, PuzzleConfig config)
    {
        var piece = new Piece
        {
            Name = $"Piece_{descriptor.PieceId}",
        };
        var material = CreatePuzzleMaterial(descriptor.UvRect, texture);
        piece.Initialize(descriptor, material);
        return piece;
    }

    public Material CreatePuzzleMaterial(Rect2 uvRect, Texture2D texture)
    {
        return new StandardMaterial3D
        {
            AlbedoTexture = texture,
            Roughness = 0.9f,
        };
    }
}