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

                cell.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f);

                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;

                bool isEven = (x + y) % 2 == 0;
                sr.color = isEven ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.7f, 0.7f, 0.7f);
                sr.sortingOrder = 0;
            }
        }
    }

    // 플레이어 오브젝트
    private void CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.transform.localScale = new Vector3(cellSize * 0.6f, cellSize * 0.6f, 1f);

        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = new Color(0.9f, 0.2f, 0.2f); // 빨간색
        sr.sortingOrder = 1;

        PlayerMover mover = player.AddComponent<PlayerMover>();
        Vector2Int startCell = new Vector2Int(width / 2, height / 2);
        mover.Setup(width, height, cellSize, startCell);
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);

        float centerX = (width - 1) * cellSize * 0.5f;
        float centerY = (height - 1) * cellSize * 0.5f;
        cam.transform.position = new Vector3(centerX, centerY, -10f);

        float sizeByHeight = height * cellSize * 0.5f + 1f;
        float sizeByWidth = (width * cellSize * 0.5f + 1f) / cam.aspect;
        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}