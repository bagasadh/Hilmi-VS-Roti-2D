using System.IO;
using HilmiVSRoti.Items;
using HilmiVSRoti.Player;
using HilmiVSRoti.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HilmiVSRoti.EditorTools
{
    /// <summary>
    /// Tool Editor untuk Tahap 6:
    /// Membuat sprite 5 jenis roti (termasuk Roti Jamuran berbintik hijau),
    /// memasang PlayerBreadEffects pada Hilmi, UI indikator durasi, dan menaruh roti di stage.
    /// </summary>
    public static class BreadSetupEditor
    {
        private const string TexturesFolder = "Assets/Tiles/Textures";

        [MenuItem("Tools/Hilmi VS Roti/6. Setup Sistem Roti & UI Indikator")]
        public static void SetupTahap6()
        {
            GameObject hilmi = GameObject.Find("Hilmi");
            if (hilmi == null)
            {
                Debug.LogError("[Hilmi VS Roti] GameObject 'Hilmi' tidak ditemukan!");
                return;
            }

            var breadEffects = hilmi.GetComponent<PlayerBreadEffects>();
            if (breadEffects == null)
                breadEffects = hilmi.AddComponent<PlayerBreadEffects>();

            Sprite tawarSprite = CreateBreadSprite("RotiTawar", new Color(0.95f, 0.88f, 0.72f), new Color(0.75f, 0.50f, 0.22f), moldy: false);
            Sprite kejuSprite = CreateBreadSprite("RotiKeju", new Color(1.00f, 0.85f, 0.25f), new Color(0.85f, 0.55f, 0.12f), moldy: false);
            Sprite cokelatSprite = CreateBreadSprite("RotiCokelat", new Color(0.45f, 0.24f, 0.12f), new Color(0.28f, 0.14f, 0.06f), moldy: false);
            Sprite sobekSprite = CreateBreadSprite("RotiSobek", new Color(0.88f, 0.65f, 0.35f), new Color(0.55f, 0.32f, 0.14f), moldy: false);
            Sprite jamuranSprite = CreateBreadSprite("RotiJamuran", new Color(0.92f, 0.85f, 0.68f), new Color(0.70f, 0.48f, 0.22f), moldy: true);

            SetupBreadUI(breadEffects);

            GameObject existing = GameObject.Find("Breads");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var root = new GameObject("Breads");

            // 0:00 - 0:20 Area Pembuka: roti pertama aman (di atas bata y=1 -> permukaan 2.0)
            SpawnBread("RotiTawar_1", root.transform, new Vector3(6f, 2.75f, 0f), BreadType.RotiTawar, 6.5f, tawarSprite);

            // 0:20 - 0:50 Area jek + jurang: beberapa jenis roti
            SpawnBread("RotiKeju_1", root.transform, new Vector3(35.5f, 2.75f, 0f), BreadType.RotiKeju, 6.5f, kejuSprite);
            SpawnBread("RotiCokelat_1", root.transform, new Vector3(50f, -0.25f, 0f), BreadType.RotiCokelat, 6.5f, cokelatSprite);

            // 0:50 - 1:20 Area labutit & KenyGK: roti sobek & roti jamuran pertama
            SpawnBread("RotiSobek_1", root.transform, new Vector3(64.5f, 2.75f, 0f), BreadType.RotiSobek, 8f, sobekSprite);
            SpawnBread("RotiJamuran_1", root.transform, new Vector3(80f, -0.25f, 0f), BreadType.RotiJamuran, 5.5f, jamuranSprite);

            // 1:20 - 1:40 Area ujian (jauh dari duri di x=95..96)
            SpawnBread("RotiTawar_2", root.transform, new Vector3(92.5f, -0.25f, 0f), BreadType.RotiTawar, 6.5f, tawarSprite);
            SpawnBread("RotiJamuran_2", root.transform, new Vector3(114.5f, -0.25f, 0f), BreadType.RotiJamuran, 5.5f, jamuranSprite);

            // 1:40 - 2:00 Arena boss
            SpawnBread("RotiSobek_Boss", root.transform, new Vector3(128.5f, 2.75f, 0f), BreadType.RotiSobek, 8f, sobekSprite);
            SpawnBread("RotiKeju_Boss", root.transform, new Vector3(138.5f, 2.75f, 0f), BreadType.RotiKeju, 6.5f, kejuSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Hilmi VS Roti] Tahap 6 selesai: Semua 5 jenis roti & UI indikator durasi siap dites!");
        }

        private static void SpawnBread(string name, Transform parent, Vector3 pos, BreadType type, float duration, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            var pickup = go.AddComponent<BreadPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("breadType").enumValueIndex = (int)type;
            so.FindProperty("duration").floatValue = duration;
            so.ApplyModifiedProperties();
        }

        private static void SetupBreadUI(PlayerBreadEffects breadEffects)
        {
            GameObject canvasObj = GameObject.Find("GameCanvas");
            if (canvasObj == null)
            {
                Debug.LogWarning("[Hilmi VS Roti] GameCanvas tidak ditemukan. Jalankan Tahap 4 dulu untuk UI lengkap.");
                return;
            }

            Transform existingText = canvasObj.transform.Find("BreadStatusText");
            if (existingText != null)
                Object.DestroyImmediate(existingText.gameObject);

            var textObj = new GameObject("BreadStatusText");
            textObj.transform.SetParent(canvasObj.transform, false);

            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -72f);
            rect.sizeDelta = new Vector2(520f, 160f);

            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.95f, 0.55f, 1f);
            text.text = "";

            var breadUI = canvasObj.GetComponent<BreadUI>();
            if (breadUI == null)
                breadUI = canvasObj.AddComponent<BreadUI>();

            var so = new SerializedObject(breadUI);
            so.FindProperty("breadEffects").objectReferenceValue = breadEffects;
            so.FindProperty("breadStatusText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        private static Sprite CreateBreadSprite(string name, Color inner, Color crust, bool moldy)
        {
            string path = $"{TexturesFolder}/{name}.png";
            if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
                AssetDatabase.CreateFolder("Assets", "Tiles");
            if (!AssetDatabase.IsValidFolder(TexturesFolder))
                AssetDatabase.CreateFolder("Assets/Tiles", "Textures");

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color mold = new Color(0.20f, 0.72f, 0.25f, 1f);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool inBody = (x >= 2 && x <= 13 && y >= 3 && y <= 10) || (x >= 3 && x <= 12 && y >= 11 && y <= 13);
                    if (!inBody)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    bool isBorder = x == 2 || x == 13 || y == 3 || y == 13 || (y > 10 && (x == 3 || x == 12));
                    tex.SetPixel(x, y, isBorder ? crust : inner);
                }
            }

            if (moldy)
            {
                // Bintik hijau kecil sebagai jebakan roti jamuran
                tex.SetPixel(5, 7, mold);
                tex.SetPixel(6, 7, mold);
                tex.SetPixel(10, 9, mold);
                tex.SetPixel(9, 5, mold);
                tex.SetPixel(7, 11, mold);
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}

