using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 파일 이름: PlayerMover.cs
// 역할: 키 입력을 받아 플레이어를 격자 위에서 한 칸씩 움직인다.
public class PlayerMover : MonoBehaviour
{
    private int gridWidth;
    private int gridHeight;
    private float cellSize;
    private Vector2Int currentCell; // 플레이어가 서 있는 칸 번호 (x, y)

    // GridManager가 호출해서 격자 정보를 전달한다
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

        // 아무 키도 안 눌렀으면 아무것도 하지 않는다
        if (direction == Vector2Int.zero) return;

        TryMove(direction);
    }

    // 키 입력을 읽어서 방향을 돌려준다.
    // else if 구조라서 한 번에 한 방향만 선택된다 → 대각선 이동 불가
    private Vector2Int ReadInput()
    {
#if ENABLE_INPUT_SYSTEM
        // 새 Input System (Unity 6 기본 설정)
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
        // 예전 Input Manager 방식
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

    // 목표 칸이 격자 안에 있을 때만 이동한다
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
        // 격자 밖이면 아무것도 하지 않는다 (이동 취소)
    }

    // 칸 번호를 실제 화면 좌표로 바꿔서 오브젝트를 옮긴다
    private void UpdateWorldPosition()
    {
        transform.position = new Vector3(currentCell.x * cellSize, currentCell.y * cellSize, 0f);
    }
}