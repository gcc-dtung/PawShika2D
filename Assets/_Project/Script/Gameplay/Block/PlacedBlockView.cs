using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Hình khối "mèo": thân màu (9-slice), tai, mặt hoạt hình và số của clue.
/// Chỉ lo phần hiển thị; dữ liệu khối nằm ở BlockModel. Mọi object con có sẵn trong PlacedBlock.prefab,
/// khối được GridManager lấy ra / trả về pool.
///
/// Vòng đời 1 khối:
///   ShowPreview  -> đang kéo, vùng đủ value: mèo hiện ra đặc ngay, viền màu mèo thu bên trong khối
///   Confirm      -> thả tay: mèo đặc lại và bật nhẹ
///   PlayWrong    -> lưới kín mà khối sai số ô: rung + viền đỏ nháy, khối ở lại
///   PlayRemove   -> chạm khối để gỡ: co nhỏ + mờ rồi về pool
///   PlayWinFlash -> thắng: thân + tai phủ trắng rồi tan (lớp sprite trắng đặc đè lên, tạo lúc cần)
/// </summary>
public class PlacedBlockView : MonoBehaviour
{
    private const float BodyFill = 0.96f; // thân khối chiếm 96% ô, phần còn lại là khe hở
    private const float BlockZ = -0.05f;

    [Header("Object con trong prefab")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private TextMeshPro numberText;
    [SerializeField] private CatFaceAnimator face;
    [SerializeField] private SpriteRenderer ears;
    [SerializeField] private SpriteRenderer ring;  // viền 9-slice (RingBlock) cho hiệu ứng đủ value
    [Tooltip("Material vẽ sprite thành 1 màu đặc (GUI/Text Shader) cho hiệu ứng lóe sáng khi thắng")]
    [SerializeField] private Material flashMaterial;

    [Header("Tai mèo (tính theo tỉ lệ ô)")]
    [SerializeField] private float earWidth = 0.36f;   // bề rộng cặp tai
    [SerializeField] private float earOverlap = 0f;    // đỉnh tai đè lên khối bên trên thêm bao nhiêu

    [Header("Đặt khối: dẹt xuống rồi bật lên (squash & stretch), đáy khối đứng yên")]
    [SerializeField, Min(0.01f)] private float squashDuration = 0.34f;
    [Tooltip("Dẹt / dài tối đa (0.12 = ngang +12%, dọc -12%)")]
    [SerializeField, Range(0f, 0.3f)] private float squashAmount = 0.12f;
    [Tooltip("Số nửa nhịp dẹt / dài trước khi đứng yên")]
    [SerializeField, Range(1f, 5f)] private float squashWobbles = 2.5f;

    [Header("Đủ value: viền màu thu bên trong (ref reffullvalue)")]
    [SerializeField] private float ringDuration = 0.2f;
    [SerializeField] private float ringStartInset = 0.12f;      // viền bắt đầu thụt vào bao nhiêu so với mép khối (world)
    [SerializeField] private float ringStartThickness = 1f;     // độ dày nét lúc đầu (1 = 0.14 world)
    [SerializeField] private float ringEndThickness = 0.35f;    // độ dày nét lúc chạm mép
    [SerializeField, Range(0f, 1f)] private float ringWhiten = 0.45f; // màu mèo pha trắng để nổi trên thân cùng màu

    [Header("Khối sai khi lưới kín")]
    [SerializeField] private Color rejectRingColor = new Color(1f, 0.25f, 0.3f, 1f);
    [SerializeField] private float rejectRingOutset = 0.05f;    // viền đỏ nằm ngoài mép khối bao nhiêu (world)
    [SerializeField] private float rejectRingThickness = 0.6f;  // độ dày nét (1 = 0.14 world)
    [SerializeField] private int rejectRingBlinks = 2;          // số nhịp nháy trong lúc rung
    [SerializeField] private float rejectShakeDuration = 0.45f;
    [SerializeField] private float rejectShakeStrength = 0.12f;
    [SerializeField] private float rejectShakeFrequency = 45f;

    [Header("Gỡ khối (chạm vào khối): nhảy bật lên rồi rơi xuống, co nhỏ và mờ dần")]
    [SerializeField] private float removeDuration = 0.34f;
    [SerializeField, Range(0.3f, 1f)] private float removeShrinkTo = 0.6f;
    [SerializeField] private float removeHopHeight = 0.35f;   // world
    [SerializeField] private float removeFallDepth = 0.25f;   // rơi thấp hơn chỗ cũ bao nhiêu khi biến mất

    [Header("Mèo kề bên phản ứng (khối mới đặt cạnh)")]
    [SerializeField] private float nudgeHeight = 0.12f;
    [SerializeField] private float nudgeDuration = 0.26f;

    [Header("Run khi đóng băng")]
    [SerializeField] private float shiverDuration = 0.45f;
    [SerializeField] private float shiverStrength = 0.035f;

    private float _cellSize = GridSpawner.CellSpacing;
    private Color _bodyBaseColor = Color.white;
    private Color _numberBaseColor = Color.white;
    private Vector3 _basePosition;
    private Vector2 _bodySize;
    private Tween _anim;     // bật / báo sai
    private Tween _ringAnim; // viền đủ value
    private Tween _flashAnim;
    private SpriteRenderer _bodyFlash;
    private SpriteRenderer _earsFlash;
    private static Material _fallbackFlashMaterial;
    private ColorType _color;
    private float _faceWidth;
    private CatFaceAnimation _faceAnim;

    public bool IsPreview { get; private set; }
    public RectInt Rect { get; private set; }

    /// <summary>Hiện mèo xem trước cho vùng đang kéo (chưa đặt), cùng màu với khung chọn.</summary>
    public void ShowPreview(RectInt rect, CellModel clueCell, ColorType color, GridGeometry geometry)
    {
        Layout(rect, clueCell, color, geometry);
        IsPreview = true;
        SetTint(Color.white, 1f);

        KillRing();
        _ringAnim = RingIn();
    }

    /// <summary>Hiện khối đã đặt (không qua xem trước).</summary>
    public void Show(BlockModel block, GridGeometry geometry)
    {
        Layout(block.Rect, block.ClueCell, block.Color, geometry);
        Confirm();
    }

    /// <summary>Thả tay, nước đi đúng: mèo xem trước thành khối thật.</summary>
    public void Confirm(CellModel clueCell = null)
    {
        bool wasPreview = IsPreview;
        IsPreview = false;
        if (wasPreview) ReactFace(); // mèo xem trước thành khối thật: đổi biểu cảm (như vừa "đáp" xuống)
        if (clueCell != null) SetNumber(clueCell); // ô "?" vẫn hiện "?" (không lộ số)
        SetTint(Color.white, 1f);
        PlayAnim(PopIn());
    }

    /// <summary>Lưới kín mà khối này sai số ô: giữ màu mèo, rung + viền đỏ nháy, khối ở lại (chạm để gỡ).</summary>
    public void PlayWrong()
    {
        IsPreview = false;
        PlayAnim(ShakeRoutine());
    }

    /// <summary>
    /// Chạm khối để gỡ: mèo nhảy bật lên (dài người ra), rồi rơi xuống thấp hơn chỗ cũ, co nhỏ và mờ dần.
    /// Xong thì gọi onDone (trả về pool).
    /// </summary>
    public void PlayRemove(Action onDone)
    {
        IsPreview = false;
        KillRing();
        _flashAnim?.Kill();
        transform.localScale = Vector3.one;
        const float upPart = 0.35f; // 35% thời gian đầu: bật lên
        Tween tween = DOVirtual.Float(0f, 1f, removeDuration, p =>
            {
                float y;
                Vector3 scale;
                if (p < upPart)
                {
                    float u = p / upPart;
                    float e = 1f - (1f - u) * (1f - u); // OutQuad
                    y = removeHopHeight * e;
                    float stretch = 0.08f * Mathf.Sin(u * Mathf.PI); // giữa cú nhảy dài người ra
                    scale = new Vector3(1f - stretch, 1f + stretch, 1f);
                }
                else
                {
                    float u = (p - upPart) / (1f - upPart);
                    float e = u * u; // InQuad: rơi nhanh dần
                    y = Mathf.Lerp(removeHopHeight, -removeFallDepth, e);
                    scale = Vector3.one * Mathf.Lerp(1f, removeShrinkTo, e);
                    SetTint(Color.white, 1f - e);
                }
                transform.localScale = scale;
                transform.position = _basePosition + new Vector3(0f, y, 0f);
            })
            .SetEase(Ease.Linear)
            .OnComplete(() => onDone?.Invoke());
        PlayAnim(tween);
    }

    /// <summary>Khối mới vừa đặt sát bên: mèo nhảy lên 1 cái (giật mình) rồi đáp xuống, đổi biểu cảm.</summary>
    public void PlayNudge(float delay)
    {
        if (IsPreview || !gameObject.activeInHierarchy) return;
        Tween tween = DOVirtual.Float(0f, 1f, nudgeDuration, p =>
            {
                float hop = Mathf.Sin(p * Mathf.PI);
                transform.position = _basePosition + new Vector3(0f, nudgeHeight * hop, 0f);
                float s = 0.05f * hop;
                transform.localScale = new Vector3(1f - s, 1f + s, 1f);
            })
            .SetDelay(delay)
            .SetEase(Ease.OutQuad)
            .OnStart(ReactFace)
            .OnComplete(ResetPose);
        PlayAnim(tween);
    }

    /// <summary>Đóng băng: mèo run lên vài nhịp ngang (biên độ giảm dần).</summary>
    public void PlayShiver(float delay)
    {
        if (IsPreview || !gameObject.activeInHierarchy) return;
        Tween tween = DOVirtual.Float(0f, 1f, shiverDuration, p =>
                transform.position = _basePosition + new Vector3(Mathf.Sin(p * Mathf.PI * 14f) * shiverStrength * (1f - p), 0f, 0f))
            .SetDelay(delay)
            .SetEase(Ease.Linear)
            .OnComplete(ResetPose);
        PlayAnim(tween);
    }

    /// <summary>
    /// Độ trong của mèo mờ Hint (nhấp nháy). Số giữ đặc: số mờ để lộ số nhạt của ô bên dưới, 2 số chồng lẫn khó đọc.
    /// </summary>
    public void SetAlpha(float alpha)
    {
        SetTint(Color.white, alpha);
        numberText.color = _numberBaseColor;
    }

    /// <summary>Đổi sang 1 biểu cảm khác (mèo vừa đặt / mèo kề bên phản ứng).</summary>
    public void ReactFace()
    {
        GameplayVisuals visuals = GameplayVisuals.Instance;
        if (visuals == null || face == null || !face.gameObject.activeSelf || _faceWidth <= 0f) return;
        CatFaceAnimation anim = visuals.GetRandomFace();
        for (int i = 0; i < 3 && anim == _faceAnim; i++) anim = visuals.GetRandomFace(); // tránh trùng mặt cũ
        if (anim == null || anim.frames.Count == 0) return;
        _faceAnim = anim;
        face.Play(anim, visuals.faceFps, _faceWidth);
    }

    private void ResetPose()
    {
        transform.position = _basePosition;
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Thắng: thân + tai phủ trắng tới <paramref name="strength"/> rồi tan dần (mèo sáng bừng như bản gốc).
    /// </summary>
    public void PlayWinFlash(float delay = 0f, float strength = 0.6f, float rise = 0.08f, float fall = 0.32f)
    {
        _flashAnim?.Kill();
        SpriteRenderer bodyFlash = EnsureFlash(ref _bodyFlash, body, "BodyFlash");
        SpriteRenderer earsFlash = ears != null && ears.gameObject.activeSelf ? EnsureFlash(ref _earsFlash, ears, "EarsFlash") : null;
        SetFlash(0f);

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.AppendInterval(delay);
        seq.Append(DOVirtual.Float(0f, strength, rise, SetFlash).SetEase(Ease.OutQuad));
        seq.Append(DOVirtual.Float(strength, 0f, fall, SetFlash).SetEase(Ease.InQuad));
        seq.OnKill(() => { if (this != null) SetFlash(0f); });
        _flashAnim = seq;

        void SetFlash(float a)
        {
            if (bodyFlash != null) bodyFlash.color = new Color(1f, 1f, 1f, a);
            if (earsFlash != null) earsFlash.color = new Color(1f, 1f, 1f, a);
        }
    }

    /// <summary>Lớp trắng đè lên <paramref name="src"/>: cùng sprite / kích thước / thứ tự vẽ, nhích về phía camera.</summary>
    private SpriteRenderer EnsureFlash(ref SpriteRenderer flash, SpriteRenderer src, string name)
    {
        if (src == null) return null;
        if (flash == null)
        {
            Material mat = flashMaterial;
            if (mat == null)
            {
                if (_fallbackFlashMaterial == null) _fallbackFlashMaterial = new Material(Shader.Find("GUI/Text Shader"));
                mat = _fallbackFlashMaterial;
            }
            GameObject go = new GameObject(name);
            go.transform.SetParent(src.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            flash = go.AddComponent<SpriteRenderer>();
            flash.sharedMaterial = mat;
        }
        flash.gameObject.layer = src.gameObject.layer;
        flash.sprite = src.sprite;
        flash.drawMode = src.drawMode;
        if (src.drawMode != SpriteDrawMode.Simple) flash.size = src.size;
        flash.sortingLayerID = src.sortingLayerID;
        flash.sortingOrder = src.sortingOrder;
        return flash;
    }

    /// <summary>Đưa về trạng thái gốc trước khi trả về pool.</summary>
    public void ResetView()
    {
        KillAnim();
        KillRing();
        _flashAnim?.Kill();
        _flashAnim = null;
        IsPreview = false;
        transform.localScale = Vector3.one;
        SetTint(Color.white, 1f);
    }

    // ---------- Dựng hình ----------

    private void Layout(RectInt rect, CellModel clueCell, ColorType color, GridGeometry geometry)
    {
        ResetView();
        Rect = rect;
        _color = color;
        _cellSize = geometry.Spacing;

        _basePosition = geometry.RectCenter(rect);
        _basePosition.z = BlockZ;
        transform.position = _basePosition;

        float width = (rect.width - 1) * _cellSize + _cellSize * BodyFill;
        float height = (rect.height - 1) * _cellSize + _cellSize * BodyFill;
        _bodySize = new Vector2(width, height);

        GameplayVisuals visuals = GameplayVisuals.Instance;
        Vector3 clueWorld = geometry.CellToWorld(clueCell.Position);
        Vector3 faceWorld = geometry.CellToWorld(PickFaceCell(rect, clueCell.Position));

        SetupBody(visuals, color, width, height);
        numberText.transform.position = clueWorld + new Vector3(0f, 0f, -0.06f);
        SetNumber(clueCell);
        SetupFace(visuals, faceWorld, rect);
        SetupEars(visuals, color, faceWorld, height);
    }

    private void SetupBody(GameplayVisuals visuals, ColorType color, float width, float height)
    {
        SetBodyColor(visuals, color);
        body.size = new Vector2(width, height);
    }

    private void SetBodyColor(GameplayVisuals visuals, ColorType color)
    {
        Sprite sprite = visuals != null ? visuals.GetBlockSprite(color) : null;
        if (sprite != null)
        {
            body.sprite = sprite;
            _bodyBaseColor = Color.white;
        }
        else
        {
            // Chưa cấu hình sprite màu: giữ sprite trắng của prefab và tô màu
            _bodyBaseColor = color.ToColor();
        }
        _numberBaseColor = NumberColor(color);
    }

    /// <summary>Số trên khối: cùng tông màu khối, đậm và tối hơn (như bản gốc: khối đỏ số đỏ sẫm, khối xanh số xanh sẫm).</summary>
    private static Color NumberColor(ColorType color)
    {
        Color.RGBToHSV(color.ToColor(), out float h, out float s, out float v);
        return TextColor.Display(Color.HSVToRGB(h, Mathf.Clamp01(s * 1.15f + 0.1f), v * 0.55f));
    }

    // [MECHANIC] Số hiện trên khối mèo (ô "?" hiện "?" khi xem trước)
    private void SetNumber(CellModel clueCell)
    {
        ClueCell clue = clueCell.Clue;
        bool show = clue != null;
        numberText.gameObject.SetActive(show);
        if (!show) return;

        if (clue.IsHidden) numberText.SetText("?");
        else numberText.SetText("{0}", clue.Value);
    }

    /// <summary>Mặt ở ô trên-phải; nếu trùng ô số thì chuyển xuống ô dưới-trái.</summary>
    private static Vector2Int PickFaceCell(RectInt rect, Vector2Int cluePos)
    {
        if (rect.width * rect.height == 1) return cluePos;
        Vector2Int topRight = new Vector2Int(rect.xMax - 1, rect.yMax - 1);
        return topRight != cluePos ? topRight : new Vector2Int(rect.xMin, rect.yMin);
    }

    private void SetupFace(GameplayVisuals visuals, Vector3 faceWorld, RectInt rect)
    {
        CatFaceAnimation anim = visuals != null ? visuals.GetRandomFace() : null;
        bool show = anim != null && anim.frames.Count > 0;
        face.gameObject.SetActive(show);
        if (!show) return;

        bool single = rect.width * rect.height == 1;
        float faceWidth = _cellSize * (single ? 0.5f : 0.72f);
        Vector3 pos = faceWorld;
        if (single) pos += new Vector3(0f, -_cellSize * 0.28f, 0f); // khối 1 ô: mặt nhỏ dưới số

        face.transform.position = new Vector3(pos.x, pos.y, BlockZ - 0.06f);
        _faceWidth = faceWidth;
        _faceAnim = anim;
        face.Play(anim, visuals.faceFps, faceWidth);
    }

    private void SetupEars(GameplayVisuals visuals, ColorType color, Vector3 faceWorld, float blockHeight)
    {
        Sprite sprite = visuals != null ? visuals.GetEarSprite(color) : null;
        bool show = sprite != null;
        ears.gameObject.SetActive(show);
        if (!show) return;

        ears.sprite = sprite;
        float k = _cellSize * earWidth / sprite.bounds.size.x;
        ears.transform.localScale = new Vector3(k, k, 1f);

        // Đỉnh tai chạm đáy khối bên trên (qua khe hở), phần còn lại lún vào thân khối này
        float earHeight = sprite.bounds.size.y * k;
        float gap = _cellSize * (1f - BodyFill);
        float tipY = _basePosition.y + blockHeight / 2f + gap + _cellSize * earOverlap;
        ears.transform.position = new Vector3(faceWorld.x, tipY - earHeight / 2f, BlockZ - 0.01f);
    }

    /// <summary>Nhân màu cho mọi phần của mèo (thân, tai, mặt, số).</summary>
    private void SetTint(Color tint, float alpha)
    {
        tint.a = alpha;
        body.color = _bodyBaseColor * tint;
        ears.color = tint;
        face.SetColor(tint);
        Color n = _numberBaseColor;
        n.a = alpha;
        numberText.color = n;
    }

    // ---------- Animation (DOTween) ----------

    // Khối tắt (trả về pool, đổi level, thoát Play) thì hủy tween; callback onDone của PlayRemove không chạy nữa
    private void OnDisable()
    {
        KillAnim();
        KillRing();
        _flashAnim?.Kill();
        _flashAnim = null;
    }

    private void KillAnim()
    {
        _anim?.Kill();
        _anim = null;
    }

    private void KillRing()
    {
        _ringAnim?.Kill();
        _ringAnim = null;
        if (ring != null) ring.gameObject.SetActive(false);
    }

    private void PlayAnim(Tween tween)
    {
        KillAnim();
        transform.position = _basePosition;
        Tween t = tween.SetLink(gameObject);
        t.OnKill(() => { if (_anim == t) _anim = null; }); // Kill trong callback có thể bị hoãn: chỉ xóa nếu vẫn là tween này
        _anim = t;
    }

    /// <summary>
    /// Khối "đáp" xuống: dẹt (ngang to, dọc thấp) -> dài -> dẹt nhẹ -> đứng yên, biên độ tắt dần.
    /// Đáy khối giữ nguyên chỗ (dịch tâm theo chiều cao) để trông như rơi xuống chạm đất.
    /// </summary>
    private Tween PopIn()
    {
        transform.localScale = Vector3.one;
        return DOVirtual.Float(0f, 1f, squashDuration, p =>
            {
                Vector2 s = FeelCurves.Squash(p, squashAmount, squashWobbles);
                transform.localScale = new Vector3(s.x, s.y, 1f);
                transform.position = _basePosition + new Vector3(0f, FeelCurves.BottomAnchorOffset(s.y, _bodySize.y), 0f);
            })
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                transform.localScale = Vector3.one;
                transform.position = _basePosition;
            });
    }

    /// <summary>
    /// Viền màu mèo hiện thụt vào trong mép khối, nở ra đúng mép, mỏng dần và mờ đi.
    /// Nét viền là 9-slice nên độ dày chỉnh bằng scale, kích thước bằng size / scale.
    /// </summary>
    private Tween RingIn()
    {
        if (ring == null) return null;

        Color c = Color.Lerp(_color.ToColor(), Color.white, ringWhiten);
        ring.gameObject.SetActive(true);
        ring.transform.localPosition = new Vector3(0f, 0f, -0.08f);

        Tween tween = null;
        tween = DOVirtual.Float(0f, 1f, ringDuration, p =>
            {
                float ease = 1f - (1f - p) * (1f - p); // nhanh lúc đầu, chậm dần khi chạm mép
                float inset = Mathf.Lerp(ringStartInset, 0f, ease);
                float k = Mathf.Lerp(ringStartThickness, ringEndThickness, ease);
                Vector2 size = _bodySize - Vector2.one * (inset * 2f);
                ring.transform.localScale = new Vector3(k, k, 1f);
                ring.size = size / k;

                c.a = 1f - p * p; // giữ rõ rồi tắt nhanh ở cuối
                ring.color = c;
            })
            .SetEase(Ease.Linear)
            .SetLink(gameObject)
            .OnComplete(() => ring.gameObject.SetActive(false))
            .OnKill(() => { if (_ringAnim == tween) _ringAnim = null; });
        return tween;
    }

    /// <summary>Khối sai khi lưới kín: giữ màu mèo, rung + viền đỏ nháy rồi tắt viền, khối ở lại.</summary>
    private Tween ShakeRoutine()
    {
        bool useRing = ring != null;
        transform.localScale = Vector3.one;
        SetTint(Color.white, 1f);
        KillRing();

        Color rc = rejectRingColor;
        if (useRing)
        {
            ring.gameObject.SetActive(true);
            ring.transform.localPosition = new Vector3(0f, 0f, -0.08f);
            float k = rejectRingThickness;
            ring.transform.localScale = new Vector3(k, k, 1f);
            ring.size = (_bodySize + Vector2.one * (rejectRingOutset * 2f)) / k;
        }

        Sequence seq = DOTween.Sequence();

        // Rung ngang, viền đỏ nháy rejectRingBlinks nhịp
        seq.Append(DOVirtual.Float(0f, 1f, rejectShakeDuration, p =>
        {
            float t = p * rejectShakeDuration;
            float offset = Mathf.Sin(t * rejectShakeFrequency) * rejectShakeStrength * (1f - p);
            transform.position = _basePosition + new Vector3(offset, 0f, 0f);
            if (useRing)
            {
                rc.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Cos(p * Mathf.PI * rejectRingBlinks));
                ring.color = rc;
            }
        }).SetEase(Ease.Linear));
        seq.AppendCallback(() =>
        {
            transform.position = _basePosition;
            if (ring != null) ring.gameObject.SetActive(false);
        });
        return seq;
    }
}
