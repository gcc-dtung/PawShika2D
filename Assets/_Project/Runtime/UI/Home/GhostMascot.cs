using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Game
{
    /// <summary>
    /// Hồn ma linh vật giữa màn Home (theme Halloween). Trôi lơ lửng do UIFloat ở object cha;
    /// chạm vào thì nhún bẹp - giãn rồi xoay lắc nhẹ (DOTween), chạm liên tục thì chạy lại từ đầu.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GhostMascot : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform body;
        [SerializeField] private Vector2 squash = new Vector2(0.18f, -0.15f);
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private float wiggle = 10f;

        private Sequence _anim;

        private void Awake()
        {
            button.onClick.AddListener(Play);
        }

        private void OnDisable()
        {
            _anim?.Kill();
            body.localScale = Vector3.one;
            body.localRotation = Quaternion.identity;
        }

        private void Play()
        {
            _anim?.Kill();
            body.localScale = Vector3.one;
            body.localRotation = Quaternion.identity;
            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            seq.Append(body.DOPunchScale(new Vector3(squash.x, squash.y, 0f), duration, 6, 0.6f));
            seq.Join(body.DOPunchRotation(new Vector3(0f, 0f, wiggle), duration, 5, 0.5f));
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
