using UnityEngine;

namespace PawBlock.Shared
{
    /// <summary>
    /// Giữ sprite nền luôn phủ kín camera khi camera zoom / dời theo lưới (PlayZone, LevelManager).
    /// Scale đặt trong scene là mức tối thiểu: đã phủ kín thì giữ nguyên, chưa kín thì phóng đều vừa đủ để phủ kín.
    /// Nền luôn đi theo tâm camera.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private SpriteRenderer _renderer;
        private float _baseScale;
        private float _lastOrtho = -1f;
        private float _lastAspect;
        private Vector3 _lastCamPos;
        private Sprite _lastSprite;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (targetCamera == null) targetCamera = Camera.main;
            _baseScale = Mathf.Max(transform.localScale.x, transform.localScale.y);
        }

        private void LateUpdate()
        {
            if (targetCamera == null || _renderer.sprite == null || !targetCamera.orthographic) return;

            // Camera chỉ đổi khi căn lưới / xoay máy: không đổi thì khỏi ghi transform (tránh đánh dấu thay đổi mỗi frame)
            Vector3 camPos = targetCamera.transform.position;
            if (targetCamera.orthographicSize == _lastOrtho && targetCamera.aspect == _lastAspect
                && camPos == _lastCamPos && _renderer.sprite == _lastSprite) return;
            _lastOrtho = targetCamera.orthographicSize;
            _lastAspect = targetCamera.aspect;
            _lastCamPos = camPos;
            _lastSprite = _renderer.sprite;

            float viewH = targetCamera.orthographicSize * 2f;
            float viewW = viewH * targetCamera.aspect;
            Vector2 spriteSize = _renderer.sprite.bounds.size; // kích thước chưa scale
            float cover = Mathf.Max(viewW / spriteSize.x, viewH / spriteSize.y);
            float k = Mathf.Max(_baseScale, cover); // chỉ phóng to khi chưa phủ kín

            transform.localScale = new Vector3(k, k, 1f);
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }
    }
}
