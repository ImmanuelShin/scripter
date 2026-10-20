namespace Scripter.Services;

public sealed class UndoRedoStack<T>
{
    private readonly int _capacity;
    private readonly List<T> _undo = [];
    private readonly List<T> _redo = [];

    public UndoRedoStack(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Record(T stateBeforeChange)
    {
        _undo.Add(stateBeforeChange);
        if (_undo.Count > _capacity)
        {
            _undo.RemoveAt(0);
        }

        _redo.Clear();
    }

    public T? Undo(T current)
    {
        if (_undo.Count == 0)
        {
            return default;
        }

        var previous = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(current);
        return previous;
    }

    public T? Redo(T current)
    {
        if (_redo.Count == 0)
        {
            return default;
        }

        var next = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(current);
        return next;
    }
}
