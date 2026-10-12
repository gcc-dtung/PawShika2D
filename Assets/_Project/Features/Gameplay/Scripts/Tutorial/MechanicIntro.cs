using System;
using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Popup giới thiệu cơ chế lần đầu người chơi gặp (Hướng, Khóa, "?"), ảnh minh họa có sẵn của bản gốc.
    /// Là 1 ILevelIntro (Order 10: sau băng HARD, trước tutorial booster). LevelIntroSequencer chờ lưới hiện xong rồi gọi Play:
    /// level có cơ chế chưa xem thì mở popup (ván tạm dừng), Ok thì lưu SaveData.seenMechanics; nhiều cơ chế mới thì lần lượt từng popup.
    /// Level giới thiệu cơ chế do Mechanic Injector xếp lịch, nên popup rơi đúng vào các level đó.
    /// </summary>
    public class MechanicIntro : MonoBehaviour, ILevelIntro
    {
        // [MECHANIC] Popup giới thiệu: thêm Kind + sprite + nhánh trong NewMechanics / ShowNext (TutorialBuilder gán sprite)
        public enum Kind
        {
            Constraint,
            Lock,
            Hidden
        }

        [SerializeField] private MechanicIntroPopup popup;
        [SerializeField] private Sprite constraintSprite;
        [SerializeField] private Sprite lockSprite;
        [SerializeField] private Sprite hiddenSprite;
        [Tooltip("Chờ thêm sau khi lưới hiện xong rồi mới mở popup")]
        [SerializeField] private float startDelay = 0.15f;

        private readonly Queue<Kind> _queue = new Queue<Kind>();
        private Action _onDone;

        // ---------- ILevelIntro ----------

        public int Order => 10;
        public float StartDelay => startDelay;

        public bool ShouldPlay(LevelIntroContext level) => popup != null && NewMechanics(level.Level).Count > 0;

        public void Play(LevelIntroContext level, Action onDone)
        {
            _queue.Clear();
            foreach (Kind kind in NewMechanics(level.Level)) _queue.Enqueue(kind);
            _onDone = onDone;
            ShowNext();
        }

        public void Cancel()
        {
            _queue.Clear();
            _onDone = null;
            if (popup != null && popup.IsOpen) popup.CloseImmediate();
        }

        /// <summary>Cơ chế có trong level mà người chơi chưa xem, theo thứ tự Mechanic Injector giới thiệu.</summary>
        private static List<Kind> NewMechanics(LevelDefinition level)
        {
            List<Kind> result = new List<Kind>();
            if (level == null || level.clues == null) return result;
            bool constraint = false, locked = false, hidden = false;
            foreach (ClueData c in level.clues)
            {
                constraint |= c.mechanic.constraint != ClueConstraint.None;
                locked |= c.mechanic.lockSteps > 0;
                hidden |= c.mechanic.hidden;
            }
            if (constraint && !Seen(Kind.Constraint)) result.Add(Kind.Constraint);
            if (locked && !Seen(Kind.Lock)) result.Add(Kind.Lock);
            if (hidden && !Seen(Kind.Hidden)) result.Add(Kind.Hidden);
            return result;
        }

        private static bool Seen(Kind kind) => SaveSystem.Data.seenMechanics.Contains(kind.ToString());

        private void ShowNext()
        {
            if (_queue.Count == 0 || popup == null)
            {
                Action done = _onDone;
                _onDone = null;
                done?.Invoke();
                return;
            }
            Kind kind = _queue.Dequeue();
            switch (kind)
            {
                case Kind.Constraint:
                    popup.Show("Orientation", "Blocks must be vertical, square, or horizontal, as the arrow shows.", constraintSprite, () => Done(kind));
                    break;
                case Kind.Lock:
                    popup.Show("Locked Number", "Place other blocks first. The lock opens after the shown number of blocks.", lockSprite, () => Done(kind));
                    break;
                default:
                    popup.Show("Hidden Number", "The \"?\" hides the number. Work out its rectangle from the cells around it.", hiddenSprite, () => Done(kind));
                    break;
            }
        }

        private void Done(Kind kind)
        {
            if (!Seen(kind)) SaveSystem.Data.seenMechanics.Add(kind.ToString());
            SaveSystem.MarkDirty();
            ShowNext();
        }
    }
}
