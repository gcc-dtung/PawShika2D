using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Thứ tự giữ đúng boosterType của BoosterDatabase gốc (1 = Freeze Time, nay là Khiên).</summary>
public enum BoosterType
{
    Hint = 0,
    Shield = 1,
    Reveal = 2
}

[Serializable]
public class BoosterData
{
    public BoosterType boosterType;
    public string name;
    [TextArea] public string detail;
    public int levelUnlock;  // mở từ level (số hiển thị, bắt đầu từ 1)
    public int valueStart;   // số lượt tặng khi mở khóa
    public int valueOneBuy;  // số lượt mỗi lần mua
    public int priceOneBuy;  // giá coin mỗi lần mua
    public Sprite icon;
}

/// <summary>Cấu hình 3 booster. Tên field giữ như asset gốc của Paw Block để dùng lại BoosterDatabase.asset.</summary>
[CreateAssetMenu(menuName = "PawBlock/Booster Database", fileName = "BoosterDatabase")]
public class BoosterDatabase : ScriptableObject
{
    public List<BoosterData> boosterDatas = new List<BoosterData>();

    public BoosterData Get(BoosterType type) => boosterDatas.Find(b => b.boosterType == type);
}
