using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gom các sprite dùng trong màn chơi. Đặt asset tại Resources/GameplayVisuals để mọi script lấy qua Instance.
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

    [Header("Ô đặc biệt")]
    public Sprite lockBodySprite;
    public Sprite lockShackleSprite;
    public Sprite arrowSprite;

    [Header("Phản hồi")]
    public Sprite bubbleSprite;
    public Sprite[] praiseSprites;

    private static GameplayVisuals _instance;
    public static GameplayVisuals Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<GameplayVisuals>("GameplayVisuals");
            return _instance;
        }
    }

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
