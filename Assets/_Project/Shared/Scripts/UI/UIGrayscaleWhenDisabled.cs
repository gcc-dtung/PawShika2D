using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>
    /// Nút bị tắt (interactable = false) thì chuyển xám thay vì tối đi: mọi Image con dùng material grayscale,
    /// chữ TMP con đổi sang material viền xám. Bật lại thì trả material gốc.
    /// Nút nên đặt ColorBlock.disabledColor = trắng để không bị tối thêm.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIGrayscaleWhenDisabled : MonoBehaviour
    {
        [SerializeField] private Material grayMaterial;
        [SerializeField] private Material grayTextMaterial;

        private Button _button;
        private Image[] _images;
        private Material[] _imageMaterials;
        private TMP_Text[] _texts;
        private Material[] _textMaterials;
        private bool _gray;
        private bool _cached;

        private void Awake() => Cache();

        private void OnEnable()
        {
            Cache();
            Apply(!_button.IsInteractable());
        }

        private void Update()
        {
            bool gray = !_button.IsInteractable();
            if (gray != _gray) Apply(gray);
        }

        private void Cache()
        {
            if (_cached) return;
            _cached = true;
            _button = GetComponent<Button>();
            _images = GetComponentsInChildren<Image>(true);
            _imageMaterials = new Material[_images.Length];
            for (int i = 0; i < _images.Length; i++) _imageMaterials[i] = _images[i].material == _images[i].defaultMaterial ? null : _images[i].material;
            _texts = GetComponentsInChildren<TMP_Text>(true);
            _textMaterials = new Material[_texts.Length];
            for (int i = 0; i < _texts.Length; i++) _textMaterials[i] = _texts[i].fontSharedMaterial;
        }

        private void Apply(bool gray)
        {
            _gray = gray;
            for (int i = 0; i < _images.Length; i++)
            {
                if (_images[i] != null) _images[i].material = gray && grayMaterial != null ? grayMaterial : _imageMaterials[i];
            }
            for (int i = 0; i < _texts.Length; i++)
            {
                if (_texts[i] != null && (grayTextMaterial != null || !gray)) _texts[i].fontSharedMaterial = gray ? grayTextMaterial : _textMaterials[i];
            }
        }
    }
}
