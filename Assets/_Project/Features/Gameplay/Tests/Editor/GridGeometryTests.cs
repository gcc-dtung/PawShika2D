using NUnit.Framework;
using UnityEngine;

namespace PawBlock.Features.Gameplay.Tests
{
    /// <summary>EditMode: GridGeometry (ô "dính" dưới ngón tay khi kéo).</summary>
    public class GridGeometryTests
    {
        // ---------- Ô "dính" khi kéo ----------

        private static readonly GridGeometry Grid5 = new GridGeometry(5, 5, 1.2f);

        [Test]
        public void StickyCell_QuaRanhGioiChutIt_VanGiuOCu()
        {
            Vector2Int current = new Vector2Int(2, 2);
            Vector3 center = Grid5.CellToWorld(current);
            // Ranh giới với ô (3,2) ở +0.6; dính 0.15 ô -> giữ tới +0.78
            Assert.AreEqual(current, Grid5.StickyCell(center + new Vector3(0.7f, 0f, 0f), current, 0.15f));
            Assert.AreEqual(new Vector2Int(3, 2), Grid5.StickyCell(center + new Vector3(0.85f, 0f, 0f), current, 0.15f));
        }

        [Test]
        public void StickyCell_TatDinh_NhuWorldToCell()
        {
            Vector2Int current = new Vector2Int(2, 2);
            Vector3 world = Grid5.CellToWorld(current) + new Vector3(0.7f, 0f, 0f);
            Assert.AreEqual(Grid5.WorldToCell(world), Grid5.StickyCell(world, current, 0f));
        }

        [Test]
        public void StickyCell_OCuNgoaiLuoi_KhongGiu()
        {
            Vector3 world = Grid5.CellToWorld(new Vector2Int(0, 0));
            Assert.AreEqual(new Vector2Int(0, 0), Grid5.StickyCell(world, new Vector2Int(-1, 0), 0.4f));
        }
    }
}
