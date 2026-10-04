using UnityEngine;


public class GridManager : MonoBehaviour
{
    [Header("격자 크기 (칸 수)")]
    [SerializeField] private int width = 8;   // 가로
    [SerializeField] private int height = 8;  // 세로

    [Header("칸 하나의 크기")]
    [SerializeField] private float cellSize = 1f;

    private Sprite squareSprite; // 네모 모양

    private void Start()
    {
        squareSprite = CreateSquareSprite();

        CreateGrid();
        CreatePlayer();
        SetupCamera();
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.filterMode = FilterMode.Point;
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void CreateGrid()
    {
        GameObject gridParent = new GameObject("Grid");

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cell = new GameObject("Cell_" + x + "_" + y);
                cell.transform.SetParent(gridParent.transform);
                cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0f);

                // 칸 크기를 살짝 줄여서 칸 사이에 틈(=격자선)이 보이게 한다
                cell.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f);

                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;

                // 체스판처럼 두 가지 색을 번갈아 사용
                bool isEven = (x + y) % 2 == 0;
                sr.color = isEven ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.7f, 0.7f, 0.7f);
                sr.sortingOrder = 0;
            }
        }
    }

    // 플레이어 오브젝트를 만든다
    private void CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.transform.localScale = new Vector3(cellSize * 0.6f, cellSize * 0.6f, 1f);

        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = new Color(0.9f, 0.2f, 0.2f); // 빨간색
        sr.sortingOrder = 1; // 격자보다 위에 보이게

        // PlayerMover를 코드로 붙이고, 격자 정보를 알려준다
        PlayerMover mover = player.AddComponent<PlayerMover>();
        Vector2Int startCell = new Vector2Int(width / 2, height / 2); // 가운데에서 시작
        mover.Setup(width, height, cellSize, startCell);
    }

    // 카메라가 격자 전체를 보도록 맞춘다
    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);

        // 격자의 중심으로 카메라 이동
        float centerX = (width - 1) * cellSize * 0.5f;
        float centerY = (height - 1) * cellSize * 0.5f;
        cam.transform.position = new Vector3(centerX, centerY, -10f);

        // 가로/세로 중 더 큰 쪽에 맞춰 화면 크기 결정
        float sizeByHeight = height * cellSize * 0.5f + 1f;
        float sizeByWidth = (width * cellSize * 0.5f + 1f) / cam.aspect;
        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}