using System;
using System.Collections.Generic;
using Godot;

public partial class PieceFactory : Node
{
    public List<PieceDescriptor> BuildDescriptors(Texture2D texture, PuzzleConfig config)
    {
        var descriptors = new List<PieceDescriptor>();
        var cellWidth = PieceShapeGeometry.CellSize;
        var cellHeight = PieceShapeGeometry.CellSize;
        var startX = -((config.Columns - 1) * cellWidth) * 0.5f;
        var startZ = -((config.Rows - 1) * cellHeight) * 0.5f;

        var horizontalEdges = new PieceEdgeShape[Math.Max(0, config.Rows - 1), config.Columns];
        var verticalEdges = new PieceEdgeShape[config.Rows, Math.Max(0, config.Columns - 1)];

        for (var row = 0; row < config.Rows - 1; row++)
        {
            for (var col = 0; col < config.Columns; col++)
            {
                horizontalEdges[row, col] = PieceShapeGeometry.GetDeterministicEdge(row, col, 17);
            }
        }

        for (var row = 0; row < config.Rows; row++)
        {
            for (var col = 0; col < config.Columns - 1; col++)
            {
                verticalEdges[row, col] = PieceShapeGeometry.GetDeterministicEdge(row, col, 53);
            }
        }

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
                    TopEdge = row == 0 ? PieceEdgeShape.Flat : PieceShapeGeometry.Opposite(horizontalEdges[row - 1, col]),
                    RightEdge = col == config.Columns - 1 ? PieceEdgeShape.Flat : verticalEdges[row, col],
                    BottomEdge = row == config.Rows - 1 ? PieceEdgeShape.Flat : horizontalEdges[row, col],
                    LeftEdge = col == 0 ? PieceEdgeShape.Flat : PieceShapeGeometry.Opposite(verticalEdges[row, col - 1]),
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

        var mesh = CreatePieceMesh(descriptor, config);
        var material = CreatePuzzleMaterial(texture);
        var pickBoundsSize = PieceShapeGeometry.CellSize + PieceShapeGeometry.GetTabDepth() * 2.0f;
        piece.Initialize(descriptor, mesh, material, config.Thickness, pickBoundsSize);
        return piece;
    }

    public Material CreatePuzzleMaterial(Texture2D texture)
    {
        return new StandardMaterial3D
        {
            AlbedoTexture = texture,
            Roughness = 0.92f,
            Metallic = 0.0f,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
            CullMode = BaseMaterial3D.CullModeEnum.Back,
        };
    }

    private Mesh CreatePieceMesh(PieceDescriptor descriptor, PuzzleConfig config)
    {
        var outline = PieceShapeGeometry.BuildOutline(descriptor);
        if (PieceShapeGeometry.SignedArea(outline) < 0.0f)
        {
            outline.Reverse();
        }

        var indices = Geometry2D.TriangulatePolygon(outline.ToArray());
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        var halfThickness = config.Thickness * 0.5f;
        var topY = halfThickness;
        var bottomY = -halfThickness;

        for (var i = 0; i < indices.Length; i += 3)
        {
            var a = outline[indices[i]];
            var b = outline[indices[i + 1]];
            var c = outline[indices[i + 2]];

            AddTriangle(surfaceTool, descriptor, topY, a, b, c);
            AddTriangle(surfaceTool, descriptor, bottomY, c, b, a);
        }

        for (var i = 0; i < outline.Count; i++)
        {
            var current = outline[i];
            var next = outline[(i + 1) % outline.Count];

            AddQuad(surfaceTool, descriptor, current, next, topY, bottomY);
        }

        surfaceTool.GenerateNormals();
        return surfaceTool.Commit();
    }

    private static void AddTriangle(SurfaceTool surfaceTool, PieceDescriptor descriptor, float y, Vector2 a, Vector2 b, Vector2 c)
    {
        AddVertex(surfaceTool, descriptor, new Vector3(a.X, y, a.Y));
        AddVertex(surfaceTool, descriptor, new Vector3(b.X, y, b.Y));
        AddVertex(surfaceTool, descriptor, new Vector3(c.X, y, c.Y));
    }

    private static void AddQuad(
        SurfaceTool surfaceTool,
        PieceDescriptor descriptor,
        Vector2 current,
        Vector2 next,
        float topY,
        float bottomY)
    {
        var topCurrent = new Vector3(current.X, topY, current.Y);
        var topNext = new Vector3(next.X, topY, next.Y);
        var bottomNext = new Vector3(next.X, bottomY, next.Y);
        var bottomCurrent = new Vector3(current.X, bottomY, current.Y);

        AddVertex(surfaceTool, descriptor, topCurrent);
        AddVertex(surfaceTool, descriptor, topNext);
        AddVertex(surfaceTool, descriptor, bottomNext);

        AddVertex(surfaceTool, descriptor, topCurrent);
        AddVertex(surfaceTool, descriptor, bottomNext);
        AddVertex(surfaceTool, descriptor, bottomCurrent);
    }

    private static void AddVertex(SurfaceTool surfaceTool, PieceDescriptor descriptor, Vector3 vertex)
    {
        var uv = new Vector2(
            descriptor.UvRect.Position.X + (descriptor.GridIndex.X + 0.5f + vertex.X / PieceShapeGeometry.CellSize) * descriptor.UvRect.Size.X,
            descriptor.UvRect.Position.Y + (descriptor.GridIndex.Y + 0.5f + vertex.Z / PieceShapeGeometry.CellSize) * descriptor.UvRect.Size.Y);
        surfaceTool.SetUV(uv);
        surfaceTool.AddVertex(vertex);
    }
}
