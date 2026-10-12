using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay.Tests
{
    /// <summary>
    /// Chạy thật trong Play mode, lấy mẫu từng frame (tween DOTween tự chạy, có delay, như lúc chơi):
    /// sóng ô quanh khối và squash khối khi đặt. Mở scene trống riêng rồi trả lại scene cũ khi xong.
    /// Mỗi frame cố định 1/60 s (Time.captureDeltaTime) để kết quả không phụ thuộc máy / editor đang chậm.
    /// Nằm trong Test Runner → EditMode (vì script game ở Assembly-CSharp, chưa tách asmdef).
    /// </summary>
    public class FeelPlayModeTests
    {
        private const string BlockPrefabPath = "Assets/_Project/Prefabs/Gameplay/PlacedBlock.prefab";
        private const string CellPrefabPath = "Assets/_Project/Prefabs/Cell.prefab";
        private const string BubblePrefabPath = "Assets/_Project/Prefabs/Gameplay/DragCountBubble.prefab";
        private const float Dt = 1f / 60f;
        private const float Eps = 1e-4f;

        private SceneSetup[] _sceneSetup;
        private readonly Dictionary<string, byte[]> _saveFiles = new Dictionary<string, byte[]>();

        private static string[] SaveFilePaths =>
            new[] { SaveSystem.FilePath, SaveSystem.FilePath + ".bak", SaveSystem.FilePath + ".tmp" };

        /// <summary>
        /// Vào Play mode thì TrustedClock tự đồng bộ giờ và ghi save, kể cả trong scene trống; UseInMemoryForTests không
        /// giữ được vì SaveSystem.ResetStatics chạy lại mỗi lần vào Play. Nên giữ nguyên byte save.json / .bak / .tmp của máy
        /// trước cả nhóm test, xong thì trả lại (file test tạo thêm thì xóa).
        /// </summary>
        [OneTimeSetUp]
        public void BackupSave()
        {
            _saveFiles.Clear();
            foreach (string path in SaveFilePaths)
            {
                _saveFiles[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
        }

        [OneTimeTearDown]
        public void RestoreSave()
        {
            // Dữ liệu save trong bộ nhớ là bản của Play mode (Domain Reload tắt): bỏ để lần dùng sau đọc lại file
            SaveSystem.UseInMemoryForTests(null);
            foreach (KeyValuePair<string, byte[]> file in _saveFiles)
            {
                if (file.Value != null)
                {
                    File.WriteAllBytes(file.Key, file.Value);
                }
                else if (File.Exists(file.Key))
                {
                    File.Delete(file.Key);
                }
            }
            _saveFiles.Clear();
        }

        /// <summary>Scene đang mở có thay đổi chưa lưu thì bỏ qua test (mở scene trống sẽ làm mất thay đổi đó).</summary>
        private void OpenEmptyScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    Assert.Ignore("Scene đang có thay đổi chưa lưu: lưu scene rồi chạy lại test.");
            }
            _sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureDeltaTime = 0f;
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            if (_sceneSetup != null && _sceneSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(_sceneSetup);
            _sceneSetup = null;
        }

        private static CellView NewCell(string name) => new GameObject(name).AddComponent<CellView>();

        // ---------- Sóng ô ----------

        [UnityTest]
        public IEnumerator Ripple_VongXaNayTreVaNheHon_RoiVeDungKichThuoc()
        {
            OpenEmptyScene();
            yield return new EnterPlayMode();
            Time.captureDeltaTime = Dt;

            const int radius = 2;
            const float amount = 0.06f, startDelay = 0.04f, stepDelay = 0.05f, duration = 0.22f;
            CellView near = NewCell("Ring1");
            CellView far = NewCell("Ring2");
            yield return null; // Awake / OnEnable xong

            float nearDelay = FeelCurves.RippleDelay(1, startDelay, stepDelay);
            float farDelay = FeelCurves.RippleDelay(2, startDelay, stepDelay);
            near.PlayBump(nearDelay, FeelCurves.RippleAmount(1, radius, amount), duration);
            far.PlayBump(farDelay, FeelCurves.RippleAmount(2, radius, amount), duration);

            float t0 = Time.time;
            float nearStart = -1f, farStart = -1f, nearPeak = 0f, farPeak = 0f;
            while (Time.time - t0 < farDelay + duration + 0.1f)
            {
                yield return null;
                float t = Time.time - t0;
                float n = near.transform.localScale.x - 1f;
                float f = far.transform.localScale.x - 1f;
                Assert.GreaterOrEqual(n, -Eps, "Ô không được co nhỏ hơn gốc");
                Assert.GreaterOrEqual(f, -Eps, "Ô không được co nhỏ hơn gốc");
                if (nearStart < 0f && n > Eps) nearStart = t;
                if (farStart < 0f && f > Eps) farStart = t;
                nearPeak = Mathf.Max(nearPeak, n);
                farPeak = Mathf.Max(farPeak, f);
            }

            Assert.GreaterOrEqual(nearStart, 0f, "Vòng 1 không nảy");
            Assert.GreaterOrEqual(farStart, 0f, "Vòng 2 không nảy");
            Assert.GreaterOrEqual(nearStart, nearDelay - Dt, "Vòng 1 nảy trước thời điểm chờ");
            Assert.GreaterOrEqual(farStart, farDelay - Dt, "Vòng 2 nảy trước thời điểm chờ");
            Assert.AreEqual(stepDelay, farStart - nearStart, 2f * Dt, "Khoảng trễ giữa 2 vòng");

            Assert.LessOrEqual(nearPeak, amount + Eps);
            Assert.Greater(nearPeak, amount * 0.9f, "Vòng 1 phải gần chạm biên độ");
            Assert.Less(farPeak, nearPeak, "Vòng 2 phải nhẹ hơn vòng 1");

            Assert.AreEqual(1f, near.transform.localScale.x, Eps, "Vòng 1 không về kích thước gốc");
            Assert.AreEqual(1f, far.transform.localScale.x, Eps, "Vòng 2 không về kích thước gốc");
        }

        [UnityTest]
        public IEnumerator Bump_TatGiuaChung_VeKichThuocGoc_VaKhongChayTiep()
        {
            OpenEmptyScene();
            yield return new EnterPlayMode();
            Time.captureDeltaTime = Dt;

            CellView cell = NewCell("Cell");
            yield return null;
            cell.PlayBump(0f, 0.1f, 0.5f);
            for (int i = 0; i < 10 && cell.transform.localScale.x <= 1f + Eps; i++) yield return null;
            Assert.Greater(cell.transform.localScale.x, 1f + Eps, "Ô chưa kịp nảy");

            cell.gameObject.SetActive(false); // vd. lưới bị xóa / trả về pool
            Assert.AreEqual(1f, cell.transform.localScale.x, Eps, "Tắt giữa chừng phải về kích thước gốc");

            cell.gameObject.SetActive(true);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(1f, cell.transform.localScale.x, Eps, "Tween cũ không được chạy tiếp");
        }

        // ---------- Khung chọn: thả tay ở vùng sai ----------

        [UnityTest]
        public IEnumerator SelectionBox_ThaTayVungSai_XamCoLaiRoiTat_KeoMoiThiHetXam()
        {
            OpenEmptyScene();
            yield return new EnterPlayMode();
            Time.captureDeltaTime = Dt;

            SelectionBoxView box = new GameObject("SelectionBox").AddComponent<SelectionBoxView>();
            SpriteRenderer sr = box.GetComponent<SpriteRenderer>();
            SerializedObject so = new SerializedObject(box);
            Color gray = so.FindProperty("rejectGray").colorValue;
            float duration = so.FindProperty("rejectDuration").floatValue;
            box.Show(Vector3.zero, new Vector2(2.4f, 1.2f));
            for (int i = 0; i < 20; i++) yield return null; // khung + alpha ổn định
            float startWidth = box.transform.localScale.x;

            box.HideRejected();
            Assert.IsTrue(box.gameObject.activeSelf, "Không được tắt ngay");
            float lastWidth = startWidth;
            bool sawGray = false;
            float t0 = Time.time;
            while (box.gameObject.activeSelf && Time.time - t0 < duration + 0.2f)
            {
                yield return null;
                if (!box.gameObject.activeSelf) break;
                float w = box.transform.localScale.x;
                Assert.LessOrEqual(w, lastWidth + Eps, "Khung chỉ được co lại");
                lastWidth = w;
                if (IsColor(sr.color, gray)) sawGray = true;
            }
            Assert.IsFalse(box.gameObject.activeSelf, "Khung phải tắt sau hiệu ứng");
            Assert.IsTrue(sawGray, "Khung phải chuyển xám");
            Assert.Less(lastWidth, startWidth, "Khung phải co lại");

            // Kéo lượt mới ngay sau đó: hiện lại màu thường, đúng cỡ
            box.Show(Vector3.zero, new Vector2(2.4f, 1.2f));
            yield return null;
            Assert.IsTrue(box.gameObject.activeSelf);
            Assert.AreEqual(startWidth, box.transform.localScale.x, 0.01f);
            Assert.IsFalse(IsColor(sr.color, gray), "Lượt kéo mới không được còn xám");
        }

        private static bool IsColor(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;

        // ---------- Ô số trong vùng kéo ----------

        [UnityTest]
        public IEnumerator OSo_TrongVungKeo_PhongTo_RaKhoiThiVe_LacXongVeChoCu()
        {
            OpenEmptyScene();
            yield return new EnterPlayMode();
            Time.captureDeltaTime = Dt;

            CellView prefab = AssetDatabase.LoadAssetAtPath<CellView>(CellPrefabPath);
            Assert.IsNotNull(prefab, "Không tìm thấy " + CellPrefabPath);
            CellView cell = Object.Instantiate(prefab);
            cell.Bind(new CellModel(Vector2Int.zero, new ClueData { value = 5 }));
            TMPro.TMP_Text text = new SerializedObject(cell).FindProperty("valueText").objectReferenceValue as TMPro.TMP_Text;
            Assert.IsNotNull(text);
            yield return null;
            Vector3 baseScale = text.transform.localScale;
            Vector3 basePos = text.transform.localPosition;

            cell.SetHighlighted(true);
            for (int i = 0; i < 20; i++) yield return null;
            Assert.Greater(text.transform.localScale.x, baseScale.x * 1.1f, "Số phải phóng to khi nằm trong vùng kéo");

            cell.SetHighlighted(false);
            for (int i = 0; i < 20; i++) yield return null;
            Assert.AreEqual(baseScale.x, text.transform.localScale.x, Eps, "Ra khỏi vùng phải về cỡ gốc");

            cell.PlayConflictShake();
            float maxOffset = 0f;
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                maxOffset = Mathf.Max(maxOffset, Mathf.Abs(text.transform.localPosition.x - basePos.x));
            }
            Assert.Greater(maxOffset, 0.01f, "Số phải lắc");
            Assert.AreEqual(basePos.x, text.transform.localPosition.x, Eps, "Lắc xong phải về chỗ cũ");
        }

        // ---------- Bong bóng đếm ô ----------

        [UnityTest]
        public IEnumerator Bubble_BamNgonTayMuot_KhongNhay()
        {
            OpenEmptyScene();
            yield return new EnterPlayMode();
            Time.captureDeltaTime = Dt;

            DragCountBubble prefab = AssetDatabase.LoadAssetAtPath<DragCountBubble>(BubblePrefabPath);
            Assert.IsNotNull(prefab, "Không tìm thấy " + BubblePrefabPath);
            DragCountBubble bubble = Object.Instantiate(prefab);
            bubble.gameObject.SetActive(false);
            Vector3 offset = new SerializedObject(bubble).FindProperty("offset").vector3Value;

            bubble.Show(Vector3.zero, 1);
            Assert.AreEqual(offset.x, bubble.transform.position.x, Eps, "Lần hiện đầu phải đứng ngay dưới ngón tay");

            // Ngón tay nhảy 2 đơn vị: bong bóng trượt dần tới, không nhảy ngay
            bubble.Show(new Vector3(2f, 0f, 0f), 2);
            yield return null;
            float x1 = bubble.transform.position.x - offset.x;
            Assert.Greater(x1, 0f);
            Assert.Less(x1, 1.5f, "Không được dính cứng vào ngón tay");
            for (int i = 0; i < 40; i++)
            {
                bubble.Show(new Vector3(2f, 0f, 0f), 2);
                yield return null;
            }
            Assert.AreEqual(2f, bubble.transform.position.x - offset.x, 0.01f, "Phải tới sát ngón tay");
        }

        // ---------- Gỡ khối ----------

        [UnityTest]
        public IEnumerator GoKhoi_NhayLenRoiRoiXuong_MoDan_GoiOnDone()
        {
            OpenEmptyScene();
            yield return new EnterPlayMode();
            Time.captureDeltaTime = Dt;

            PlacedBlockView view = SpawnBlock(out SpriteRenderer body);
            SerializedObject so = new SerializedObject(view);
            float hop = so.FindProperty("removeHopHeight").floatValue;
            float fall = so.FindProperty("removeFallDepth").floatValue;
            float duration = so.FindProperty("removeDuration").floatValue;
            for (int i = 0; i < 30; i++) yield return null; // squash lúc đặt xong
            float baseY = view.transform.position.y;

            bool done = false;
            view.PlayRemove(() => done = true);
            float maxUp = 0f, minDown = 0f;
            int firstUpFrame = -1, firstDownFrame = -1;
            int frames = Mathf.CeilToInt((duration + 0.2f) / Dt);
            for (int frame = 0; !done && frame < frames; frame++)
            {
                yield return null;
                if (done) break;
                float dy = view.transform.position.y - baseY;
                maxUp = Mathf.Max(maxUp, dy);
                minDown = Mathf.Min(minDown, dy);
                if (firstUpFrame < 0 && dy > Eps) firstUpFrame = frame;
                if (firstDownFrame < 0 && dy < -Eps) firstDownFrame = frame;
            }
            Assert.IsTrue(done, "onDone phải được gọi để trả khối về pool");
            Assert.Greater(maxUp, hop * 0.8f, "Phải nhảy bật lên");
            Assert.Less(minDown, -fall * 0.5f, "Phải rơi xuống thấp hơn chỗ cũ");
            Assert.Less(firstUpFrame, firstDownFrame, "Lên trước rồi mới rơi");
            Assert.Less(body.color.a, 0.2f, "Phải mờ dần khi biến mất");
        }

        private static PlacedBlockView SpawnBlock(out SpriteRenderer body)
        {
            PlacedBlockView prefab = AssetDatabase.LoadAssetAtPath<PlacedBlockView>(BlockPrefabPath);
            Assert.IsNotNull(prefab, "Không tìm thấy " + BlockPrefabPath);
            PlacedBlockView view = Object.Instantiate(prefab);
            body = new SerializedObject(view).FindProperty("body").objectReferenceValue as SpriteRenderer;

            RectInt rect = new RectInt(0, 0, 3, 2);
            GridGeometry geometry = new GridGeometry(4, 4, GridSpawner.CellSpacing);
            CellModel clue = new CellModel(Vector2Int.zero, new ClueData { value = 6 });
            List<CellModel> cells = new List<CellModel>();
            for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
                cells.Add(x == 0 && y == 0 ? clue : new CellModel(new Vector2Int(x, y)));
            view.Show(new BlockModel(rect, cells, clue, default), geometry);
            return view;
        }

        // ---------- Squash khối ----------

        [UnityTest]
        public IEnumerator Squash_KhoiDatXuong_DetRoiDai_DayDungYen_VeNguyenTrang()
        {
            OpenEmptyScene();
            yield return new EnterPlayMode();
            Time.captureDeltaTime = Dt;

            PlacedBlockView prefab = AssetDatabase.LoadAssetAtPath<PlacedBlockView>(BlockPrefabPath);
            Assert.IsNotNull(prefab, "Không tìm thấy " + BlockPrefabPath);
            PlacedBlockView view = Object.Instantiate(prefab);
            SpriteRenderer body = new SerializedObject(view).FindProperty("body").objectReferenceValue as SpriteRenderer;
            float amount = new SerializedObject(view).FindProperty("squashAmount").floatValue;
            float duration = new SerializedObject(view).FindProperty("squashDuration").floatValue;
            Assert.IsNotNull(body, "Prefab chưa gán body");
            yield return null;

            // Khối 3 × 2, ô số 6 ở góc dưới trái
            RectInt rect = new RectInt(0, 0, 3, 2);
            GridGeometry geometry = new GridGeometry(4, 4, GridSpawner.CellSpacing);
            CellModel clue = new CellModel(Vector2Int.zero, new ClueData { value = 6 });
            List<CellModel> cells = new List<CellModel>();
            for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
                cells.Add(x == 0 && y == 0 ? clue : new CellModel(new Vector2Int(x, y)));
            view.Show(new BlockModel(rect, cells, clue, default), geometry);

            Vector3 basePosition = view.transform.position;
            float restBottom = float.NaN;
            float firstX = float.NaN, maxSquash = 0f, maxStretch = 0f, maxBottomDrift = 0f;
            float t0 = Time.time;
            while (Time.time - t0 < duration + 0.1f)
            {
                Vector3 s = view.transform.localScale;
                float bottom = body.bounds.min.y;
                if (float.IsNaN(restBottom)) restBottom = bottom; // mẫu đầu: tween chưa chạy frame nào, khối đang scale 1
                maxBottomDrift = Mathf.Max(maxBottomDrift, Mathf.Abs(bottom - restBottom));

                float d = s.x - 1f;
                if (float.IsNaN(firstX) && Mathf.Abs(d) > Eps) firstX = s.x;
                maxSquash = Mathf.Max(maxSquash, d);
                maxStretch = Mathf.Max(maxStretch, -d);
                yield return null;
            }

            Assert.IsFalse(float.IsNaN(firstX), "Khối không squash");
            Assert.Greater(firstX, 1f, "Nhịp đầu phải dẹt (ngang to ra)");
            Assert.LessOrEqual(maxSquash, amount + Eps);
            Assert.Greater(maxSquash, amount * 0.5f, "Dẹt quá ít");
            Assert.Greater(maxStretch, Eps, "Không có nhịp dài ra");
            Assert.Less(maxBottomDrift, 0.005f, "Đáy khối bị xê dịch");

            Assert.AreEqual(1f, view.transform.localScale.x, Eps);
            Assert.AreEqual(1f, view.transform.localScale.y, Eps);
            Assert.AreEqual(basePosition.y, view.transform.position.y, Eps, "Khối không về đúng vị trí");
        }
    }
}
