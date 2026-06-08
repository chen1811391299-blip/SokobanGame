using System;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public event Action          OnStateChanged;
    public event Action          OnLevelComplete;
    public event Action<int>     OnMoveCountChanged;

    private SokobanGrid _grid;
    private UndoManager _undo;
    private int         _moves;
    public  bool        InputEnabled { get; set; } = true;
    public  int         MoveCount    => _moves;

    public void Init(SokobanGrid grid, UndoManager undo)
    {
        _grid  = grid;
        _undo  = undo;
        _moves = 0;
    }

    void Update()
    {
        if (!InputEnabled || _grid == null) return;

        if (Input.GetKeyDown(KeyCode.Z) &&
            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            TryUndo();
            return;
        }

        Vector2Int dir = Vector2Int.zero;
        if      (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))    dir = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))  dir = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  dir = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector2Int.right;

        if (ModifierManager.Instance != null &&
            ModifierManager.Instance.ActiveModifier == ModifierType.MirrorControls)
            dir = new Vector2Int(-dir.x, dir.y);

        if (dir != Vector2Int.zero) TryMove(dir);
    }

    private void TryMove(Vector2Int dir)
    {
        _undo.Push(_grid.GetSnapshot(_moves));
        bool boxMoved = _grid.Boxes.Contains(_grid.PlayerPos + dir);
        if (_grid.TryMove(dir))
        {
            _moves++;
            OnMoveCountChanged?.Invoke(_moves);
            OnStateChanged?.Invoke();
            if (boxMoved) AudioManager.PlayPush(); else AudioManager.PlayMove();
            if (_grid.IsComplete()) OnLevelComplete?.Invoke();
        }
        else
        {
            _undo.TryPop(out _);
        }
    }

    private void TryUndo()
    {
        if (_undo.TryPop(out var state))
        {
            _grid.LoadSnapshot(state);
            _moves = state.moveCount;
            OnMoveCountChanged?.Invoke(_moves);
            OnStateChanged?.Invoke();
            AudioManager.PlayUndo();
        }
    }

    public void ResetMoves() { _moves = 0; OnMoveCountChanged?.Invoke(0); }

    public void MoveInDirection(Vector2Int dir)
    {
        if (!InputEnabled || _grid == null) return;
        TryMove(dir);
    }
}
