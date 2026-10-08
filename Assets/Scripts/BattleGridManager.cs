using UnityEngine;

// 파일 이름: BattleGridManager.cs
// 역할: BattleScene의 전투 격자를 만들고, 카메라를 맞추고, 좌표 변환 기능을 제공한다.
// 붙일 곳: BattleScene의 BattleGrid 오브젝트
public class BattleGridManager : MonoBehaviour
{
    [Header("격자 크기 (칸 수)")]
    [SerializeField] private int width = 20;
    [SerializeField] private int height = 20;

    [Header("칸 하나의 크기 (월드 단위)")]
    [SerializeField] private float cellSize = 1f;

    [Header("칸 사이 틈 (1 = 틈 없음, 0.95 = 얇은 선처럼 보임)")]
    [Range(0.8f, 1f)]
    [SerializeField] private float cellFill = 0.95f;

    [Header("카메라")]
    [SerializeField] private float cameraPadding = 1f; // 격자 바깥 여백

    [Header("칸 색상 (체스판처럼 번갈아 사용)")]
    [SerializeField] private Color colorA = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] private Color colorB = new Color(0.70f, 0.70f, 0.70f);

    private Sprite squareSprite;

    // 다른 스크립트에서 읽을 수 있는 값들
    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    private void Start()
    {
        squareSprite = CreateSquareSprite();
        CreateGrid();
        SetupCamera();
    }

    // =========================================================
    // 좌표 변환 기능 (나중에 캐릭터를 배치할 때 사용)
    // 칸 번호 (0,0)은 왼쪽 아래 칸이고, BattleGrid 오브젝트의 위치가 그 칸의 중심이다.
    // =========================================================

    // 칸 번호 → 월드 좌표 (칸의 중심)
    public Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3 origin = transform.position;
        return new Vector3(origin.x + cell.x * cellSize, origin.y + cell.y * cellSize, origin.z);
    }

    // 월드 좌표 → 칸 번호 (격자 밖이어도 계산은 해 준다. 밖인지는 IsInsideGrid로 확인)
    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        Vector3 origin = transform.position;
        int x = Mathf.RoundToInt((worldPosition.x - origin.x) / cellSize);
        int y = Mathf.RoundToInt((worldPosition.y - origin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    // 그 칸이 격자 안에 있는지 확인
    public bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    // 격자 전체의 중심 월드 좌표
    public Vector3 GetGridCenter()
    {
        Vector3 origin = transform.position;
        return new Vector3(
            origin.x + (width - 1) * cellSize * 0.5f,
            origin.y + (height - 1) * cellSize * 0.5f,
            origin.z);
    }

    // =========================================================
    // 격자 생성
    // =========================================================

    // 흰색 네모 스프라이트를 코드로 만든다 (별도 이미지 파일 필요 없음)
    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.filterMode = FilterMode.Point;
        texture.Apply();

        // 마지막 숫자 1f = 1유닛당 1픽셀 → 스프라이트 크기가 정확히 1x1
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void CreateGrid()
    {
        GameObject cellsParent = new GameObject("Cells");
        cellsParent.transform.SetParent(transform, false);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cellId = new Vector2Int(x, y);

                GameObject cell = new GameObject("Cell_" + x + "_" + y);
                cell.transform.SetParent(cellsParent.transform, false);
                cell.transform.position = CellToWorld(cellId);
                cell.transform.localScale = new Vector3(cellSize * cellFill, cellSize * cellFill, 1f);

                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;
                sr.color = ((x + y) % 2 == 0) ? colorA : colorB;
                sr.sortingOrder = 0; // 캐릭터는 나중에 1 이상으로 두면 격자 위에 보인다
            }
        }
    }

    // =========================================================
    // 카메라: 격자 전체가 화면에 들어오도록 맞춘다
    // =========================================================
    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("MainCamera 태그가 붙은 카메라를 찾지 못했습니다.");
            return;
        }

        cam.orthographic = true;

        Vector3 center = GetGridCenter();
        cam.transform.position = new Vector3(center.x, center.y, -10f);

        // 세로 기준, 가로 기준 중 더 큰 값을 사용해서 격자가 잘리지 않게 한다
        float halfHeight = height * cellSize * 0.5f + cameraPadding;
        float halfWidth = width * cellSize * 0.5f + cameraPadding;
        float sizeByHeight = halfHeight;
        float sizeByWidth = halfWidth / cam.aspect;

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}