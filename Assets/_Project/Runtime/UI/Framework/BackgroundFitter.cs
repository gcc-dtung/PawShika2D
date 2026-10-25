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
        [SerializeField] private SpriteRenderer background;

        private float _baseScale;
        private float _lastOrtho = -1f;
        private float _lastAspect;
        private Vector3 _lastCamPos;
        private Sprite _lastSprite;

        private void Awake()
        {
            _baseScale = Mathf.Max(transform.localScale.x, transform.localScale.y);
        }

        private void LateUpdate()
        {
            if (targetCamera == null || background.sprite == null || !targetCamera.orthographic) return;

            // Camera chỉ đổi khi căn lưới / xoay máy: không đổi thì khỏi ghi transform (tránh đánh dấu thay đổi mỗi frame)
            Vector3 camPos = targetCamera.transform.position;
            if (targetCamera.orthographicSize == _lastOrtho && targetCamera.aspect == _lastAspect
                && camPos == _lastCamPos && background.sprite == _lastSprite) return;
            _lastOrtho = targetCamera.orthographicSize;
            _lastAspect = targetCamera.aspect;
            _lastCamPos = camPos;
            _lastSprite = background.sprite;

            float viewH = targetCamera.orthographicSize * 2f;
            float viewW = viewH * targetCamera.aspect;
            Vector2 spriteSize = background.sprite.bounds.size; // kích thước chưa scale
            float cover = Mathf.Max(viewW / spriteSize.x, viewH / spriteSize.y);
            float k = Mathf.Max(_baseScale, cover); // chỉ phóng to khi chưa phủ kín

            transform.localScale = new Vector3(k, k, 1f);
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (background == null) background = GetComponent<SpriteRenderer>();
        }
#endif
    }
}
