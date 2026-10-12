using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.DailyChallenge
{
    /// <summary>1 món trong bong thoại quà (DailyGiftBubble): icon + số lượng ("x2", "15m"). Nhân bản từ món mẫu.</summary>
    public class GiftItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amount;

        public Image Icon => icon;
        public TMP_Text Amount => amount;

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate() => EditorFill();

        public void EditorFill()
        {
            if (icon == null) icon = GetComponent<Image>();
            if (amount == null) amount = GetComponentInChildren<TMP_Text>(true);
        }
#endif
    }
}
