using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Gom các sprite dùng trong màn chơi. Asset ở Configs/GameplayVisuals, GameBootstrap truyền vào component cần (Init).
    /// Mảng blockSprites / earSprites đánh index theo thứ tự enum ColorType.
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayVisuals", menuName = "ScriptableObjects/GameplayVisuals")]
    public class GameplayVisuals : ScriptableObject
    {
        [Header("Khối mèo (theo thứ tự ColorType)")]
        public Sprite[] blockSprites;
        public Sprite[] earSprites;

        [Header("Mặt mèo: mỗi phần tử là 1 chuỗi khung hình")]
        public CatFaceAnimation[] faces;
        public float faceFps = 24f;

        [Header("Phản hồi")]
        public Sprite bubbleSprite;
        public Sprite[] praiseSprites;

        [Header("Booster")]
        [Tooltip("Khung xanh trên từng ô của vùng Hint đang gợi ý (khung_reveal, Cell/WaitSameHint gốc), 9-slice")]
        public Sprite hintCellFrame;


        public Sprite GetBlockSprite(ColorType color) => Pick(blockSprites, (int)color);
        public Sprite GetEarSprite(ColorType color) => Pick(earSprites, (int)color);

        public CatFaceAnimation GetRandomFace()
        {
            if (faces == null || faces.Length == 0) return null;
            return faces[UnityEngine.Random.Range(0, faces.Length)];
        }

        public Sprite GetRandomPraise()
        {
            if (praiseSprites == null || praiseSprites.Length == 0) return null;
            return praiseSprites[UnityEngine.Random.Range(0, praiseSprites.Length)];
        }

        private static Sprite Pick(Sprite[] arr, int index)
        {
            if (arr == null || arr.Length == 0) return null;
            return arr[Mathf.Clamp(index, 0, arr.Length - 1)];
        }
    }

    [Serializable]
    public class CatFaceAnimation
    {
        public string name;
        public List<Sprite> frames = new List<Sprite>();
    }
}
