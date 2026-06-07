using System.Collections.Generic;
using UnityEngine;

public class UndoManager : MonoBehaviour
{
    private readonly Stack<GameState> _stack = new();

    public bool CanUndo => _stack.Count > 0;

    public void Push(GameState state) => _stack.Push(state);

    public bool TryPop(out GameState state)
    {
        if (_stack.Count == 0) { state = default; return false; }
        state = _stack.Pop();
        return true;
    }

    public void Clear() => _stack.Clear();
}
