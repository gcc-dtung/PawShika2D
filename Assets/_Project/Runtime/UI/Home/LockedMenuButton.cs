using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Game
{
    /// <summary>
    /// Nút tính năng chưa mở trên Home (theme Halloween: vạc, rương, bóng bay...): chỉ hiện nhãn "Lv X".
    /// Bấm thì icon lắc "không được" và nhãn nảy lên để báo cần lên cấp; không mở gì.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LockedMenuButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform icon;
        [SerializeField] private RectTransform label;
        [SerializeField] private float shake = 14f;
        [SerializeField] private float duration = 0.4f;

        private Sequence _anim;

        private void Awake()
        {
            button.onClick.AddListener(Play);
        }

        private void OnDisable()
        {
            _anim?.Kill();
            if (icon != null) icon.localRotation = Quaternion.identity;
            if (label != null) label.localScale = Vector3.one;
        }

        private void Play()
        {
            _anim?.Kill();
            if (icon != null) icon.localRotation = Quaternion.identity;
            if (label != null) label.localScale = Vector3.one;
            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            if (icon != null) seq.Join(icon.DOPunchRotation(new Vector3(0f, 0f, shake), duration, 8, 0.5f));
            if (label != null) seq.Join(label.DOPunchScale(Vector3.one * 0.25f, duration, 5, 0.5f));
            seq.OnKill(() => { if (_anim == seq) _anim = null; });
            _anim = seq;
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (button == null) button = GetComponent<Button>();
        }
#endif
    }
}
