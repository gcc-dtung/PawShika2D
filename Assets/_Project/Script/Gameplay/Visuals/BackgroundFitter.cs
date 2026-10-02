using UnityEngine;

/// <summary>Giữ sprite nền luôn phủ kín camera khi LevelManager zoom camera theo kích thước lưới.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || _renderer.sprite == null || !targetCamera.orthographic) return;

        float viewH = targetCamera.orthographicSize * 2f;
        float viewW = viewH * targetCamera.aspect;
        Vector2 spriteSize = _renderer.sprite.bounds.size;
        float k = Mathf.Max(viewW / spriteSize.x, viewH / spriteSize.y);

        transform.localScale = new Vector3(k, k, 1f);
        Vector3 camPos = targetCamera.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
    }
}
