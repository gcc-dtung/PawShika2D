using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// Nút bị tắt (interactable = false) thì chuyển xám thay vì tối đi: mọi Image con dùng material grayscale,
    /// chữ TMP con đổi sang material viền xám. Bật lại thì trả material gốc.
    /// Nút nên đặt ColorBlock.disabledColor = trắng để không bị tối thêm.
    /// Danh sách Image / chữ con do Editor thu sẵn (OnValidate / UIAutoSetup), lúc chạy không tìm.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIGrayscaleWhenDisabled : MonoBehaviour
    {
        [SerializeField] private Material grayMaterial;
        [SerializeField] private Material grayTextMaterial;

        [Header("Editor tự điền")]
        [SerializeField] private Button button;
        [SerializeField] private Image[] images;
        [SerializeField] private TMP_Text[] texts;

        private Material[] _imageMaterials;
        private Material[] _textMaterials;
        private bool _gray;
        private bool _cached;

        private void Awake() => Cache();

        private void OnEnable()
        {
            Cache();
            Apply(!button.IsInteractable());
        }

        private void Update()
        {
            bool gray = !button.IsInteractable();
            if (gray != _gray) Apply(gray);
        }

        private void Cache()
        {
            if (_cached) return;
            _cached = true;
            _imageMaterials = new Material[images.Length];
            for (int i = 0; i < images.Length; i++) _imageMaterials[i] = images[i] == null || images[i].material == images[i].defaultMaterial ? null : images[i].material;
            _textMaterials = new Material[texts.Length];
            for (int i = 0; i < texts.Length; i++) _textMaterials[i] = texts[i] != null ? texts[i].fontSharedMaterial : null;
        }

        private void Apply(bool gray)
        {
            _gray = gray;
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null) images[i].material = gray && grayMaterial != null ? grayMaterial : _imageMaterials[i];
            }
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && (grayTextMaterial != null || !gray)) texts[i].fontSharedMaterial = gray ? grayTextMaterial : _textMaterials[i];
            }
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate() => EditorCollect();

        /// <summary>Thu lại Image / chữ con; trả về true nếu có đổi.</summary>
        public bool EditorCollect()
        {
            Button b = GetComponent<Button>();
            Image[] imgs = GetComponentsInChildren<Image>(true);
            TMP_Text[] txts = GetComponentsInChildren<TMP_Text>(true);
            bool changed = b != button || !Same(images, imgs) || !Same(texts, txts);
            button = b;
            images = imgs;
            texts = txts;
            return changed;
        }

        private static bool Same<T>(T[] a, T[] b) where T : Object
        {
            if (a == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
#endif
    }
}
