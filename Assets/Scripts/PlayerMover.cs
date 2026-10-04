using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerMover : MonoBehaviour
{
    private int gridWidth;
    private int gridHeight;
    private float cellSize;
    private Vector2Int currentCell;

    public void Setup(int width, int height, float size, Vector2Int startCell)
    {
        gridWidth = width;
        gridHeight = height;
        cellSize = size;
        currentCell = startCell;
        UpdateWorldPosition();
    }

    private void Update()
    {
        Vector2Int direction = ReadInput();

        if (direction == Vector2Int.zero) return;

        TryMove(direction);
    }

    private Vector2Int ReadInput()
    {
#if ENABLE_INPUT_SYSTEM

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2Int.zero;

        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            return Vector2Int.up;
        else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            return Vector2Int.down;
        else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            return Vector2Int.left;
        else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            return Vector2Int.right;
#elif ENABLE_LEGACY_INPUT_MANAGER

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            return Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            return Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            return Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            return Vector2Int.right;
#endif
        return Vector2Int.zero;
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetCell = currentCell + direction;

        bool insideX = targetCell.x >= 0 && targetCell.x < gridWidth;
        bool insideY = targetCell.y >= 0 && targetCell.y < gridHeight;

        if (insideX && insideY)
        {
            currentCell = targetCell;
            UpdateWorldPosition();
        }
    }

    private void UpdateWorldPosition()
    {
        transform.position = new Vector3(currentCell.x * cellSize, currentCell.y * cellSize, 0f);
    }
}