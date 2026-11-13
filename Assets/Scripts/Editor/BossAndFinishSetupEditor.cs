using System.IO;
using System.Linq;
using HilmiVSRoti.Enemies;
using HilmiVSRoti.Level;
using HilmiVSRoti.Player;
using HilmiVSRoti.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HilmiVSRoti.EditorTools
{
    /// <summary>
    /// Tool Editor untuk Tahap 7:
    /// Menempatkan Final Boss (Zaki Raksasa) di arena boss, Garis Finish,
    /// serta UI HP Boss dan layar Menang/Kalah.
    /// </summary>
    public static class BossAndFinishSetupEditor
    {
        [MenuItem("Tools/Hilmi VS Roti/7. Setup Final Boss, Finish & Layar Menang-Kalah")]
        public static void SetupTahap7()
        {
            GameObject hilmi = GameObject.Find("Hilmi");
            PlayerHealth playerHealth = hilmi != null ? hilmi.GetComponent<PlayerHealth>() : null;

            BossShockwave wavePrefab = CreateShockwavePrefab();
            ZakiBoss boss = SpawnZakiBoss(wavePrefab);
            FinishLine finish = SpawnFinishLine(boss);
            SetupBossAndEndUI(playerHealth, boss, finish);
            EnsureEventSystem();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Hilmi VS Roti] Tahap 7 selesai: Final Boss Zaki Raksasa, Finish, dan layar Menang/Kalah siap dites!");
        }

        private static ZakiBoss SpawnZakiBoss(BossShockwave wavePrefab)
        {
            GameObject existing = GameObject.Find("ZakiBoss");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var go = new GameObject("ZakiBoss");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
                go.layer = enemyLayer;
            go.transform.position = new Vector3(135f, 0.82f, 0f);
            go.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/jek/jek_Idle_sheet.png")
                .OfType<Sprite>()
                .FirstOrDefault();
            sr.sortingOrder = 4;

            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Enemies/JekAnimator.controller");

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.0f, 1.16f);
            col.offset = new Vector2(0f, -0.24f);

            var boss = go.AddComponent<ZakiBoss>();
            var so = new SerializedObject(boss);
            so.FindProperty("maxHp").intValue = 5;
            so.FindProperty("shockwavePrefab").objectReferenceValue = wavePrefab;
            so.ApplyModifiedProperties();

            return boss;
        }

        private static FinishLine SpawnFinishLine(ZakiBoss boss)
        {
            GameObject existing = GameObject.Find("FinishLine");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var go = new GameObject("FinishLine");
            go.transform.position = new Vector3(148f, 0.5f, 0f);
            go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/Textures/CheckpointFlag.png");

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 2.5f);

            var finish = go.AddComponent<FinishLine>();
            var so = new SerializedObject(finish);
            so.FindProperty("finalBoss").objectReferenceValue = boss;
            so.ApplyModifiedProperties();

            return finish;
        }

        private static void SetupBossAndEndUI(PlayerHealth health, ZakiBoss boss, FinishLine finish)
        {
            GameObject canvasObj = GameObject.Find("GameCanvas");
            if (canvasObj == null)
            {
                Debug.LogWarning("[Hilmi VS Roti] GameCanvas tidak ditemukan. Jalankan Tahap 4 terlebih dahulu.");
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // HP Boss di atas tengah
            Text bossText = CreateOrReplaceText(canvasObj.transform, "BossHealthText",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(460f, 50f),
                font, 28, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.25f, 1f));

            // Teks notifikasi tengah atas
            Text noticeText = CreateOrReplaceText(canvasObj.transform, "NoticeText",
                new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(720f, 60f),
                font, 26, TextAnchor.MiddleCenter, Color.yellow);

            // Panel Menang / Kalah
            Transform oldPanel = canvasObj.transform.Find("EndPanel");
            if (oldPanel != null)
                Object.DestroyImmediate(oldPanel.gameObject);

            var panelObj = new GameObject("EndPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.72f);

            Text endTitle = CreateOrReplaceText(panelObj.transform, "EndTitleText",
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(760f, 160f),
                font, 46, TextAnchor.MiddleCenter, Color.white);

            // Tombol Restart
            var btnObj = new GameObject("RestartButton");
            btnObj.transform.SetParent(panelObj.transform, false);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.35f);
            btnRect.anchorMax = new Vector2(0.5f, 0.35f);
            btnRect.sizeDelta = new Vector2(240f, 64f);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.7f, 0.35f, 1f);
            var btn = btnObj.AddComponent<Button>();

            CreateOrReplaceText(btnObj.transform, "Label",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 50f),
                font, 28, TextAnchor.MiddleCenter, Color.white).text = "Main Lagi";

            var ui = canvasObj.GetComponent<BossAndEndUI>();
            if (ui == null)
                ui = canvasObj.AddComponent<BossAndEndUI>();

            UnityEventTools.AddPersistentListener(btn.onClick, ui.RestartGame);

            var so = new SerializedObject(ui);
            so.FindProperty("playerHealth").objectReferenceValue = health;
            so.FindProperty("finalBoss").objectReferenceValue = boss;
            so.FindProperty("finishLine").objectReferenceValue = finish;
            so.FindProperty("bossHealthText").objectReferenceValue = bossText;
            so.FindProperty("noticeText").objectReferenceValue = noticeText;
            so.FindProperty("endPanel").objectReferenceValue = panelObj;
            so.FindProperty("endTitleText").objectReferenceValue = endTitle;
            so.ApplyModifiedProperties();

            panelObj.SetActive(false);
        }

        private static Text CreateOrReplaceText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size, Font font, int fontSize, TextAnchor align, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var txt = obj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = align;
            txt.color = color;
            return txt;
        }

        private static BossShockwave CreateShockwavePrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            const string texPath = "Assets/Tiles/Textures/BossWave.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(texPath) == null)
            {
                var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                Color clear = new Color(0f, 0f, 0f, 0f);
                Color wave = new Color(1f, 0.35f, 0.15f, 1f);
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                        tex.SetPixel(x, y, (x + y > 6 && x < 14 && y < 14) ? wave : clear);
                tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

                var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spritePixelsPerUnit = 16f;
                    imp.filterMode = FilterMode.Point;
                    imp.SaveAndReimport();
                }
            }

            var temp = new GameObject("BossShockwave");
            var sr = temp.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            sr.sortingOrder = 2;

            var col = temp.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            temp.AddComponent<BossShockwave>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, "Assets/Prefabs/BossShockwave.prefab");
            Object.DestroyImmediate(temp);
            return prefab.GetComponent<BossShockwave>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }
}
