using HilmiVSRoti.Level;
using HilmiVSRoti.Player;
using HilmiVSRoti.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HilmiVSRoti.EditorTools
{
    /// <summary>
    /// Tool Editor untuk Tahap 4:
    /// Menambahkan PlayerHealth ke Hilmi, membuat beberapa Checkpoint di stage,
    /// dan membuat Canvas UI sederhana untuk menampilkan nyawa serta pesan Game Over.
    /// </summary>
    public static class HealthAndCheckpointSetupEditor
    {
        [MenuItem("Tools/Hilmi VS Roti/4. Setup Nyawa, Checkpoint & UI")]
        public static void SetupTahap4()
        {
            GameObject hilmi = GameObject.Find("Hilmi");
            if (hilmi == null)
            {
                Debug.LogError("[Hilmi VS Roti] GameObject 'Hilmi' tidak ditemukan! Jalankan Tahap 1-3 terlebih dahulu.");
                return;
            }

            var health = hilmi.GetComponent<PlayerHealth>();
            if (health == null)
                health = hilmi.AddComponent<PlayerHealth>();

            CreateCheckpoints();
            CreateHealthUI(health);

            EditorUtility.SetDirty(hilmi);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Hilmi VS Roti] Tahap 4 selesai: Sistem nyawa, checkpoint, dan UI siap dites!");
        }

        private static void CreateCheckpoints()
        {
            GameObject existing = GameObject.Find("Checkpoints");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var root = new GameObject("Checkpoints");
            Sprite flagSprite = CreateFlagSprite();

            float[] checkpointX = { 27f, 60f, 92f, 122f };
            for (int i = 0; i < checkpointX.Length; i++)
            {
                var cp = new GameObject($"Checkpoint_{i + 1}");
                cp.transform.SetParent(root.transform);
                cp.transform.position = new Vector3(checkpointX[i], -0.25f, 0f);
                cp.transform.localScale = Vector3.one;

                var sr = cp.AddComponent<SpriteRenderer>();
                sr.sprite = flagSprite;
                sr.sortingOrder = 0;

                var col = cp.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1f, 2f);
                cp.AddComponent<Checkpoint>();
            }
        }

        private static void CreateHealthUI(PlayerHealth health)
        {
            GameObject existing = GameObject.Find("GameCanvas");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var canvasObj = new GameObject("GameCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasObj.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Teks Nyawa di pojok kiri atas
            var healthTextObj = new GameObject("HealthText");
            healthTextObj.transform.SetParent(canvasObj.transform, false);
            var healthRect = healthTextObj.AddComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0f, 1f);
            healthRect.anchorMax = new Vector2(0f, 1f);
            healthRect.pivot = new Vector2(0f, 1f);
            healthRect.anchoredPosition = new Vector2(24f, -20f);
            healthRect.sizeDelta = new Vector2(360f, 60f);

            var healthText = healthTextObj.AddComponent<Text>();
            healthText.font = font;
            healthText.fontSize = 32;
            healthText.fontStyle = FontStyle.Bold;
            healthText.color = Color.white;
            healthText.text = "Nyawa: ♥ ♥ ♥";

            // Teks Game Over di tengah layar
            var statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(canvasObj.transform, false);
            var statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0.5f);
            statusRect.anchorMax = new Vector2(0.5f, 0.5f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.anchoredPosition = Vector2.zero;
            statusRect.sizeDelta = new Vector2(600f, 160f);

            var statusText = statusObj.AddComponent<Text>();
            statusText.font = font;
            statusText.fontSize = 48;
            statusText.fontStyle = FontStyle.Bold;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(1f, 0.3f, 0.3f, 1f);
            statusObj.SetActive(false);

            var healthUI = canvasObj.AddComponent<HealthUI>();
            var so = new SerializedObject(healthUI);
            so.FindProperty("playerHealth").objectReferenceValue = health;
            so.FindProperty("healthText").objectReferenceValue = healthText;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.ApplyModifiedProperties();
        }

        private static Sprite CreateFlagSprite()
        {
            const string flagPath = "Assets/Tiles/Textures/CheckpointFlag.png";
            if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
                AssetDatabase.CreateFolder("Assets", "Tiles");
            if (!AssetDatabase.IsValidFolder("Assets/Tiles/Textures"))
                AssetDatabase.CreateFolder("Assets/Tiles", "Textures");

            if (AssetDatabase.LoadAssetAtPath<Sprite>(flagPath) == null)
            {
                var tex = new Texture2D(16, 32, TextureFormat.RGBA32, false);
                Color clear = new Color(0f, 0f, 0f, 0f);
                Color pole = new Color(0.85f, 0.85f, 0.85f, 1f);
                Color flag = Color.white; // Diwarnai oleh Checkpoint.cs (abu-abu -> hijau)

                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        if (x >= 2 && x <= 3)
                            tex.SetPixel(x, y, pole);
                        else if (y >= 18 && y <= 30 && x > 3 && x <= 14)
                            tex.SetPixel(x, y, flag);
                        else
                            tex.SetPixel(x, y, clear);
                    }
                }
                tex.Apply();
                System.IO.File.WriteAllBytes(flagPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(flagPath, ImportAssetOptions.ForceUpdate);

                var importer = AssetImporter.GetAtPath(flagPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 16f;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(flagPath);
        }
    }
}

