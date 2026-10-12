using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode: công thức game feel trong FeelCurves (squash khối, ô nảy, sóng ô, cao độ tick kéo).
/// Chạy: Window → General → Test Runner → EditMode.
/// </summary>
public class FeelCurvesTests
{
    private const float Amount = 0.12f;
    private const float Wobbles = 2.5f;
    private const int Samples = 400;
    private const float Eps = 1e-4f;

    // ---------- Squash ----------

    [Test]
    public void Squash_BatDauVaKetThucDungYen()
    {
        Assert.That(FeelCurves.Squash(0f, Amount, Wobbles), Is.EqualTo(Vector2.one).Using(Vector2EqualityComparer(Eps)));
        Assert.That(FeelCurves.Squash(1f, Amount, Wobbles), Is.EqualTo(Vector2.one).Using(Vector2EqualityComparer(Eps)));
    }

    [Test]
    public void Squash_DetTruocRoiMoiDai()
    {
        // Mẫu đầu tiên khác 1 phải là dẹt (X > 1, Y < 1); sau đó phải có lúc dài (X < 1, Y > 1)
        int firstMove = -1;
        bool stretched = false;
        for (int i = 1; i < Samples; i++)
        {
            Vector2 s = FeelCurves.Squash(i / (float)Samples, Amount, Wobbles);
            if (firstMove < 0 && Mathf.Abs(s.x - 1f) > Eps)
            {
                firstMove = i;
                Assert.Greater(s.x, 1f, "Nhịp đầu phải dẹt: ngang to ra");
                Assert.Less(s.y, 1f, "Nhịp đầu phải dẹt: dọc thấp xuống");
            }
            if (firstMove >= 0 && s.x < 1f - Eps && s.y > 1f + Eps) stretched = true;
        }
        Assert.GreaterOrEqual(firstMove, 0, "Không thấy khối dẹt");
        Assert.IsTrue(stretched, "Không có nhịp dài ra sau khi dẹt");
    }

    [Test]
    public void Squash_KhongVuotBienDo_VaGiuDienTichGanDung()
    {
        for (int i = 0; i <= Samples; i++)
        {
            Vector2 s = FeelCurves.Squash(i / (float)Samples, Amount, Wobbles);
            Assert.LessOrEqual(Mathf.Abs(s.x - 1f), Amount + Eps);
            Assert.AreEqual(2f, s.x + s.y, Eps, "Ngang to bao nhiêu thì dọc co bấy nhiêu");
        }
    }

    [Test]
    public void Squash_BienDoTatDan()
    {
        // Đỉnh |d| của từng nửa nhịp phải nhỏ dần
        float lastPeak = float.MaxValue;
        float peak = 0f;
        int sign = 0;
        for (int i = 1; i < Samples; i++)
        {
            float d = FeelCurves.Squash(i / (float)Samples, Amount, Wobbles).x - 1f;
            int sg = d > Eps ? 1 : d < -Eps ? -1 : 0;
            if (sg == 0) continue;
            if (sign != 0 && sg != sign)
            {
                Assert.Less(peak, lastPeak, "Nhịp sau phải nhẹ hơn nhịp trước");
                lastPeak = peak;
                peak = 0f;
            }
            sign = sg;
            peak = Mathf.Max(peak, Mathf.Abs(d));
        }
        Assert.Less(peak, lastPeak);
    }

    [Test]
    public void Squash_PNgoaiKhoangBiKep()
    {
        Assert.That(FeelCurves.Squash(-1f, Amount, Wobbles), Is.EqualTo(Vector2.one).Using(Vector2EqualityComparer(Eps)));
        Assert.That(FeelCurves.Squash(2f, Amount, Wobbles), Is.EqualTo(Vector2.one).Using(Vector2EqualityComparer(Eps)));
    }

    [TestCase(0.9f, 3.4f)]
    [TestCase(1.1f, 3.4f)]
    [TestCase(0.75f, 1.0f)]
    public void BottomAnchorOffset_DayDungYen(float scaleY, float height)
    {
        const float centerY = 5f;
        float bottomAtRest = centerY - height / 2f;
        float bottom = centerY + FeelCurves.BottomAnchorOffset(scaleY, height) - height * scaleY / 2f;
        Assert.AreEqual(bottomAtRest, bottom, Eps);
    }

    // ---------- Ô nảy ----------

    [Test]
    public void Bump_VeLaiKichThuocGoc_VaDinhOGiua()
    {
        Assert.AreEqual(1f, FeelCurves.Bump(0f, 0.06f), Eps);
        Assert.AreEqual(1f, FeelCurves.Bump(1f, 0.06f), Eps);
        Assert.AreEqual(1.06f, FeelCurves.Bump(0.5f, 0.06f), Eps);
        for (int i = 0; i <= Samples; i++)
            Assert.GreaterOrEqual(FeelCurves.Bump(i / (float)Samples, 0.06f), 1f - Eps, "Ô không được co nhỏ hơn gốc");
    }

    // ---------- Sóng ô ----------

    [TestCase(1, 1, 0)] // trong vùng
    [TestCase(3, 1, 0)]
    [TestCase(0, 1, 1)] // sát cạnh trái
    [TestCase(4, 2, 1)] // góc chéo
    [TestCase(2, 3, 2)] // vòng 2 phía trên
    [TestCase(6, 1, 3)]
    public void RingDistance_TinhCaCheo(int x, int y, int expected)
    {
        RectInt rect = new RectInt(1, 0, 3, 2); // x 1..3, y 0..1
        Assert.AreEqual(expected, FeelCurves.RingDistance(rect, new Vector2Int(x, y)));
    }

    [Test]
    public void RippleAmount_GanManhHon_NgoaiBanKinhBang0()
    {
        const int radius = 2;
        Assert.AreEqual(0f, FeelCurves.RippleAmount(0, radius, 0.06f), "Ô của chính khối không nảy");
        Assert.AreEqual(0.06f, FeelCurves.RippleAmount(1, radius, 0.06f), Eps);
        Assert.Less(FeelCurves.RippleAmount(2, radius, 0.06f), FeelCurves.RippleAmount(1, radius, 0.06f));
        Assert.Greater(FeelCurves.RippleAmount(2, radius, 0.06f), 0f);
        Assert.AreEqual(0f, FeelCurves.RippleAmount(3, radius, 0.06f));
        Assert.AreEqual(0f, FeelCurves.RippleAmount(1, 0, 0.06f), "Bán kính 0 = tắt sóng");
    }

    [Test]
    public void RippleDelay_VongXaTreHon()
    {
        Assert.AreEqual(0.04f, FeelCurves.RippleDelay(1, 0.04f, 0.05f), Eps);
        Assert.AreEqual(0.09f, FeelCurves.RippleDelay(2, 0.04f, 0.05f), Eps);
        Assert.AreEqual(0.04f, FeelCurves.RippleDelay(0, 0.04f, 0.05f), Eps, "Vòng 0 không trễ âm");
    }

    // ---------- Cao độ tick kéo ----------

    private static readonly int[] Pentatonic = { 0, 2, 4, 7, 9, 12, 14, 16 };

    [Test]
    public void DragPitch_O1LaCaoDoGoc_Va1QuangTamGapDoi()
    {
        Assert.AreEqual(0.9f, FeelCurves.DragPitch(1, 0.9f, Pentatonic), Eps);
        Assert.AreEqual(1.8f, FeelCurves.DragPitch(6, 0.9f, Pentatonic), Eps); // nửa cung 12 = 1 quãng tám
    }

    [Test]
    public void DragPitch_TangDan_VaGiuNotCuoiKhiQuaMang()
    {
        float last = 0f;
        for (int count = 1; count <= Pentatonic.Length; count++)
        {
            float pitch = FeelCurves.DragPitch(count, 0.9f, Pentatonic);
            Assert.Greater(pitch, last, $"Ô thứ {count} phải cao hơn ô trước");
            last = pitch;
        }
        Assert.AreEqual(last, FeelCurves.DragPitch(Pentatonic.Length + 5, 0.9f, Pentatonic), Eps);
        Assert.AreEqual(0.9f, FeelCurves.DragPitch(0, 0.9f, Pentatonic), Eps, "Số ô 0 không lỗi chỉ số mảng");
    }

    [Test]
    public void DragPitch_MangRong_TraVeCaoDoGoc()
    {
        Assert.AreEqual(0.9f, FeelCurves.DragPitch(4, 0.9f, null), Eps);
        Assert.AreEqual(0.9f, FeelCurves.DragPitch(4, 0.9f, new int[0]), Eps);
    }

    // ---------- Ô "dính" khi kéo ----------

    private static readonly GridGeometry Grid5 = new GridGeometry(5, 5, 1.2f);

    [Test]
    public void StickyCell_QuaRanhGioiChutIt_VanGiuOCu()
    {
        Vector2Int current = new Vector2Int(2, 2);
        Vector3 center = Grid5.CellToWorld(current);
        // Ranh giới với ô (3,2) ở +0.6; dính 0.15 ô -> giữ tới +0.78
        Assert.AreEqual(current, FeelCurves.StickyCell(Grid5, center + new Vector3(0.7f, 0f, 0f), current, 0.15f));
        Assert.AreEqual(new Vector2Int(3, 2), FeelCurves.StickyCell(Grid5, center + new Vector3(0.85f, 0f, 0f), current, 0.15f));
    }

    [Test]
    public void StickyCell_TatDinh_NhuWorldToCell()
    {
        Vector2Int current = new Vector2Int(2, 2);
        Vector3 world = Grid5.CellToWorld(current) + new Vector3(0.7f, 0f, 0f);
        Assert.AreEqual(Grid5.WorldToCell(world), FeelCurves.StickyCell(Grid5, world, current, 0f));
    }

    [Test]
    public void StickyCell_OCuNgoaiLuoi_KhongGiu()
    {
        Vector3 world = Grid5.CellToWorld(new Vector2Int(0, 0));
        Assert.AreEqual(new Vector2Int(0, 0), FeelCurves.StickyCell(Grid5, world, new Vector2Int(-1, 0), 0.4f));
    }

    // ---------- Combo ----------

    [Test]
    public void ComboCounter_DatNhanhThiTang_ChamThiVe1_ResetVe0()
    {
        ComboCounter combo = new ComboCounter(2.5f);
        Assert.AreEqual(1, combo.Register(10f));
        Assert.AreEqual(2, combo.Register(11f));
        Assert.AreEqual(3, combo.Register(13.5f), "Đúng bằng cửa sổ vẫn tính");
        Assert.AreEqual(1, combo.Register(16.1f), "Quá cửa sổ thì về 1");
        combo.Reset();
        Assert.AreEqual(0, combo.Count);
        Assert.AreEqual(1, combo.Register(16.2f), "Sau Reset lần đặt kế là 1 dù rất gần");
    }

    [Test]
    public void ComboPitch_TangDanVaCoTran()
    {
        Assert.AreEqual(1f, FeelCurves.ComboPitch(1, 0.06f, 1.3f), Eps);
        Assert.AreEqual(1.06f, FeelCurves.ComboPitch(2, 0.06f, 1.3f), Eps);
        Assert.AreEqual(1.3f, FeelCurves.ComboPitch(50, 0.06f, 1.3f), Eps);
        Assert.AreEqual(1f, FeelCurves.ComboPitch(0, 0.06f, 1.3f), Eps, "Combo 0 không thấp hơn gốc");
    }

    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(9, 3)]
    public void PraiseTier_TheoCombo(int combo, int expected)
    {
        Assert.AreEqual(expected, FeelCurves.PraiseTier(combo, 4));
    }

    private static System.Collections.Generic.IEqualityComparer<Vector2> Vector2EqualityComparer(float eps) =>
        new Vector2Comparer(eps);

    private class Vector2Comparer : System.Collections.Generic.IEqualityComparer<Vector2>
    {
        private readonly float _eps;
        public Vector2Comparer(float eps) { _eps = eps; }
        public bool Equals(Vector2 a, Vector2 b) => Mathf.Abs(a.x - b.x) <= _eps && Mathf.Abs(a.y - b.y) <= _eps;
        public int GetHashCode(Vector2 v) => 0;
    }
}
