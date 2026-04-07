using System.Collections.Generic;
using Godot;

public partial class PieceFactory : Node
{
    private static readonly Shader PuzzleShader = new()
    {
        Code = @"
shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

uniform sampler2D albedo_texture : source_color;
uniform vec2 uv_scale = vec2(1.0, 1.0);
uniform vec2 uv_offset = vec2(0.0, 0.0);
uniform float roughness_value = 0.9;

void fragment()
{
    vec2 atlas_uv = UV * uv_scale + uv_offset;
    ALBEDO = texture(albedo_texture, atlas_uv).rgb;
    ROUGHNESS = roughness_value;
}
"
    };

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
        var material = new ShaderMaterial
        {
            Shader = PuzzleShader,
        };
        material.SetShaderParameter("albedo_texture", texture);
        material.SetShaderParameter("uv_scale", new Vector2(uvRect.Size.X, uvRect.Size.Y));
        material.SetShaderParameter("uv_offset", new Vector2(uvRect.Position.X, uvRect.Position.Y));
        material.SetShaderParameter("roughness_value", 0.9f);
        return material;
    }
}
