using System;
using System.Collections.Generic;
using Godot;

public static class PieceShapeGeometry
{
    public const float CellSize = 1.0f;
    public const int EdgeSegments = 10;
    public const float TabWidthRatio = 0.42f;
    public const float TabDepthRatio = 0.18f;

    public static List<Vector2> BuildOutline(PieceDescriptor descriptor)
    {
        var halfSize = CellSize * 0.5f;
        var points = new List<Vector2>();

        AppendHorizontalEdge(points, -halfSize, halfSize, -halfSize, descriptor.TopEdge, -1.0f);
        AppendVerticalEdge(points, -halfSize, halfSize, halfSize, descriptor.RightEdge, 1.0f);
        AppendHorizontalEdge(points, halfSize, -halfSize, halfSize, descriptor.BottomEdge, 1.0f);
        AppendVerticalEdge(points, halfSize, -halfSize, -halfSize, descriptor.LeftEdge, -1.0f);

        return points;
    }

    public static Rect2 GetBounds(List<Vector2> outline)
    {
        if (outline.Count == 0)
        {
            return new Rect2();
        }

        var minX = outline[0].X;
        var maxX = outline[0].X;
        var minY = outline[0].Y;
        var maxY = outline[0].Y;

        foreach (var point in outline)
        {
            minX = Mathf.Min(minX, point.X);
            maxX = Mathf.Max(maxX, point.X);
            minY = Mathf.Min(minY, point.Y);
            maxY = Mathf.Max(maxY, point.Y);
        }

        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }

    public static float GetTabDepth()
    {
        return CellSize * TabDepthRatio;
    }

    public static PieceEdgeShape GetDeterministicEdge(int row, int col, int salt)
    {
        var hash = row * 92821 + col * 68917 + salt * 2971;
        return (hash & 1) == 0 ? PieceEdgeShape.Outward : PieceEdgeShape.Inward;
    }

    public static PieceEdgeShape Opposite(PieceEdgeShape edgeShape)
    {
        return edgeShape switch
        {
            PieceEdgeShape.Outward => PieceEdgeShape.Inward,
            PieceEdgeShape.Inward => PieceEdgeShape.Outward,
            _ => PieceEdgeShape.Flat,
        };
    }

    public static float SignedArea(List<Vector2> polygon)
    {
        var area = 0.0f;
        for (var i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];
            var next = polygon[(i + 1) % polygon.Count];
            area += current.X * next.Y - next.X * current.Y;
        }

        return area * 0.5f;
    }

    private static void AppendHorizontalEdge(
        List<Vector2> points,
        float startX,
        float endX,
        float y,
        PieceEdgeShape edgeShape,
        float outwardSign)
    {
        var direction = MathF.Sign(endX - startX);
        var tabWidth = CellSize * TabWidthRatio;
        var tabDepth = GetTabDepth() * outwardSign * (int)edgeShape;
        var centerX = (startX + endX) * 0.5f;
        var tabStart = centerX - tabWidth * 0.5f * direction;
        var tabEnd = centerX + tabWidth * 0.5f * direction;

        AddPoint(points, new Vector2(startX, y));
        AddPoint(points, new Vector2(tabStart, y));

        if (edgeShape != PieceEdgeShape.Flat)
        {
            for (var segment = 1; segment < EdgeSegments; segment++)
            {
                var t = segment / (float)EdgeSegments;
                var x = Mathf.Lerp(tabStart, tabEnd, t);
                var offset = MathF.Sin(t * Mathf.Pi) * tabDepth;
                AddPoint(points, new Vector2(x, y + offset));
            }
        }

        AddPoint(points, new Vector2(tabEnd, y));
        AddPoint(points, new Vector2(endX, y));
    }

    private static void AppendVerticalEdge(
        List<Vector2> points,
        float startY,
        float endY,
        float x,
        PieceEdgeShape edgeShape,
        float outwardSign)
    {
        var direction = MathF.Sign(endY - startY);
        var tabWidth = CellSize * TabWidthRatio;
        var tabDepth = GetTabDepth() * outwardSign * (int)edgeShape;
        var centerY = (startY + endY) * 0.5f;
        var tabStart = centerY - tabWidth * 0.5f * direction;
        var tabEnd = centerY + tabWidth * 0.5f * direction;

        AddPoint(points, new Vector2(x, startY));
        AddPoint(points, new Vector2(x, tabStart));

        if (edgeShape != PieceEdgeShape.Flat)
        {
            for (var segment = 1; segment < EdgeSegments; segment++)
            {
                var t = segment / (float)EdgeSegments;
                var y = Mathf.Lerp(tabStart, tabEnd, t);
                var offset = MathF.Sin(t * Mathf.Pi) * tabDepth;
                AddPoint(points, new Vector2(x + offset, y));
            }
        }

        AddPoint(points, new Vector2(x, tabEnd));
        AddPoint(points, new Vector2(x, endY));
    }

    private static void AddPoint(List<Vector2> points, Vector2 point)
    {
        if (points.Count == 0 || points[^1].DistanceTo(point) > 0.0001f)
        {
            points.Add(point);
        }
    }
}
