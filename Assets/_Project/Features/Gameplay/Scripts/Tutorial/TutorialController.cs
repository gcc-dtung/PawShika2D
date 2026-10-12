using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Tutorial lần đầu mở game (như bản gốc): 4 màn nhỏ chơi bằng gameplay thật, có lời nhắc, vạch tiến độ, bàn tay kéo mẫu.
    /// - Chạy khi save chưa có tutorialDone và người chơi còn ở level 1 (người chơi cũ không bị bắt chơi lại).
    /// - Trong tutorial: đặt sai không trừ mạng (GridManager), thắng màn không hiện màn WELL DONE (LevelManager).
    /// - Xong bước cuối hoặc bấm Skip: lưu tutorialDone, vào thẳng level hiện tại (level 1).
    /// </summary>
    public class TutorialController : Singleton<TutorialController>
    {
        [SerializeField] private TutorialView view;
        [SerializeField] private TutorialHand hand;
        [Tooltip("Chờ lưới lóe sáng xong rồi mới sang bước kế")]
        [SerializeField] private float nextStepDelay = 1.1f;

        private List<TutorialLevels.Step> _steps;
        private int _index = -1;
        private Tween _pending;

        /// <summary>Đang trong tutorial.</summary>
        public static bool Active { get; private set; }

        /// <summary>Lần mở game này cần chạy tutorial (ScreenFlow / LevelManager hỏi lúc Start).</summary>
        public static bool Pending =>
            Instance != null && Instance.isActiveAndEnabled && !SaveSystem.Data.tutorialDone && SaveSystem.Data.currentLevel == 0;

        private void OnEnable()
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.onGameWin.AddListener(OnStepWon);
                GridManager.Instance.onBlockPlaced.AddListener(OnBlockPlaced);
                GridManager.Instance.onBlockRemoved.AddListener(OnBlockPlaced);
            }
            if (view != null && view.SkipButton != null) view.SkipButton.onClick.AddListener(Skip);
        }

        private void OnDisable()
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.onGameWin.RemoveListener(OnStepWon);
                GridManager.Instance.onBlockPlaced.RemoveListener(OnBlockPlaced);
                GridManager.Instance.onBlockRemoved.RemoveListener(OnBlockPlaced);
            }
            if (view != null && view.SkipButton != null) view.SkipButton.onClick.RemoveListener(Skip);
            _pending?.Kill();
            Active = false;
        }

        /// <summary>Bắt đầu tutorial từ bước 1 (ScreenFlow gọi khi mở game; Debug Panel gọi để xem lại).</summary>
        public void Begin()
        {
            _pending?.Kill();
            _steps = TutorialLevels.Create();
            Active = true;
            if (view != null) view.Show();
            LoadStep(0);
        }

        public void Skip()
        {
            if (Active) Finish();
        }

        private void LoadStep(int index)
        {
            _index = index;
            TutorialLevels.Step step = _steps[index];
            if (hand != null) hand.Stop();
            if (view != null) view.SetStep(index, _steps.Count, step.message);
            if (LevelManager.Instance != null) LevelManager.Instance.LoadTutorial(step.level);
            // Bàn tay chờ lưới xuất hiện xong
            _pending = DOVirtual.DelayedCall(0.6f, ShowHint).SetLink(gameObject);
        }

        private void OnBlockPlaced(BlockModel _)
        {
            if (Active) ShowHint();
        }

        /// <summary>Bàn tay kéo mẫu cho số đầu tiên (theo thứ tự gợi ý) chưa có khối đúng; hết gợi ý thì ẩn.</summary>
        private void ShowHint()
        {
            if (!Active || hand == null || GridManager.Instance == null || GridManager.Instance.Board == null) return;
            BoardModel board = GridManager.Instance.Board;
            foreach (var hint in _steps[_index].hints)
            {
                CellModel cell = board.GetCell(hint.clue);
                if (cell == null || (cell.Block != null && cell.Block.Rect.Equals(BoardModel.RectFrom(hint.from, hint.to)))) continue;
                hand.Play(board.Geometry.CellToWorld(hint.from), board.Geometry.CellToWorld(hint.to));
                return;
            }
            hand.Stop();
        }

        private void OnStepWon()
        {
            if (!Active) return;
            if (hand != null) hand.Stop();
            _pending?.Kill();
            _pending = DOVirtual.DelayedCall(nextStepDelay, () =>
            {
                if (_index + 1 < _steps.Count) LoadStep(_index + 1);
                else Finish();
            }).SetLink(gameObject);
        }

        private void Finish()
        {
            _pending?.Kill();
            Active = false;
            if (hand != null) hand.Stop();
            if (view != null) view.Hide();
            SaveSystem.Data.tutorialDone = true;
            SaveSystem.MarkDirty();
            if (LevelManager.Instance != null) LevelManager.Instance.LoadLevel(SaveSystem.Data.currentLevel);
        }
    }
}
