using Godot;

// One draggable slot. Created from code by StorageUI, so it needs no scene.
// It draws itself (background, icon, count) and handles drag and drop
// using Godot's built-in _GetDragData / _CanDropData / _DropData.
// Hovering an item shows a tooltip with its name, category and description.
public partial class SlotUI : Control
{
    public Storage Source;   // which storage this slot shows
    public int Index;        // which slot of that storage

    [Export] public TextureRect _icon;
    public Texture2D Background;   // set by StorageUI; null = plain dark square
    private Label _count;
    private Label _nameFallback;   // shown if the item has no icon yet

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(64, 64);
        MouseFilter = MouseFilterEnum.Stop;   // this node must receive the mouse

        // Children ignore the mouse, otherwise they would steal drags and drops.
        Control bg;
        if (Background != null)
        {
            TextureRect backgroundImage = new TextureRect();
            backgroundImage.Texture = Background;
            backgroundImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            backgroundImage.StretchMode = TextureRect.StretchModeEnum.Scale;
            backgroundImage.MouseFilter = MouseFilterEnum.Ignore;
            bg = backgroundImage;
        }
        else
        {
            ColorRect backgroundColor = new ColorRect();
            backgroundColor.Color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            backgroundColor.MouseFilter = MouseFilterEnum.Ignore;
            bg = backgroundColor;
        }

        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        _icon = new TextureRect();
        _icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _icon.MouseFilter = MouseFilterEnum.Ignore;
        _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 6);
        AddChild(_icon);

        _nameFallback = new Label();
        _nameFallback.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _nameFallback.HorizontalAlignment = HorizontalAlignment.Center;
        _nameFallback.VerticalAlignment = VerticalAlignment.Center;
        _nameFallback.MouseFilter = MouseFilterEnum.Ignore;
        _nameFallback.AddThemeFontSizeOverride("font_size", 11);
        _nameFallback.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 4);
        AddChild(_nameFallback);

        _count = new Label();
        _count.HorizontalAlignment = HorizontalAlignment.Right;
        _count.VerticalAlignment = VerticalAlignment.Bottom;
        _count.MouseFilter = MouseFilterEnum.Ignore;
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

        // Name of the item, or "" when the slot is empty.
        string itemName = "";
        if (item != null && item.Name != null)
        {
            itemName = item.Name;
        }

        // Icon
        if (item != null)
        {
            _icon.Texture = item.Icon;
        }
        else
        {
            _icon.Texture = null;
        }
        _icon.Visible = item != null && item.Icon != null;

        // Name shown on the slot only when the item has no icon
        _nameFallback.Text = itemName;
        _nameFallback.Visible = item != null && item.Icon == null;

        // Stack number
        if (item != null && count > 1)
        {
            _count.Text = count.ToString();
        }
        else
        {
            _count.Text = "";
        }

        // Must be non-empty for the tooltip to show at all. The text itself is only
        // used as a fallback, because _MakeCustomTooltip below builds the real one.
        TooltipText = itemName;
    }

    // Builds the hover tooltip from the item in this slot. Godot calls this
    // each time the cursor rests on the slot, so it is always up to date.
    public override GodotObject _MakeCustomTooltip(string forText)
    {
        if (Source == null)
        {
            return null;
        }

        ItemData item = Source.Items[Index];
        if (item == null)
        {
            return null;
        }

        VBoxContainer box = new VBoxContainer();
        box.CustomMinimumSize = new Vector2(220, 0);

        Label nameLabel = new Label();
        nameLabel.Text = item.Name;
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        box.AddChild(nameLabel);

        Label category = new Label();
        category.Text = item.Category.ToString();
        category.Modulate = new Color(1, 1, 1, 0.6f);
        category.AddThemeFontSizeOverride("font_size", 11);
        box.AddChild(category);

        if (!string.IsNullOrEmpty(item.Desc))
        {
            Label description = new Label();
            description.Text = item.Desc;
            description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            description.CustomMinimumSize = new Vector2(220, 0);
            box.AddChild(description);
        }

        MarginContainer margin = new MarginContainer();
        string[] sides = new string[] { "left", "right", "top", "bottom" };
        foreach (string side in sides)
        {
            margin.AddThemeConstantOverride($"margin_{side}", 8);
        }
        margin.AddChild(box);

        PanelContainer panel = new PanelContainer();
        panel.AddChild(margin);
        return panel;
    }

    // Called by Godot when the player starts dragging from this slot.
    // Returning an empty Variant means "nothing to drag".
    public override Variant _GetDragData(Vector2 atPosition)
    {
        ItemData item = Source.Items[Index];
        if (item == null)
        {
            return default;
        }

        // Hold Shift while dragging to pick up a single item instead of the whole stack.
        int amount = Source.Counts[Index];
        if (Input.IsPhysicalKeyPressed(Key.Shift))
        {
            amount = 1;
        }

        // The preview that follows the cursor, centered on it.
        Control visual;
        if (item.Icon != null)
        {
            TextureRect iconPreview = new TextureRect();
            iconPreview.Texture = item.Icon;
            iconPreview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            iconPreview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            iconPreview.Size = new Vector2(48, 48);
            iconPreview.Position = new Vector2(-24, -24);
            iconPreview.Modulate = new Color(1, 1, 1, 0.8f);
            visual = iconPreview;
        }
        else
        {
            Label namePreview = new Label();
            namePreview.Text = item.Name;
            namePreview.Position = new Vector2(-24, -10);
            visual = namePreview;
        }

        Control preview = new Control();
        preview.AddChild(visual);
        SetDragPreview(preview);

        SlotDragData dragData = new SlotDragData();
        dragData.From = Source;
        dragData.FromSlot = Index;
        dragData.Amount = amount;
        return Variant.From(dragData);
    }

    // Called constantly while hovering with a drag. Decides if the slot highlights as a valid target.
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Object)
        {
            return false;
        }

        SlotDragData d = data.AsGodotObject() as SlotDragData;
        if (d == null)
        {
            return false;
        }

        return Storage.Transfer(d.From, d.FromSlot, Source, Index, d.Amount, true);
    }

    // Called when the mouse is released over this slot.
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        SlotDragData d = data.AsGodotObject() as SlotDragData;
        if (d != null)
        {
            Storage.Transfer(d.From, d.FromSlot, Source, Index, d.Amount);
        }
    }
}
