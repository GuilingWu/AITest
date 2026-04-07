using System.Collections.Generic;
using Godot;

public partial class ControlPanel : Control
{
    private const int PreviewTextureSize = 144;

    [Signal] public delegate void ExitRequestedEventHandler();

    public override void _Ready()
    {
        var backButton = GetNodeOrNull<Button>("SafeArea/RootColumn/TopBarPanel/TopBar/BackButton")
            ?? GetNodeOrNull<Button>("SafeArea/RootColumn/TopBar/BackButton");
        if (backButton != null)
        {
            backButton.Pressed += () => EmitSignal(SignalName.ExitRequested);
        }
    }

    public void RefreshProgress(int solved, int total)
    {
        var label = GetNodeOrNull<Label>("SafeArea/RootColumn/TopBarPanel/TopBar/ProgressLabel")
            ?? GetNodeOrNull<Label>("SafeArea/RootColumn/TopBar/ProgressLabel");
        if (label != null)
        {
            label.Text = $"Progress {solved} / {total}";
        }
    }

    public void RefreshStars(int stars)
    {
        var label = GetNodeOrNull<Label>("SafeArea/RootColumn/TopBarPanel/TopBar/StarLabel")
            ?? GetNodeOrNull<Label>("SafeArea/RootColumn/TopBar/StarLabel");
        if (label != null)
        {
            label.Text = $"{stars} Stars";
        }
    }

    public void SetStorageMode(StorageMode mode)
    {
        var tabs = GetNodeOrNull<Control>("SafeArea/RootColumn/BottomPanel/BottomColumn/StorageModeTabs");
        var horizontalList = GetNodeOrNull<ScrollContainer>("SafeArea/RootColumn/BottomPanel/BottomColumn/HorizontalPieceList");
        var stackHint = GetNodeOrNull<Control>("SafeArea/RootColumn/BottomPanel/BottomColumn/StackHintOverlay");

        if (tabs != null)
        {
            tabs.Visible = false;
        }

        if (horizontalList != null)
        {
            horizontalList.Visible = mode == StorageMode.HorizontalList;
            horizontalList.Set("horizontal_scroll_mode", 1);
            horizontalList.Set("vertical_scroll_mode", 0);
        }

        if (stackHint != null)
        {
            stackHint.Visible = mode == StorageMode.Stack;
        }
    }

    public void PopulatePieceList(List<Piece> pieces, Texture2D texture)
    {
        var items = GetNodeOrNull<HBoxContainer>("SafeArea/RootColumn/BottomPanel/BottomColumn/HorizontalPieceList/Items");
        if (items == null)
        {
            return;
        }

        foreach (Node child in items.GetChildren())
        {
            child.QueueFree();
        }

        var sourceImage = texture.GetImage();
        foreach (var piece in pieces)
        {
            items.AddChild(CreatePiecePreview(piece, sourceImage));
        }

        var itemWidth = 106.0f;
        items.CustomMinimumSize = new Vector2(Mathf.Max(0.0f, pieces.Count * itemWidth), 96.0f);
    }

    private static Control CreatePiecePreview(Piece piece, Image sourceImage)
    {
        var root = new Control
        {
            CustomMinimumSize = new Vector2(110, 110),
            Modulate = piece.CurrentArea == PieceArea.Puzzle ? new Color(1f, 1f, 1f, 0.35f) : Colors.White,
        };

        var preview = new TextureRect
        {
            Position = new Vector2(7, 7),
            CustomMinimumSize = new Vector2(96, 96),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
        };

        if (piece.Descriptor != null)
        {
            preview.Texture = BuildPiecePreviewTexture(piece.Descriptor, sourceImage);
        }

        root.AddChild(preview);
        return root;
    }

    private static Texture2D BuildPiecePreviewTexture(PieceDescriptor descriptor, Image sourceImage)
    {
        var outline = PieceShapeGeometry.BuildOutline(descriptor);
        var bounds = PieceShapeGeometry.GetBounds(outline);
        var polygon = outline.ToArray();
        var image = Image.CreateEmpty(PreviewTextureSize, PreviewTextureSize, false, Image.Format.Rgba8);
        var textureWidth = sourceImage.GetWidth();
        var textureHeight = sourceImage.GetHeight();

        for (var y = 0; y < PreviewTextureSize; y++)
        {
            for (var x = 0; x < PreviewTextureSize; x++)
            {
                var localX = bounds.Position.X + (x + 0.5f) / PreviewTextureSize * bounds.Size.X;
                var localY = bounds.Position.Y + (y + 0.5f) / PreviewTextureSize * bounds.Size.Y;
                var localPoint = new Vector2(localX, localY);
                if (!Geometry2D.IsPointInPolygon(localPoint, polygon))
                {
                    image.SetPixel(x, y, Colors.Transparent);
                    continue;
                }

                var uv = new Vector2(
                    descriptor.UvRect.Position.X + (descriptor.GridIndex.X + 0.5f + localX / PieceShapeGeometry.CellSize) * descriptor.UvRect.Size.X,
                    descriptor.UvRect.Position.Y + (descriptor.GridIndex.Y + 0.5f + localY / PieceShapeGeometry.CellSize) * descriptor.UvRect.Size.Y);
                uv = uv.Clamp(Vector2.Zero, Vector2.One);

                var sampleX = Mathf.Clamp(Mathf.RoundToInt(uv.X * (textureWidth - 1)), 0, textureWidth - 1);
                var sampleY = Mathf.Clamp(Mathf.RoundToInt(uv.Y * (textureHeight - 1)), 0, textureHeight - 1);
                image.SetPixel(x, y, sourceImage.GetPixel(sampleX, sampleY));
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}
