namespace STS2Advisor.Scripts;

/// <summary>
/// Simple mutex for UI drag so overlapping panels don't all move at once.
/// </summary>
internal static class PanelDragState
{
    private static bool _isDragging;
    private static string? _owner;

    internal static bool TryStart(string owner)
    {
        if (_isDragging) return false;
        _isDragging = true;
        _owner = owner;
        return true;
    }

    internal static bool IsOwner(string owner) => _isDragging && _owner == owner;

    internal static void End(string owner)
    {
        if (!_isDragging) return;
        if (_owner != owner) return;
        _isDragging = false;
        _owner = null;
    }
}

