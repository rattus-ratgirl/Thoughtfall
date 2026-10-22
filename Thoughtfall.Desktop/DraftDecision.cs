namespace Thoughtfall.Desktop;

/// <summary>The user's choice for an unsaved draft when quitting.</summary>
internal enum DraftDecision
{
    /// <summary>Keep the draft and remain open.</summary>
    Cancel,

    /// <summary>Quit without saving the draft.</summary>
    Discard,

    /// <summary>Save the draft before quitting.</summary>
    Save,
}
