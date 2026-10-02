using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Phản hồi trong màn chơi:
/// - Đặt đúng: chữ khen (NICE / AMAZING...) bật lên ở giữa khối.
/// - Đặt sai: camera rung, điện thoại rung (mèo rung đỏ do PlacedBlockView tự chạy).
/// Chữ khen lấy từ pool (prefab), không tạo GameObject mới.
/// </summary>
public class GameplayFeedback : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private SpriteRenderer praisePrefab;

    [Header("Khen")]
    [SerializeField, Range(0f, 1f)] private float praiseChance = 0.6f;
    [SerializeField] private int praiseMinCells = 2;
    [SerializeField] private float praiseWidth = 2.6f;
    [SerializeField] private float praiseDuration = 0.9f;

    [Header("Sai")]
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 0.15f;
    [SerializeField] private bool vibrate = true;

    private Camera _cam;
    private Tween _shake;
    private Vector3 _shakeOrigin;
    private readonly List<Tween> _praiseTweens = new List<Tween>();
    private ObjectPool<SpriteRenderer> _praisePool;

    private void Awake()
    {
        _cam = Camera.main;
        _praisePool = CreatePool(praisePrefab, 4);
    }

    private ObjectPool<SpriteRenderer> CreatePool(SpriteRenderer prefab, int capacity)
    {
        if (prefab == null) Debug.LogError($"[GameplayFeedback] Chưa gán prefab trên {name}");
        return new ObjectPool<SpriteRenderer>(
            createFunc: () => Instantiate(prefab, transform),
            actionOnGet: sr => sr.gameObject.SetActive(true),
            actionOnRelease: sr => sr.gameObject.SetActive(false),
            // Thoát Play mode: scene hủy object trước, rồi Editor mới Clear() pool -> object có thể đã null
            actionOnDestroy: sr => { if (sr != null) Destroy(sr.gameObject); },
            collectionCheck: false,
            defaultCapacity: capacity);
    }

    private void Start()
    {
        if (GridManager.Instance == null) return;
        GridManager.Instance.onBlockPlaced.AddListener(OnBlockPlaced);
        GridManager.Instance.onMistake.AddListener(OnMistake);
    }

    // Tắt component: hủy mọi tween. Chữ khen chạy nốt tới cuối (complete) để trả về pool; camera về vị trí gốc.
    private void OnDisable()
    {
        for (int i = _praiseTweens.Count - 1; i >= 0; i--) _praiseTweens[i]?.Kill(true);
        _praiseTweens.Clear();
        _shake?.Kill();
        _shake = null;
    }

    private void OnDestroy()
    {
        if (GridManager.Instance == null) return;
        GridManager.Instance.onBlockPlaced.RemoveListener(OnBlockPlaced);
        GridManager.Instance.onMistake.RemoveListener(OnMistake);
    }

    private void OnBlockPlaced(BlockModel block)
    {
        if (block == null || block.Area < praiseMinCells) return;
        if (Random.value > praiseChance) return;

        GameplayVisuals visuals = GameplayVisuals.Instance;
        Sprite praise = visuals != null ? visuals.GetRandomPraise() : null;
        if (praise == null) return;

        SpriteRenderer sr = _praisePool.Get();
        sr.sprite = praise;
        sr.color = Color.white;
        sr.transform.position = GridManager.Instance.Board.Geometry.RectCenter(block.Rect) + new Vector3(0f, 0f, -1f);
        PlayPraise(sr);
    }

    /// <summary>Bật to nhanh (0.3 → 1.15 → 1), giữ, rồi bay lên và mờ dần; xong trả về pool.</summary>
    private void PlayPraise(SpriteRenderer sr)
    {
        Transform t = sr.transform;
        float baseScale = praiseWidth / sr.sprite.bounds.size.x;
        Vector3 start = t.position;
        float d = praiseDuration;
        t.localScale = Vector3.one * baseScale * 0.3f;

        Sequence seq = DOTween.Sequence();
        seq.Append(t.DOScale(baseScale * 1.15f, d * 0.2f).SetEase(Ease.Linear));
        seq.Append(t.DOScale(baseScale, d * 0.1f).SetEase(Ease.Linear));
        seq.Insert(d * 0.5f, t.DOMove(start + Vector3.up * 0.6f, d * 0.5f).SetEase(Ease.Linear));
        seq.Insert(d * 0.6f, sr.DOFade(0f, d * 0.4f).SetEase(Ease.Linear));
        seq.SetLink(sr.gameObject);
        seq.OnKill(() =>
        {
            // Complete hay bị Kill(true) đều qua đây đúng 1 lần -> trả về pool
            _praiseTweens.Remove(seq);
            if (sr != null) _praisePool.Release(sr);
        });
        _praiseTweens.Add(seq);
    }

    private void OnMistake(Vector3 center, Vector2 size)
    {
        ShakeCamera();
        if (vibrate) Vibrate();
    }

    private static void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    /// <summary>Rung ngang, biên độ giảm dần; bị hủy giữa chừng thì camera vẫn về đúng vị trí gốc.</summary>
    private void ShakeCamera()
    {
        if (_cam == null) return;
        if (_shake == null) _shakeOrigin = _cam.transform.position; // đang rung dở thì giữ gốc cũ
        _shake?.Kill();

        Transform cam = _cam.transform;
        Tween tween = null;
        tween = DOVirtual.Float(0f, 1f, shakeDuration, p =>
            {
                float t = p * shakeDuration;
                cam.position = _shakeOrigin + new Vector3(Mathf.Sin(t * 90f) * shakeStrength * (1f - p), 0f, 0f);
            })
            .SetEase(Ease.Linear)
            .SetLink(gameObject)
            .OnKill(() =>
            {
                if (cam != null) cam.position = _shakeOrigin;
                if (_shake == tween) _shake = null;
            });
        _shake = tween;
    }
}
