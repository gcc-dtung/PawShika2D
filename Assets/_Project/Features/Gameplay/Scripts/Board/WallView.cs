using DG.Tweening;
using UnityEngine;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Hiển thị 1 đoạn tường (hàng rào) giống ảnh popup giới thiệu: thanh gỗ có viền đậm, 2 cọc tròn ở 2 đầu.
    /// Đoạn tường nằm trên đường kẻ giữa 2 ô, dài đúng 1 cạnh ô, cọc nằm ở giao điểm các đường kẻ:
    /// 2 đoạn nối nhau (thẳng hoặc vuông góc) dùng chung 1 cọc nên ghép thành hàng rào liền / góc vuông.
    /// Vẽ trên khối mèo (khối không bao giờ vắt qua tường). Thứ tự vẽ: viền thanh &lt; thanh &lt; viền cọc &lt; cọc,
    /// nên cọc chung của 2 đoạn luôn đè lên cả 2 thanh.
    /// Bản thử: chưa có art, dùng ProceduralSprites (Pill cho thanh, Disc cho cọc). Object con dựng sẵn bằng EditorBuild.
    /// </summary>
    public class WallView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer barOutline;
        [SerializeField] private SpriteRenderer bar;
        [SerializeField] private SpriteRenderer[] postOutlines = new SpriteRenderer[2];
        [SerializeField] private SpriteRenderer[] posts = new SpriteRenderer[2];

        [Header("Hình")]
        [SerializeField] private Color wood = new Color(0.91f, 0.67f, 0.41f);
        [SerializeField] private Color outline = new Color(0.48f, 0.29f, 0.16f);
        [SerializeField] private float barThickness = 0.11f;
        [SerializeField] private float postSize = 0.2f;
        [Tooltip("Độ dày viền đậm quanh thanh và cọc (world)")]
        [SerializeField] private float outlineWidth = 0.025f;
        [Tooltip("Thứ tự vẽ của viền thanh; thanh +1, viền cọc +2, cọc +3. Trên khối (20–22), dưới khung chọn (30)")]
        [SerializeField] private int sortingOrder = 23;

        [Header("Ngón tay đang kéo đẩy vào tường: tường bị đẩy ra theo hướng đẩy rồi bật về")]
        [SerializeField] private float pushDistance = 0.07f;
        [SerializeField] private float pushDuration = 0.24f;

        private Vector3 _basePos;
        private Tween _anim;

        public WallData Data { get; private set; }

        public void Show(WallData data, GridGeometry geometry)
        {
            KillAnim();
            Data = data;

            // Gốc ở giữa đoạn; trục x cục bộ chạy dọc theo tường (tường Right đứng dọc -> xoay 90°)
            _basePos = (geometry.CellToWorld(WallRules.CellA(data)) + geometry.CellToWorld(WallRules.CellB(data))) / 2f;
            transform.position = _basePos;
            transform.rotation = data.side == WallSide.Right ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
            transform.localScale = Vector3.one;

            float half = geometry.Spacing / 2f;
            SetBar(barOutline, geometry.Spacing, barThickness + outlineWidth * 2f, outline, sortingOrder);
            SetBar(bar, geometry.Spacing, barThickness, wood, sortingOrder + 1);
            for (int i = 0; i < 2; i++)
            {
                Vector3 end = new Vector3(i == 0 ? -half : half, 0f, 0f);
                SetPost(postOutlines[i], end, postSize + outlineWidth * 2f, outline, sortingOrder + 2);
                SetPost(posts[i], end, postSize, wood, sortingOrder + 3);
            }
        }

        // Pill nằm ngang, 9-slice 2 đầu: vẽ to rồi thu nhỏ transform để đầu tròn không méo (như ô hướng của CellView)
        private static void SetBar(SpriteRenderer sr, float length, float thickness, Color color, int order)
        {
            if (sr == null) return;
            if (sr.sprite == null) sr.sprite = ProceduralSprites.Pill;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.color = color;
            sr.sortingOrder = order;
            float spriteHeight = sr.sprite.bounds.size.y;
            float k = thickness / spriteHeight;
            sr.transform.localPosition = Vector3.zero;
            sr.transform.localScale = new Vector3(k, k, 1f);
            sr.size = new Vector2((length + thickness) / k, spriteHeight); // 2 đầu tròn chui vào dưới cọc
        }

        private static void SetPost(SpriteRenderer sr, Vector3 localPos, float size, Color color, int order)
        {
            if (sr == null) return;
            if (sr.sprite == null) sr.sprite = ProceduralSprites.Disc;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = color;
            sr.sortingOrder = order;
            float k = size / sr.sprite.bounds.size.x;
            sr.transform.localPosition = localPos;
            sr.transform.localScale = new Vector3(k, k, 1f);
        }

        /// <summary>Hiện cùng lúc với số trên ô (sau khi các ô nảy xong): nảy từ giữa ra (đều 2 chiều để cọc không méo).</summary>
        public void PlayIntro(float delay, float duration)
        {
            KillAnim();
            transform.localScale = Vector3.zero;
            _anim = transform.DOScale(Vector3.one, duration).SetDelay(delay).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        /// <summary>Ngón tay đẩy vào tường theo <paramref name="dir"/> (1 trục): tường bị đẩy ra 1 chút rồi bật về.</summary>
        public void PlayPushed(Vector2Int dir)
        {
            if (dir == Vector2Int.zero || !gameObject.activeInHierarchy) return;
            KillAnim();
            Vector3 offset = new Vector3(dir.x, dir.y, 0f).normalized * pushDistance;
            _anim = DOVirtual.Float(0f, 1f, pushDuration, p => transform.position = _basePos + offset * Mathf.Sin(p * Mathf.PI))
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnKill(() => { if (this != null) transform.position = _basePos; });
        }

        private void KillAnim()
        {
            _anim?.Kill(true);
            _anim = null;
            transform.position = _basePos;
            transform.localScale = Vector3.one;
        }

        private void OnDisable() => KillAnim();

#if UNITY_EDITOR
        /// <summary>Editor: dựng object con (thanh + viền, 2 cọc + viền). Chạy 1 lần (menu chuột phải component) rồi lưu prefab.</summary>
        [ContextMenu("Dựng sẵn object con")]
        public void EditorBuild()
        {
            barOutline = Child("BarOutline");
            bar = Child("Bar");
            postOutlines = new[] { Child("PostOutlineA"), Child("PostOutlineB") };
            posts = new[] { Child("PostA"), Child("PostB") };
            SpriteRenderer own = GetComponent<SpriteRenderer>();
            if (own != null) DestroyImmediate(own, true); // bản cũ vẽ bằng SpriteRenderer trên chính object
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private SpriteRenderer Child(string childName)
        {
            Transform t = transform.Find(childName);
            if (t == null)
            {
                t = new GameObject(childName).transform;
                t.SetParent(transform, false);
            }
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            return sr != null ? sr : t.gameObject.AddComponent<SpriteRenderer>();
        }
#endif
    }
}
