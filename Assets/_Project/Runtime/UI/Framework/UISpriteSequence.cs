using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// Phát lại chuỗi khung hình dựng sẵn từ animation Spine (dơi Halloween ở Home): mỗi khung một sprite + vị trí (anchoredPosition).
    /// Vòng lặp dài loopFrames khung; khung firstFrame..firstFrame+frames.Length-1 thì hiện, ngoài khoảng (hoặc sprite trống) thì ẩn Image.
    /// Lệch pha bằng startOffset (giây). Dùng thời gian thật (unscaled) như UIPulse.
    /// </summary>
    public class UISpriteSequence : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private float fps = 30f;
        [Tooltip("Số khung của cả vòng lặp (gồm cả đoạn ẩn)")]
        [SerializeField] private int loopFrames = 1;
        [Tooltip("Khung trong vòng lặp mà frames[0] bắt đầu hiện")]
        [SerializeField] private int firstFrame;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Vector2[] positions;
        [SerializeField] private float startOffset;

        private RectTransform _rect;
        private int _shown = -2;

        private void Awake()
        {
            _rect = image.rectTransform;
        }

        private void OnEnable()
        {
            _shown = -2;
        }

        private void Update()
        {
            int f = Mathf.FloorToInt((Time.unscaledTime + startOffset) * fps) % Mathf.Max(1, loopFrames);
            int i = f - firstFrame;
            if (i < 0 || i >= frames.Length || frames[i] == null) i = -1;
            if (i == _shown) return;
            _shown = i;
            image.enabled = i >= 0;
            if (i < 0) return;
            image.sprite = frames[i];
            _rect.anchoredPosition = positions[i];
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (image == null) image = GetComponent<Image>();
        }
#endif
    }
}
