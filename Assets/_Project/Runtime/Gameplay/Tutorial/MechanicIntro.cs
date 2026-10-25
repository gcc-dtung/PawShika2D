using System;
using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Popup giới thiệu cơ chế lần đầu người chơi gặp (Ô trống, Hướng, Khóa, "?", Tường, Góc, Số mơ hồ), ảnh minh họa có sẵn của bản gốc (Ô trống, Tường, Góc: ảnh vẽ thêm cùng kiểu).
    /// Là 1 ILevelIntro (Order 10: sau băng HARD, trước tutorial booster). LevelIntroSequencer chờ lưới hiện xong rồi gọi Play:
    /// level có cơ chế chưa xem thì mở popup (ván tạm dừng), Ok thì lưu GameplaySave.seenMechanics; nhiều cơ chế mới thì lần lượt từng popup.
    /// Level giới thiệu cơ chế do Mechanic Injector xếp lịch, nên popup rơi đúng vào các level đó.
    /// </summary>
    public class MechanicIntro : MonoBehaviour, ILevelIntro
    {
        private ISaveService _save;
        private GameplaySave _data;

        /// <summary>GameBootstrap gọi trước Start: section "gameplay".</summary>
        public void Init(ISaveService save)
        {
            _save = save;
            _data = save.Get<GameplaySave>(GameplaySave.Key);
        }

        // [MECHANIC] Popup giới thiệu: thêm Kind + sprite + nhánh trong NewMechanics / ShowNext (sprite gán trên component MechanicIntro trong scene)
        public enum Kind
        {
            Constraint,
            Lock,
            Hidden,
            Wall, // thêm cuối: seenMechanics lưu theo tên
            Corner,
            Void,
            Ambiguous
        }

        [SerializeField] private MechanicIntroPopup popup;
        [SerializeField] private Sprite constraintSprite;
        [SerializeField] private Sprite lockSprite;
        [SerializeField] private Sprite hiddenSprite;
        [SerializeField] private Sprite wallSprite;
        [SerializeField] private Sprite cornerSprite;
        [SerializeField] private Sprite voidSprite;
        [SerializeField] private Sprite ambiguousSprite;
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
        private List<Kind> NewMechanics(LevelDefinition level)
        {
            List<Kind> result = new List<Kind>();
            if (level == null) return result;
            bool constraint = false, locked = false, hidden = false, corner = false, ambiguous = false;
            foreach (ClueData c in level.clues ?? new ClueData[0])
            {
                constraint |= c.mechanic.constraint != ClueConstraint.None;
                locked |= c.mechanic.lockSteps > 0;
                hidden |= c.mechanic.hidden;
                corner |= c.mechanic.corner != ClueCorner.None;
                ambiguous |= c.mechanic.ambiguity != ClueAmbiguity.None;
            }
            // Ô trống được giới thiệu sớm nhất (Mechanic Injector: level 5) nên hiện trước
            if (HasVoid(level) && !Seen(Kind.Void)) result.Add(Kind.Void);
            if (constraint && !Seen(Kind.Constraint)) result.Add(Kind.Constraint);
            if (locked && !Seen(Kind.Lock)) result.Add(Kind.Lock);
            if (hidden && !Seen(Kind.Hidden)) result.Add(Kind.Hidden);
            if (level.walls != null && level.walls.Length > 0 && !Seen(Kind.Wall)) result.Add(Kind.Wall);
            if (corner && !Seen(Kind.Corner)) result.Add(Kind.Corner);
            if (ambiguous && !Seen(Kind.Ambiguous)) result.Add(Kind.Ambiguous);
            return result;
        }

        private static bool HasVoid(LevelDefinition level)
        {
            if (level.cellOverrides == null) return false;
            foreach (CellOverrideData o in level.cellOverrides)
            {
                if (o.type == CellType.Void) return true;
            }
            return false;
        }

        private bool Seen(Kind kind) => _data != null && _data.seenMechanics.Contains(kind.ToString());

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
                case Kind.Void:
                    popup.Show("Empty Cells", "Gaps in the board are not cells. Blocks can't cover them, so fill only the cells you see.", voidSprite, () => Done(kind));
                    break;
                case Kind.Corner:
                    popup.Show("Corner Number", "The number must sit in the marked corner or side of its block.", cornerSprite, () => Done(kind));
                    break;
                case Kind.Wall:
                    popup.Show("Fence", "Blocks can't cross a fence. The cells on each side belong to different blocks.", wallSprite, () => Done(kind));
                    break;
                case Kind.Ambiguous:
                    popup.Show("Fuzzy Number", "Some numbers only give a hint: >5 means more than 5 cells, <5 fewer than 5, E an even count, O an odd count, 3/5 one of the two.", ambiguousSprite, () => Done(kind));
                    break;
                case Kind.Hidden:
                default:
                    popup.Show("Hidden Number", "The \"?\" hides the number. Work out its rectangle from the cells around it.", hiddenSprite, () => Done(kind));
                    break;
            }
        }

        private void Done(Kind kind)
        {
            if (!Seen(kind)) _data.seenMechanics.Add(kind.ToString());
            _save.MarkDirty();
            ShowNext();
        }
    }
}
