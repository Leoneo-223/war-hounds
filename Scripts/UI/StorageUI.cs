using Godot;
using System.Collections.Generic;

// The drag-and-drop window: the container on one side, the player's inventory on the other.
// One instance lives in the game (CanvasLayer). Crates, racks and machines call Open().
public partial class StorageUI : CanvasLayer
{
    public static StorageUI Instance { get; private set; }

    [Export] public Control Root;              
    [Export] public Label ContainerTitle;      
    [Export] public Texture2D SlotTexture; 
    [Export] public GridContainer ContainerGrid;
    [Export] public GridContainer InventoryGrid;

    private Storage _container;
    private readonly List<SlotUI> _slotUIs = new();

    public override void _Ready()
    {
        Instance = this;
        Root.Visible = false;
    }

    public override void _ExitTree()
    {
        // Make sure a closed scene never leaves the game stuck in "menu open" mode.
        if (Instance == this) GameState.UiOpen = false;
    }

    public void Open(Storage container, string title)
    {
        if (Root.Visible || container == null) return;
        _container = container;
        ContainerTitle.Text = title;

        BuildSlots(ContainerGrid, container);
        BuildSlots(InventoryGrid, Inventory.Instance);

        // Redraw the slots whenever either storage changes.
        container.Changed += RefreshAll;
        Inventory.Instance.Changed += RefreshAll;

        GameState.UiOpen = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        Root.Visible = true;
        RefreshAll();
    }

    public void Close()
    {
        if (!Root.Visible) return;

        GetViewport().GuiCancelDrag();   // drop any drag still in progress

        _container.Changed -= RefreshAll;
        Inventory.Instance.Changed -= RefreshAll;
        _container = null;

        ClearSlots(ContainerGrid);
        ClearSlots(InventoryGrid);
        _slotUIs.Clear();

        Root.Visible = false;
        GameState.UiOpen = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    // E, Tab or Escape close the window.
    public override void _Input(InputEvent e)
    {
        if (!Root.Visible) return;

        if (e is InputEventKey { Pressed: true, Echo: false } k &&
            (k.PhysicalKeycode == Key.E || k.PhysicalKeycode == Key.Tab || k.PhysicalKeycode == Key.Escape))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildSlots(GridContainer grid, Storage storage)
    {
        for (int i = 0; i < storage.SlotCount; i++)
        {
            var slot = new SlotUI { Source = storage, Index = i, Background = SlotTexture };
            grid.AddChild(slot);    // _Ready runs here and builds the slot visuals
            _slotUIs.Add(slot);
        }
    }

    private static void ClearSlots(GridContainer grid)
    {
        foreach (Node child in grid.GetChildren())
        {
            grid.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void RefreshAll()
    {
        foreach (var slot in _slotUIs) slot.Refresh();
    }
}