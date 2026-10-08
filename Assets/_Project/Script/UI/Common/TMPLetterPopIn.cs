using TMPro;
using UnityEngine;

/// <summary>
/// Chữ hiện lần lượt từng ký tự: mỗi chữ bắt đầu to (startScale), xoay lệch, trong suốt, rồi thu về đúng cỡ (OutBack nảy nhẹ),
/// hiện dần và xoay về thẳng; chữ sau bắt đầu trễ letterDelay. Sửa trực tiếp đỉnh lưới chữ của TextMeshPro (không tách object),
/// giữ nguyên màu / gradient / viền của text. Chạy theo thời gian thực. Hide() ẩn hết chữ (gọi trước Play nếu muốn chờ).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TMPLetterPopIn : MonoBehaviour
{
    [SerializeField] private float letterDelay = 0.06f;
    [SerializeField] private float letterDuration = 0.38f;
    [SerializeField] private float startScale = 2.4f;
    [Tooltip("Góc lệch lớn nhất lúc bắt đầu (độ), mỗi chữ ngẫu nhiên trong ±")]
    [SerializeField] private float startRotation = 16f;
    [Tooltip("Độ nảy khi thu về cỡ chuẩn (OutBack overshoot)")]
    [SerializeField] private float overshoot = 1.7f;
    [Tooltip("Phần đầu của mỗi chữ để hiện từ trong suốt lên rõ")]
    [SerializeField, Range(0.05f, 1f)] private float fadeRatio = 0.35f;

    private TMP_Text _text;
    private TMP_MeshInfo[] _source;
    private float[] _rotations;
    private float _time;
    private float _duration;
    private bool _playing;
    private bool _hidden;

    /// <summary>Thời gian từ lúc Play tới khi chữ cuối về đúng cỡ.</summary>
    public float Duration
    {
        get
        {
            if (_source == null) Prepare();
            return _duration;
        }
    }

    private TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        _playing = false;
        _hidden = false;
        if (_text != null) _text.ForceMeshUpdate(); // về trạng thái chữ bình thường
    }

    public void Play()
    {
        Prepare();
        _time = 0f;
        _playing = true;
        _hidden = false;
        Apply(0f);
    }

    public void Hide()
    {
        Prepare();
        _playing = false;
        _hidden = true;
        HideAll(); // mọi chữ chưa tới lượt: trong suốt
    }

    /// <summary>Hiện ngay đủ chữ (vd bỏ qua hiệu ứng).</summary>
    public void Finish()
    {
        _playing = false;
        _hidden = false;
        Text.ForceMeshUpdate();
    }

    private void Prepare()
    {
        TMP_Text t = Text;
        t.ForceMeshUpdate();
        _source = t.textInfo.CopyMeshInfoVertexData();
        int n = t.textInfo.characterCount;
        _rotations = new float[n];
        for (int i = 0; i < n; i++) _rotations[i] = Random.Range(-startRotation, startRotation);
        _duration = (Mathf.Max(1, n) - 1) * letterDelay + letterDuration;
    }

    // Đang ẩn: TMP dựng lại lưới chữ (đổi text, layout...) thì ẩn lại; không đẩy lưới lên GPU mỗi frame
    private void OnTextChanged(Object obj)
    {
        if (_hidden && obj == _text) HideAll();
    }

    // LateUpdate: sau mọi thay đổi layout trong frame, trước khi canvas vẽ
    private void LateUpdate()
    {
        if (!_playing) return;
        _time += Time.unscaledDeltaTime;
        if (_time >= _duration)
        {
            Finish();
            return;
        }
        Apply(_time);
    }

    private void Apply(float time)
    {
        TMP_TextInfo info = Text.textInfo;
        if (_source == null || info.meshInfo.Length != _source.Length) return;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex;
            int v = ch.vertexIndex;
            Vector3[] src = _source[m].vertices;
            Color32[] srcCol = _source[m].colors32;
            Vector3[] dst = info.meshInfo[m].vertices;
            Color32[] dstCol = info.meshInfo[m].colors32;

            float p = time < 0f ? 0f : Mathf.Clamp01((time - i * letterDelay) / letterDuration);
            float scale = Mathf.LerpUnclamped(startScale, 1f, OutBack(p));
            float rot = Mathf.Lerp(_rotations[i], 0f, 1f - (1f - p) * (1f - p));
            float alpha = time < 0f ? 0f : Mathf.Clamp01(p / fadeRatio);

            Vector3 center = (src[v] + src[v + 2]) * 0.5f; // giữa góc dưới trái và trên phải
            Matrix4x4 mtx = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, rot), Vector3.one * scale) * Matrix4x4.Translate(-center);
            for (int k = 0; k < 4; k++)
            {
                dst[v + k] = mtx.MultiplyPoint3x4(src[v + k]);
                Color32 c = srcCol[v + k];
                c.a = (byte)(c.a * alpha);
                dstCol[v + k] = c;
            }
        }
        Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary>Mọi chữ trong suốt, đọc thẳng từ lưới hiện tại (không cần _source khớp số chữ).</summary>
    private void HideAll()
    {
        TMP_TextInfo info = Text.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;
            Color32[] col = info.meshInfo[ch.materialReferenceIndex].colors32;
            for (int k = 0; k < 4; k++) col[ch.vertexIndex + k].a = 0;
        }
        Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private float OutBack(float p)
    {
        float c = overshoot + 1f;
        float x = p - 1f;
        return 1f + c * x * x * x + overshoot * x * x;
    }
}
