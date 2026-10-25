using DG.Tweening;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Bàn tay minh họa kéo trên lưới (world space, vẽ trên khối mèo): hiện ở ô bắt đầu, nhấn xuống, trượt tới ô kết thúc,
    /// nhấc lên rồi mờ đi; lặp lại tới khi Stop. Ẩn trong lúc người chơi đang kéo.
    /// Đầu ngón tay nằm ở gốc object; sprite là con, dời theo fingertipOffset.
    /// </summary>
    public class TutorialHand : MonoBehaviour
    {
        private GridManager _grid;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(GridManager grid)
        {
            _grid = grid;
        }

        [SerializeField] private SpriteRenderer sprite;
        [Tooltip("Thời gian trượt từ ô đầu tới ô cuối")]
        [SerializeField] private float moveDuration = 0.9f;
        [SerializeField] private float fadeDuration = 0.2f;
        [Tooltip("Nghỉ giữa 2 lần lặp")]
        [SerializeField] private float loopDelay = 0.5f;
        [SerializeField] private float pressScale = 0.85f;

        private Sequence _loop;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            _baseScale = transform.localScale;
            SetAlpha(0f);
        }

        private void OnDisable() => Kill();

        private void Update()
        {
            // Người chơi đang kéo: ẩn để không che, thả tay thì hiện lại
            if (sprite != null) sprite.enabled = _grid == null || !_grid.IsSelecting;
        }

        /// <summary>Lặp động tác kéo từ from tới to (tọa độ world).</summary>
        public void Play(Vector3 from, Vector3 to)
        {
            Kill();
            gameObject.SetActive(true);
            transform.position = from;
            transform.localScale = _baseScale;
            SetAlpha(0f);

            _loop = DOTween.Sequence()
                .AppendCallback(() =>
                {
                    transform.position = from;
                    transform.localScale = _baseScale;
                })
                .Append(Fade(1f, fadeDuration))
                .Append(transform.DOScale(_baseScale * pressScale, 0.15f).SetEase(Ease.OutQuad))
                .Append(transform.DOMove(to, moveDuration).SetEase(Ease.InOutSine))
                .AppendInterval(0.15f)
                .Append(transform.DOScale(_baseScale, 0.15f).SetEase(Ease.OutQuad))
                .Append(Fade(0f, fadeDuration))
                .AppendInterval(loopDelay)
                .SetLoops(-1)
                .SetLink(gameObject);
        }

        public void Stop()
        {
            Kill();
            SetAlpha(0f);
            gameObject.SetActive(false);
        }

        private void Kill()
        {
            _loop?.Kill();
            _loop = null;
        }

        private Tween Fade(float alpha, float duration) =>
            sprite != null ? sprite.DOFade(alpha, duration) : DOVirtual.DelayedCall(duration, null);

        private void SetAlpha(float alpha)
        {
            if (sprite == null) return;
            Color c = sprite.color;
            c.a = alpha;
            sprite.color = c;
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
        }
#endif
    }
}
