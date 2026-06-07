using System;
using UnityEngine;

[Serializable]
public struct GameState
{
    public Vector2Int playerPos;
    public Vector2Int[] boxPositions;
    public bool[] doorStates;
    public int moveCount;

    public GameState(Vector2Int player, Vector2Int[] boxes, bool[] doors, int moves)
    {
        playerPos    = player;
        boxPositions = (Vector2Int[])boxes.Clone();
        doorStates   = doors != null ? (bool[])doors.Clone() : Array.Empty<bool>();
        moveCount    = moves;
    }
}
