using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PawBlock.Features.Gameplay
{
    public enum LevelDifficulty
    {
        Normal = 0,
        Hard = 1,
        VeryHard = 2
    }

    /// <summary>Hình / chữ / màu của 1 mức khó (Hard, Very Hard).</summary>
    [Serializable]
    public class DifficultyStyle
    {
        public LevelDifficulty difficulty;
        public string label = "Hard";                 // chữ trên tab (nút PLAY, khung HUD)
        [Header("Nút PLAY (Main Menu)")]
        public Sprite playFace;                        // mặt nút (btn_hard_1 / btn_very_hard_1)
        public Sprite playBacker;                      // nền tối sau mặt nút, lộ ra khi nhấn (btn_hard_2 / btn_very_hard_2)
        public Color playTabTextColor = Color.white;   // chữ trên tab kem
        [FormerlySerializedAs("playTitleOutline")]
        public Material playLevelOutline;              // viền chữ "Level n" (TMP preset cùng font với chữ, mặt chữ giữ trắng)
        [Header("HUD")]
        public Sprite hudFrame;                        // khung có tab bao ô LEVEL (khung_hard1 / khung_expert1)
        public Material hudTabOutline;                 // viền chữ trên tab (TMP preset)
        [Header("Hiệu ứng vào màn")]
        public Sprite skull;                           // mèo đầu lâu (khung_hard_2 / khung_expert_2)
        public Sprite banner;                          // băng chữ dài quá 2 mép màn (khung_hard_3_wide / khung_expert_3_wide)
    }

    /// <summary>
    /// Theme theo độ khó của level (LevelDefinition.difficulty): Normal giữ giao diện thường;
    /// Hard / Very Hard đổi nút PLAY, khung HUD và có hiệu ứng vào màn (DifficultyIntro).
    /// </summary>
    [CreateAssetMenu(menuName = "PawBlock/Difficulty Theme", fileName = "DifficultyTheme")]
    public class DifficultyTheme : ScriptableObject
    {
        public DifficultyStyle[] styles = new DifficultyStyle[0];
        [Tooltip("Khung viền kem có tab của nút PLAY (khung_home_hard), dùng chung cho các mức khó")]
        public Sprite playFrame;
        [Tooltip("Dải băng cảnh báo sọc (khung_expert_4), dùng chung")]
        public Sprite hazardTape;

        /// <summary>Kiểu của mức khó; Normal (hoặc chưa cấu hình) trả về null.</summary>
        public DifficultyStyle Get(LevelDifficulty difficulty)
        {
            if (difficulty == LevelDifficulty.Normal) return null;
            foreach (DifficultyStyle s in styles)
            {
                if (s != null && s.difficulty == difficulty) return s;
            }
            return null;
        }
    }
}
