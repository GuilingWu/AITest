using System.Collections.Generic;
using Godot;

public partial class ControlPanel : Control
{
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

        foreach (var piece in pieces)
        {
            items.AddChild(CreatePiecePreview(piece, texture));
        }

        var itemWidth = 106.0f;
        items.CustomMinimumSize = new Vector2(Mathf.Max(0.0f, pieces.Count * itemWidth), 96.0f);
    }

    private static Panel CreatePiecePreview(Piece piece, Texture2D texture)
    {
        var panel = new Panel
        {
            CustomMinimumSize = new Vector2(96, 96),
            Modulate = piece.CurrentArea == PieceArea.Puzzle ? new Color(1f, 1f, 1f, 0.35f) : Colors.White,
        };

        var preview = new TextureRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
        };

        if (piece.Descriptor != null)
        {
            var textureSize = texture.GetSize();
            preview.Texture = new AtlasTexture
            {
                Atlas = texture,
                Region = new Rect2(
                    piece.Descriptor.UvRect.Position.X * textureSize.X,
                    piece.Descriptor.UvRect.Position.Y * textureSize.Y,
                    piece.Descriptor.UvRect.Size.X * textureSize.X,
                    piece.Descriptor.UvRect.Size.Y * textureSize.Y),
            };
        }

        panel.AddChild(preview);
        return panel;
    }
}
