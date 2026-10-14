namespace Puzzle.Player
{
    /// <summary>
    /// What the mouse does right now (selecting, placing a building...). PlayerController holds exactly
    /// one active mode and forwards input to it. Add a new tool by adding a new mode.
    /// </summary>
    public interface IInteractionMode
    {
        /// <summary>Called when this mode becomes active.</summary>
        void Enter();

        /// <summary>Called when another mode takes over. Clean up anything shown (highlights, ghosts).</summary>
        void Exit();

        /// <summary>Called every frame while active.</summary>
        void Update(in PointerInfo pointer);

        /// <summary>Select (left click) pressed over the world.</summary>
        void OnSelectPressed(in PointerInfo pointer);

        /// <summary>Select released (anywhere).</summary>
        void OnSelectReleased(in PointerInfo pointer);

        /// <summary>Cancel (right click / Escape) pressed.</summary>
        void OnCancel();
    }
}
