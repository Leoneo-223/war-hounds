using Godot;

// One draggable slot. Created from code by StorageUI, so it needs no scene.
// It draws itself (background, icon, count) and handles drag and drop
// using Godot's built-in _GetDragData / _CanDropData / _DropData.
public partial class SlotUI : Control
{
    public Storage Source;   // which storage this slot shows
    public int Index;        // which slot of that storage

    [Export] public TextureRect _icon;
    private Label _count;
    private Label _nameFallback;   // shown if the item has no icon yet

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(64, 64);
        MouseFilter = MouseFilterEnum.Stop;   // this node must receive the mouse

        // Children ignore the mouse, otherwise they would steal drags and drops.
        var bg = new ColorRect { Color = new Color(0.12f, 0.12f, 0.12f, 0.9f), MouseFilter = MouseFilterEnum.Ignore };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        _icon = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 6);
        AddChild(_icon);

        _nameFallback = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _nameFallback.AddThemeFontSizeOverride("font_size", 11);
        _nameFallback.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 4);
        AddChild(_nameFallback);

        _count = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _count.AddThemeColorOverride("font_outline_color", Colors.Black);
        _count.AddThemeConstantOverride("outline_size", 4);
        _count.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 4);
        AddChild(_count);
    }

    // Redraws this slot from the storage data.
    public void Refresh()
    {
        ItemData item = Source.Items[Index];
        int count = Source.Counts[Index];

        _icon.Texture = item?.Icon;
        _icon.Visible = item?.Icon != null;

        _nameFallback.Text = item?.Name ?? "";
        _nameFallback.Visible = item != null && item.Icon == null;

        _count.Text = (item != null && count > 1) ? count.ToString() : "";
        TooltipText = item?.Name ?? "";
    }

    // Called by Godot when the player starts dragging from this slot.
    // Returning an empty Variant means "nothing to drag".
    public override Variant _GetDragData(Vector2 atPosition)
    {
        ItemData item = Source.Items[Index];
        if (item == null) return default;

        // Hold Shift while dragging to pick up a single item instead of the whole stack.
        int amount = Input.IsPhysicalKeyPressed(Key.Shift) ? 1 : Source.Counts[Index];

        // The preview that follows the cursor, centered on it.
        Control visual;
        if (item.Icon != null)
            visual = new TextureRect
            {
                Texture = item.Icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Size = new Vector2(48, 48),
                Position = new Vector2(-24, -24),
                Modulate = new Color(1, 1, 1, 0.8f)
            };
        else
            visual = new Label { Text = item.Name, Position = new Vector2(-24, -10) };

        var preview = new Control();
        preview.AddChild(visual);
        SetDragPreview(preview);

        return Variant.From(new SlotDragData { From = Source, FromSlot = Index, Amount = amount });
    }

    // Called constantly while hovering with a drag. Decides if the slot highlights as a valid target.
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Object) return false;
        return data.AsGodotObject() is SlotDragData d
            && Storage.Transfer(d.From, d.FromSlot, Source, Index, d.Amount, dryRun: true);
    }

    // Called when the mouse is released over this slot.
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.AsGodotObject() is SlotDragData d)
            Storage.Transfer(d.From, d.FromSlot, Source, Index, d.Amount);
    }
}