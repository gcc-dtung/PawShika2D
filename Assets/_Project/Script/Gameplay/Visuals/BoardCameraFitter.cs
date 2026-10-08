using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phóng camera cho lưới vừa vùng chơi mỗi khi GridSpawner dựng lưới mới (tách khỏi LevelManager).
/// Có PlayZone: PlayZone căn (và tự căn lại khi màn hình đổi).
/// PlayZone đang tắt (ở Menu) thì căn tạm theo tỉ lệ màn hình; vào màn chơi PlayZone căn lại.
/// </summary>
public class BoardCameraFitter : MonoBehaviour
{
    [SerializeField] private GridSpawner gridSpawner;
    [Tooltip("Vùng chơi trong Canvas_HUD: lưới luôn nằm trọn trong vùng này. Không có (hoặc đang tắt) thì dùng tỉ lệ bên dưới.")]
    [SerializeField] private PlayZone playZone;
    [SerializeField] private Camera targetCamera;
    [SerializeField, Range(0.3f, 1f)] private float boardHeightRatio = 0.62f;
    [SerializeField, Range(0.5f, 1f)] private float boardWidthRatio = 0.92f;
    [SerializeField, Range(-0.5f, 0.5f)] private float boardCenterOffset = 0.02f; // >0: lưới lệch lên trên

    private void Awake()
    {
        if (gridSpawner == null) gridSpawner = FindFirstObjectByType<GridSpawner>();
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (gridSpawner != null) gridSpawner.gridCellEvent.AddListener(OnGridSpawned);
    }

    private void OnDisable()
    {
        if (gridSpawner != null) gridSpawner.gridCellEvent.RemoveListener(OnGridSpawned);
    }

    private void OnGridSpawned(List<CellView> _) => Fit(gridSpawner.LevelData);

    public void Fit(LevelDefinition level)
    {
        if (level == null) return;
        float gridW = level.size * GridSpawner.CellSpacing;
        float gridH = level.Height * GridSpawner.CellSpacing;

        if (playZone != null)
        {
            playZone.SetContent(new Vector2(gridW, gridH));
            if (playZone.isActiveAndEnabled) return;
        }

        if (targetCamera == null || !targetCamera.orthographic) return;
        float aspect = targetCamera.aspect;

        float sizeByHeight = gridH / 2f / boardHeightRatio;
        float sizeByWidth = gridW / 2f / (aspect * boardWidthRatio);
        float ortho = Mathf.Max(PlayZone.DefaultMinOrthoSize, sizeByHeight, sizeByWidth);
        targetCamera.orthographicSize = ortho;

        // Dời camera xuống để lưới nằm hơi cao hơn giữa màn hình
        Vector3 pos = targetCamera.transform.position;
        targetCamera.transform.position = new Vector3(0f, -boardCenterOffset * 2f * ortho, pos.z);
    }
}
