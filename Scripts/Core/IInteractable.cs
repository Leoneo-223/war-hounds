// Anything the player can look at and press E on implements this.
// The Interactor raycast only cares about this interface, so pickups,
// stations, crates and doors can all be separate classes.
public interface IInteractable
{
    // Text shown under the crosshair while the player looks at this object.
    // Return "" to show nothing.
    string GetPrompt();
 
    // Called when the player presses E while looking at this object.
    void Interact();
}