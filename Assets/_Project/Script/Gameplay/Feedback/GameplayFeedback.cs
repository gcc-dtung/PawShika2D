using System.Collections;
using UnityEngine;

/// <summary>
/// Phản hồi trong màn chơi:
/// - Đặt đúng: chữ khen (NICE / AMAZING...) bật lên ở giữa khối.
/// - Đặt sai: vùng chớp đỏ, camera rung, điện thoại rung.
/// </summary>
public class GameplayFeedback : MonoBehaviour
{
    [Header("Khen")]
    [SerializeField, Range(0f, 1f)] private float praiseChance = 0.6f;
    [SerializeField] private int praiseMinCells = 2;
    [SerializeField] private float praiseWidth = 2.6f;
    [SerializeField] private float praiseDuration = 0.9f;

    [Header("Sai")]
    [SerializeField] private Color mistakeColor = new Color(1f, 0.2f, 0.25f, 0.55f);
    [SerializeField] private float mistakeFlashDuration = 0.45f;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 0.15f;
    [SerializeField] private bool vibrate = true;

    private Camera _cam;
    private Sprite _square;
    private Coroutine _shake;

    private void Awake()
    {
        _cam = Camera.main;
        Texture2D tex = Texture2D.whiteTexture;
        _square = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
    }

    private void Start()
    {
        if (GridManager.Instance == null) return;
        GridManager.Instance.onBlockPlaced.AddListener(OnBlockPlaced);
        GridManager.Instance.onMistake.AddListener(OnMistake);
    }

    private void OnDestroy()
    {
        if (GridManager.Instance == null) return;
        GridManager.Instance.onBlockPlaced.RemoveListener(OnBlockPlaced);
        GridManager.Instance.onMistake.RemoveListener(OnMistake);
    }

    private void OnBlockPlaced(PlacedBlock block)
    {
        if (block == null || block.Rect.width * block.Rect.height < praiseMinCells) return;
        if (Random.value > praiseChance) return;

        GameplayVisuals visuals = GameplayVisuals.Instance;
        Sprite praise = visuals != null ? visuals.GetRandomPraise() : null;
        if (praise == null) return;

        SpriteRenderer sr = CreateRenderer("Praise", praise, 60);
        sr.transform.position = block.transform.position + new Vector3(0f, 0f, -1f);
        StartCoroutine(PraiseRoutine(sr));
    }

    private IEnumerator PraiseRoutine(SpriteRenderer sr)
    {
        float baseScale = praiseWidth / sr.sprite.bounds.size.x;
        Vector3 start = sr.transform.position;
        float t = 0f;
        while (t < praiseDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / praiseDuration);
            // Bật to nhanh, giữ, rồi bay lên và mờ dần
            float pop = p < 0.2f ? Mathf.Lerp(0.3f, 1.15f, p / 0.2f) : (p < 0.3f ? Mathf.Lerp(1.15f, 1f, (p - 0.2f) / 0.1f) : 1f);
            sr.transform.localScale = Vector3.one * baseScale * pop;
            sr.transform.position = start + Vector3.up * Mathf.Max(0f, p - 0.5f) * 1.2f;
            Color c = sr.color;
            c.a = p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f;
            sr.color = c;
            yield return null;
        }
        Destroy(sr.gameObject);
    }

    private void OnMistake(Vector3 center, Vector2 size)
    {
        SpriteRenderer flash = CreateRenderer("MistakeFlash", _square, 50);
        flash.transform.position = center;
        flash.transform.localScale = new Vector3(size.x, size.y, 1f);
        flash.color = mistakeColor;
        StartCoroutine(FlashRoutine(flash));

        if (_shake != null) StopCoroutine(_shake);
        _shake = StartCoroutine(ShakeCamera());

        if (vibrate) Vibrate();
    }

    private static void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    private IEnumerator FlashRoutine(SpriteRenderer sr)
    {
        Color baseColor = sr.color;
        float t = 0f;
        while (t < mistakeFlashDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / mistakeFlashDuration);
            // 2 nhịp chớp rồi tắt
            float blink = Mathf.Abs(Mathf.Sin(p * Mathf.PI * 2f));
            Color c = baseColor;
            c.a = baseColor.a * blink * (1f - p * 0.5f);
            sr.color = c;
            yield return null;
        }
        Destroy(sr.gameObject);
    }

    private IEnumerator ShakeCamera()
    {
        if (_cam == null) yield break;
        Vector3 origin = _cam.transform.position;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(t / shakeDuration);
            _cam.transform.position = origin + new Vector3(Mathf.Sin(t * 90f) * shakeStrength * damper, 0f, 0f);
            yield return null;
        }
        _cam.transform.position = origin;
        _shake = null;
    }

    private SpriteRenderer CreateRenderer(string name, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }
}
