using System;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Mở khóa booster bằng tutorial (như bản gốc): booster khóa tới khi lần đầu vào level >= levelUnlock (BoosterDatabase).
    /// Là 1 ILevelIntro (Order 20, sau băng HARD và popup cơ chế): màn hình tối lại, chỉ nút booster đó sáng + bàn tay chỉ vào,
    /// lời nhắc lấy từ BoosterData.detail. Người chơi phải bấm nút: BoosterManager báo TutorialPressed -> mở khóa
    /// (lưu SaveData.unlockedBoosters), lần dùng đầu tiên miễn phí. Mỗi lần nạp level chỉ chạy tối đa 1 tutorial (booster mở sớm nhất trước).
    /// Bước riêng sau khi mở khóa (vd Đóng băng: báo đồng hồ đứng yên) nghe BoosterTutorialCompletedEvent (FreezeTutorialDemo),
    /// nên thêm booster mới không phải sửa class này.
    /// </summary>
    public class BoosterTutorial : MonoBehaviour, ILevelIntro
    {
        private BoosterManager _boosterManager;
        private ISaveService _save;
        private GameplaySave _data;

        /// <summary>GameBootstrap gọi trước Start: section "gameplay".</summary>
        public void Init(ISaveService save, BoosterManager boosterManager)
        {
            _save = save;
            _data = save.Get<GameplaySave>(GameplaySave.Key);
            _boosterManager = boosterManager;
        }

        [SerializeField] private BoosterTutorialView view;
        [Tooltip("Chờ thêm sau khi lưới hiện xong rồi mới tối màn hình")]
        [SerializeField] private float startDelay = 0.3f;

        private BoosterManager _boosters;
        private Action _onDone;
        private BoosterType _target;
        private bool _active;

        private void Start()
        {
            _boosters = _boosterManager;
            if (_boosters != null) _boosters.TutorialPressed += OnTutorialPressed;
        }

        private void OnDestroy()
        {
            if (_boosters != null) _boosters.TutorialPressed -= OnTutorialPressed;
        }

        // ---------- ILevelIntro ----------

        public int Order => 20;
        public float StartDelay => startDelay;

        public bool ShouldPlay(LevelIntroContext level) => !level.IsDaily && NextToUnlock(level.Number) != null;

        public void Play(LevelIntroContext level, Action onDone)
        {
            BoosterData data = NextToUnlock(level.Number);
            BoosterButton button = data != null ? BoosterButton.Find(data.boosterType) : null;
            if (data == null || button == null || view == null || _boosters == null)
            {
                // Không có giao diện: mở khóa luôn để người chơi không bị kẹt booster
                if (data != null) Unlock(data.boosterType);
                onDone?.Invoke();
                return;
            }

            _target = data.boosterType;
            _active = true;
            _onDone = onDone;
            _boosters.BeginTutorial(_target); // chỉ nút này bấm được, coi như đã mở (hiện icon + số lượt)
            view.Show(button, Message(data, level.Level));
        }

        public void Cancel()
        {
            if (!_active) return;
            _active = false;
            _onDone = null;
            if (_boosters != null) _boosters.EndTutorial();
            if (view != null) view.Hide();
        }

        // ---------- Luồng ----------

        /// <summary>Booster chưa mở có levelUnlock nhỏ nhất mà level <paramref name="levelNumber"/> đã đạt tới.</summary>
        private BoosterData NextToUnlock(int levelNumber)
        {
            BoosterManager bm = _boosters != null ? _boosters : _boosterManager;
            if (bm == null || bm.Database == null) return null;
            BoosterData best = null;
            foreach (BoosterData d in bm.Database.boosterDatas)
            {
                if (d == null || _data.IsBoosterUnlocked(d.boosterType) || levelNumber < d.levelUnlock) continue;
                if (best == null || d.levelUnlock < best.levelUnlock) best = d;
            }
            return best;
        }

        /// <summary>
        /// Lời nhắc = BoosterData.detail (dùng chung với popup mua). Nếu Đóng băng mở ở level Hard (đổi levelUnlock):
        /// nói rõ vì sao nên bật ngay ở level này.
        /// </summary>
        private static string Message(BoosterData data, LevelDefinition level)
        {
            bool hard = level != null && level.difficulty != LevelDifficulty.Normal;
            return data.boosterType == BoosterType.Freeze && hard ? "Tough level! " + data.detail : data.detail;
        }

        /// <summary>BoosterManager báo người chơi bấm đúng nút: mở khóa, đóng lớp tối (booster chạy ngay sau đó).</summary>
        private void OnTutorialPressed(BoosterType type)
        {
            if (!_active || type != _target) return;
            _active = false;
            if (view != null) view.Hide();
            Unlock(type);

            Action done = _onDone;
            _onDone = null;
            done?.Invoke(); // mở khóa lưới ngay, BoosterManager dùng booster tiếp trong cùng lần bấm
            EventBus<BoosterTutorialCompletedEvent>.Raise(new BoosterTutorialCompletedEvent(type));
        }

        private void Unlock(BoosterType type)
        {
            _data.UnlockBooster(type);
            _save.MarkDirty();
            EventBus<BoosterLockChangedEvent>.Raise(new BoosterLockChangedEvent(type, true));
        }
    }
}
