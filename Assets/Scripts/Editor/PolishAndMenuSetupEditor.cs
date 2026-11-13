using System.IO;
using HilmiVSRoti.Audio;
using HilmiVSRoti.Enemies;
using HilmiVSRoti.Level;
using HilmiVSRoti.Player;
using HilmiVSRoti.UI;
using HilmiVSRoti.VFX;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HilmiVSRoti.EditorTools
{
    /// <summary>
    /// Tool Editor untuk Tahap 8 (Versi Poles Visual SPUM-style):
    /// - Memperbarui tekstur tile (rumput, tanah, bata 2.5D, duri) ke detail 32x32
    /// - Menambahkan latar awan & bukit pixel-art + GameVfxManager (partikel)
    /// - Memoles HUD (ikon hati pixel, Boss HP Bar berbingkai), Main Menu, & End Panel bergaya RPG pixel-art
    /// </summary>
    public static class PolishAndMenuSetupEditor
    {
        private const string UiTexFolder = "Assets/Tiles/Textures/UI";

        [MenuItem("Tools/Hilmi VS Roti/8. Setup Suara SFX, Main Menu & Poles UI")]
        public static void SetupTahap8()
        {
            EnsureUiTextureFolder();
            LevelSetupEditor.RefreshTileTextures();
            EnemySetupEditor.RefreshCrateDisguiseSprite();

            SetupAudioManager();
            SetupVfxManager();
            SetupBackgroundDecor();
            ReplaceBrickWallsWithCameraBounds();
            StyleDynamicObstacles();
            ApplyPixelOutlineMaterials();
            EnsureEventSystem();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[Hilmi VS Roti] GameCanvas tidak ditemukan! Jalankan Tahap 4 terlebih dahulu.");
                return;
            }

            canvas.pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            Sprite panelFrame = CreateSlicedUiSprite("UiFrame", 24, 6,
                new Color(0.08f, 0.06f, 0.12f, 1f),
                new Color(0.90f, 0.70f, 0.26f, 1f),
                new Color(0.15f, 0.16f, 0.25f, 0.97f));

            Sprite hudPill = CreateSlicedUiSprite("UiHudPill", 16, 4,
                new Color(0.07f, 0.06f, 0.10f, 0.92f),
                new Color(0.76f, 0.58f, 0.24f, 0.95f),
                new Color(0.11f, 0.12f, 0.19f, 0.86f));

            Sprite btnGreen = CreateButtonSprite("UiButtonGreen",
                new Color(0.28f, 0.78f, 0.36f, 1f), new Color(0.18f, 0.56f, 0.26f, 1f), new Color(0.09f, 0.30f, 0.13f, 1f));
            Sprite btnBlue = CreateButtonSprite("UiButtonBlue",
                new Color(0.32f, 0.50f, 0.72f, 1f), new Color(0.22f, 0.36f, 0.54f, 1f), new Color(0.11f, 0.18f, 0.28f, 1f));
            Sprite btnRed = CreateButtonSprite("UiButtonRed",
                new Color(0.84f, 0.30f, 0.30f, 1f), new Color(0.64f, 0.20f, 0.20f, 1f), new Color(0.36f, 0.10f, 0.10f, 1f));

            Sprite heartFull = CreateHeartSprite("HeartFull", filled: true);
            Sprite heartEmpty = CreateHeartSprite("HeartEmpty", filled: false);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetupHealthHud(canvas.gameObject, font, hudPill, heartFull, heartEmpty);
            EnsureBreadUI(canvas.gameObject, font, hudPill);
            EnsureBossAndEndUI(canvas.gameObject, font, panelFrame, hudPill, btnGreen);
            PolishMenuHint(canvas.transform, font, hudPill);
            SetupMainMenu(canvas.transform, font, panelFrame, hudPill, btnGreen, btnBlue, btnRed);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Hilmi VS Roti] Tahap 8 selesai: Tile 32x32, VFX partikel, background, suara SFX, & UI pixel-art siap dites!");
        }

        private static void EnsureUiTextureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
                AssetDatabase.CreateFolder("Assets", "Tiles");
            if (!AssetDatabase.IsValidFolder("Assets/Tiles/Textures"))
                AssetDatabase.CreateFolder("Assets/Tiles", "Textures");
            if (!AssetDatabase.IsValidFolder(UiTexFolder))
                AssetDatabase.CreateFolder("Assets/Tiles/Textures", "UI");
        }

        private static void SetupAudioManager()
        {
            GameObject existing = GameObject.Find("GameAudioManager");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var go = new GameObject("GameAudioManager");
            go.AddComponent<AudioSource>();
            go.AddComponent<GameAudioManager>();
        }

        private static void SetupVfxManager()
        {
            GameObject existing = GameObject.Find("GameVfxManager");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var go = new GameObject("GameVfxManager");
            go.AddComponent<GameVfxManager>();
        }

        private static void SetupBackgroundDecor()
        {
            GameObject existing = GameObject.Find("BackgroundDecor");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var root = new GameObject("BackgroundDecor");
            Sprite cloudSprite = CreateCloudSprite();
            Sprite hillSprite = CreateHillSprite();

            // Bukit jauh di sepanjang stage
            for (float x = -4f; x <= 156f; x += 18f)
            {
                var hill = new GameObject($"BgHill_{x:0}");
                hill.transform.SetParent(root.transform, false);
                hill.transform.position = new Vector3(x, -0.35f, 0f);
                hill.transform.localScale = new Vector3(6.5f, 3.2f, 1f);

                var sr = hill.AddComponent<SpriteRenderer>();
                sr.sprite = hillSprite;
                sr.sortingOrder = -12;
                sr.color = new Color(0.26f, 0.48f, 0.62f, 0.55f);
            }

            // Awan pixel-art di langit
            for (float x = 0f; x <= 150f; x += 13f)
            {
                float y = 3.8f + ((int)x % 3) * 0.85f;
                var cloud = new GameObject($"BgCloud_{x:0}");
                cloud.transform.SetParent(root.transform, false);
                cloud.transform.position = new Vector3(x, y, 0f);
                cloud.transform.localScale = new Vector3(2.4f, 2.0f, 1f);

                var sr = cloud.AddComponent<SpriteRenderer>();
                sr.sprite = cloudSprite;
                sr.sortingOrder = -10;
                sr.color = new Color(1f, 1f, 1f, 0.82f);
            }

            // Gedung Kampus Gunadarma pixel-art di area start
            string buildingPath = $"{UiTexFolder}/GunadarmaBuilding.png";
            if (File.Exists(buildingPath))
            {
                AssetDatabase.ImportAsset(buildingPath, ImportAssetOptions.ForceUpdate);
                var bldImporter = AssetImporter.GetAtPath(buildingPath) as TextureImporter;
                if (bldImporter != null)
                {
                    bldImporter.textureType = TextureImporterType.Sprite;
                    bldImporter.spriteImportMode = SpriteImportMode.Single;
                    bldImporter.spritePixelsPerUnit = 16f;
                    bldImporter.filterMode = FilterMode.Point;
                    bldImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    var settings = new TextureImporterSettings();
                    bldImporter.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                    bldImporter.SetTextureSettings(settings);
                    bldImporter.SaveAndReimport();
                }

                Sprite bldSprite = AssetDatabase.LoadAssetAtPath<Sprite>(buildingPath);
                if (bldSprite != null)
                {
                    var bldObj = new GameObject("GunadarmaCampusStart");
                    bldObj.transform.SetParent(root.transform, false);
                    bldObj.transform.position = new Vector3(1.5f, -1.25f, 0f);
                    bldObj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

                    var bldSr = bldObj.AddComponent<SpriteRenderer>();
                    bldSr.sprite = bldSprite;
                    bldSr.sortingOrder = -5;
                }
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }
            else if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static void SetupHealthHud(GameObject canvasObj, Font font, Sprite hudPill, Sprite heartFull, Sprite heartEmpty)
        {
            Transform oldPill = canvasObj.transform.Find("HealthHudPanel");
            if (oldPill != null)
                Object.DestroyImmediate(oldPill.gameObject);

            Transform oldHealthText = canvasObj.transform.Find("HealthText");
            if (oldHealthText != null)
                Object.DestroyImmediate(oldHealthText.gameObject);

            var pill = new GameObject("HealthHudPanel");
            pill.transform.SetParent(canvasObj.transform, false);
            var pillRect = pill.AddComponent<RectTransform>();
            pillRect.anchorMin = new Vector2(0f, 1f);
            pillRect.anchorMax = new Vector2(0f, 1f);
            pillRect.pivot = new Vector2(0f, 1f);
            pillRect.anchoredPosition = new Vector2(20f, -16f);
            pillRect.sizeDelta = new Vector2(236f, 52f);

            var pillImg = pill.AddComponent<Image>();
            pillImg.sprite = hudPill;
            pillImg.type = Image.Type.Sliced;

            Text label = CreateCrispText(pill.transform, "HealthText", "NYAWA", font, 17, FontStyle.Bold,
                new Color(1f, 0.90f, 0.55f, 1f), new Vector2(-58f, 0f), new Vector2(82f, 36f), TextAnchor.MiddleLeft, addOutline: true);

            Image[] hearts = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var hObj = new GameObject($"Heart_{i + 1}");
                hObj.transform.SetParent(pill.transform, false);
                var hRect = hObj.AddComponent<RectTransform>();
                hRect.anchoredPosition = new Vector2(12f + i * 34f, 0f);
                hRect.sizeDelta = new Vector2(26f, 26f);

                var hImg = hObj.AddComponent<Image>();
                hImg.sprite = heartFull;
                hImg.preserveAspect = true;
                hearts[i] = hImg;
            }

            var healthUI = canvasObj.GetComponent<HealthUI>();
            if (healthUI == null)
                healthUI = canvasObj.AddComponent<HealthUI>();

            var so = new SerializedObject(healthUI);
            so.FindProperty("playerHealth").objectReferenceValue = Object.FindFirstObjectByType<PlayerHealth>();
            so.FindProperty("healthText").objectReferenceValue = label;
            so.FindProperty("heartIcons").arraySize = hearts.Length;
            for (int i = 0; i < hearts.Length; i++)
                so.FindProperty("heartIcons").GetArrayElementAtIndex(i).objectReferenceValue = hearts[i];
            so.FindProperty("heartFullSprite").objectReferenceValue = heartFull;
            so.FindProperty("heartEmptySprite").objectReferenceValue = heartEmpty;
            so.ApplyModifiedProperties();
        }

        private static void EnsureBreadUI(GameObject canvasObj, Font font, Sprite hudPill)
        {
            var breadEffects = Object.FindFirstObjectByType<PlayerBreadEffects>();
            if (breadEffects == null)
                return;

            Transform oldText = canvasObj.transform.Find("BreadStatusText");
            if (oldText != null)
                Object.DestroyImmediate(oldText.gameObject);

            Text text = CreateCrispText(canvasObj.transform, "BreadStatusText", "", font, 19, FontStyle.Bold,
                new Color(1f, 0.95f, 0.55f, 1f), Vector2.zero, new Vector2(520f, 150f), TextAnchor.UpperLeft, addOutline: true);

            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -70f);

            var breadUI = canvasObj.GetComponent<BreadUI>();
            if (breadUI == null)
                breadUI = canvasObj.AddComponent<BreadUI>();

            var so = new SerializedObject(breadUI);
            so.FindProperty("breadEffects").objectReferenceValue = breadEffects;
            so.FindProperty("breadStatusText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        private static void EnsureBossAndEndUI(GameObject canvasObj, Font font, Sprite panelFrame, Sprite hudPill, Sprite btnGreen)
        {
            var health = Object.FindFirstObjectByType<PlayerHealth>();
            var boss = Object.FindFirstObjectByType<ZakiBoss>();
            var finish = Object.FindFirstObjectByType<FinishLine>();
            if (health == null || boss == null || finish == null)
                return;

            string[] oldBossObjs = { "BossHealthText", "BossBarRoot", "NoticeText", "EndPanel" };
            foreach (string objName in oldBossObjs)
            {
                Transform t = canvasObj.transform.Find(objName);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }

            // Boss HP Bar berbingkai di atas tengah
            var bossRoot = new GameObject("BossBarRoot");
            bossRoot.transform.SetParent(canvasObj.transform, false);
            var bossRect = bossRoot.AddComponent<RectTransform>();
            bossRect.anchorMin = new Vector2(0.5f, 1f);
            bossRect.anchorMax = new Vector2(0.5f, 1f);
            bossRect.pivot = new Vector2(0.5f, 1f);
            bossRect.anchoredPosition = new Vector2(0f, -14f);
            bossRect.sizeDelta = new Vector2(380f, 52f);

            var bossBg = bossRoot.AddComponent<Image>();
            bossBg.sprite = hudPill;
            bossBg.type = Image.Type.Sliced;

            Text bossText = CreateCrispText(bossRoot.transform, "BossHealthText", "ZAKI RAKSASA  5/5", font, 17, FontStyle.Bold,
                new Color(1f, 0.85f, 0.45f, 1f), new Vector2(0f, 11f), new Vector2(350f, 24f), TextAnchor.MiddleCenter, addOutline: true);

            var barTrack = new GameObject("BossHpTrack");
            barTrack.transform.SetParent(bossRoot.transform, false);
            var trackRect = barTrack.AddComponent<RectTransform>();
            trackRect.anchoredPosition = new Vector2(0f, -10f);
            trackRect.sizeDelta = new Vector2(340f, 14f);
            barTrack.AddComponent<Image>().color = new Color(0.18f, 0.08f, 0.10f, 1f);

            var barFillObj = new GameObject("BossHpFill");
            barFillObj.transform.SetParent(barTrack.transform, false);
            var fillRect = barFillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            var fillImg = barFillObj.AddComponent<Image>();
            fillImg.sprite = CreateSolidWhiteSprite();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.color = new Color(0.92f, 0.24f, 0.22f, 1f);

            // Notifikasi tengah atas
            Text noticeText = CreateCrispText(canvasObj.transform, "NoticeText", "", font, 24, FontStyle.Bold,
                new Color(1f, 0.92f, 0.35f, 1f), Vector2.zero, new Vector2(720f, 60f), TextAnchor.MiddleCenter, addOutline: true);
            var noticeRect = noticeText.GetComponent<RectTransform>();
            noticeRect.anchorMin = new Vector2(0.5f, 0.78f);
            noticeRect.anchorMax = new Vector2(0.5f, 0.78f);

            // EndPanel bergaya SPUM
            var panelObj = new GameObject("EndPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelObj.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.78f);

            var card = new GameObject("EndCard");
            card.transform.SetParent(panelObj.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(560f, 300f);
            var cardImg = card.AddComponent<Image>();
            cardImg.sprite = panelFrame;
            cardImg.type = Image.Type.Sliced;

            Text endTitle = CreateCrispText(card.transform, "EndTitleText", "", font, 34, FontStyle.Bold,
                Color.white, new Vector2(0f, 45f), new Vector2(500f, 130f), TextAnchor.MiddleCenter, addOutline: true);

            CreatePixelButton(card.transform, "RestartButton", "MAIN LAGI", font, btnGreen,
                new Vector2(0f, -72f), new Vector2(240f, 54f), out Button btn);

            var ui = canvasObj.GetComponent<BossAndEndUI>();
            if (ui == null)
                ui = canvasObj.AddComponent<BossAndEndUI>();

            UnityEventTools.AddPersistentListener(btn.onClick, ui.RestartGame);

            var so = new SerializedObject(ui);
            so.FindProperty("playerHealth").objectReferenceValue = health;
            so.FindProperty("finalBoss").objectReferenceValue = boss;
            so.FindProperty("finishLine").objectReferenceValue = finish;
            so.FindProperty("bossHealthText").objectReferenceValue = bossText;
            so.FindProperty("bossBarRoot").objectReferenceValue = bossRoot;
            so.FindProperty("bossHealthFill").objectReferenceValue = fillImg;
            so.FindProperty("noticeText").objectReferenceValue = noticeText;
            so.FindProperty("endPanel").objectReferenceValue = panelObj;
            so.FindProperty("endTitleText").objectReferenceValue = endTitle;
            so.ApplyModifiedProperties();

            bossRoot.SetActive(false);
            panelObj.SetActive(false);
        }

        private static void PolishMenuHint(Transform canvasTransform, Font font, Sprite hudPill)
        {
            Transform oldHint = canvasTransform.Find("MenuHintText");
            if (oldHint != null)
                Object.DestroyImmediate(oldHint.gameObject);

            Transform oldPill = canvasTransform.Find("MenuHintPill");
            if (oldPill != null)
                Object.DestroyImmediate(oldPill.gameObject);

            var pill = new GameObject("MenuHintPill");
            pill.transform.SetParent(canvasTransform, false);
            var pillRect = pill.AddComponent<RectTransform>();
            pillRect.anchorMin = new Vector2(1f, 1f);
            pillRect.anchorMax = new Vector2(1f, 1f);
            pillRect.pivot = new Vector2(1f, 1f);
            pillRect.anchoredPosition = new Vector2(-20f, -16f);
            pillRect.sizeDelta = new Vector2(154f, 44f);

            var img = pill.AddComponent<Image>();
            img.sprite = hudPill;
            img.type = Image.Type.Sliced;

            CreateCrispText(pill.transform, "MenuHintText", "[Esc] Menu", font, 16, FontStyle.Bold,
                new Color(0.95f, 0.92f, 0.80f, 1f), Vector2.zero, new Vector2(126f, 30f), TextAnchor.MiddleCenter, addOutline: true);
        }

        private static void SetupMainMenu(Transform canvasTransform, Font font, Sprite panelFrame, Sprite hudPill,
            Sprite btnGreen, Sprite btnBlue, Sprite btnRed)
        {
            Transform oldMenu = canvasTransform.Find("MainMenuPanel");
            if (oldMenu != null)
                Object.DestroyImmediate(oldMenu.gameObject);

            var existingScript = canvasTransform.GetComponent<MainMenuUI>();
            if (existingScript != null)
                Object.DestroyImmediate(existingScript);

            var menuPanel = new GameObject("MainMenuPanel");
            menuPanel.transform.SetParent(canvasTransform, false);
            var panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            menuPanel.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.11f, 0.84f);

            var card = new GameObject("MenuCard");
            card.transform.SetParent(menuPanel.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(610f, 450f);
            var cardImg = card.AddComponent<Image>();
            cardImg.sprite = panelFrame;
            cardImg.type = Image.Type.Sliced;

            CreateCrispText(card.transform, "TitleText", "HILMI VS ROTI", font, 44, FontStyle.Bold,
                new Color(1f, 0.86f, 0.28f, 1f), new Vector2(0f, 158f), new Vector2(540f, 64f), TextAnchor.MiddleCenter, addOutline: true);

            CreateCrispText(card.transform, "SubtitleText", "Kalahkan Zaki Raksasa & Sentuh Bendera Finish!", font, 17, FontStyle.Normal,
                new Color(0.88f, 0.92f, 1f, 1f), new Vector2(0f, 110f), new Vector2(520f, 32f), TextAnchor.MiddleCenter, addOutline: false);

            var guideBox = new GameObject("ControlsBox");
            guideBox.transform.SetParent(card.transform, false);
            var guideRect = guideBox.AddComponent<RectTransform>();
            guideRect.anchoredPosition = new Vector2(0f, 38f);
            guideRect.sizeDelta = new Vector2(530f, 82f);
            var guideImg = guideBox.AddComponent<Image>();
            guideImg.sprite = hudPill;
            guideImg.type = Image.Type.Sliced;

            CreateCrispText(guideBox.transform, "ControlsText",
                "A / D atau Panah : Jalan   |   Spasi / W : Lompat\nInjak kepala musuh  •  Roti = Buff (Awas Roti Jamuran!)",
                font, 16, FontStyle.Normal, new Color(0.92f, 0.94f, 0.98f, 1f),
                Vector2.zero, new Vector2(490f, 64f), TextAnchor.MiddleCenter, addOutline: false);

            var mainMenuUI = canvasTransform.gameObject.AddComponent<MainMenuUI>();

            Text startLabel = CreatePixelButton(card.transform, "StartButton", "MULAI MAIN", font, btnGreen,
                new Vector2(0f, -44f), new Vector2(280f, 50f), out Button startBtn);

            Text soundLabel = CreatePixelButton(card.transform, "SoundButton", "SUARA SFX: NYALA", font, btnBlue,
                new Vector2(0f, -106f), new Vector2(280f, 46f), out Button soundBtn);

            CreatePixelButton(card.transform, "QuitButton", "KELUAR", font, btnRed,
                new Vector2(0f, -166f), new Vector2(280f, 44f), out Button quitBtn);

            UnityEventTools.AddPersistentListener(startBtn.onClick, mainMenuUI.StartOrResumeGame);
            UnityEventTools.AddPersistentListener(soundBtn.onClick, mainMenuUI.ToggleSound);
            UnityEventTools.AddPersistentListener(quitBtn.onClick, mainMenuUI.QuitGame);

            Transform endPanel = canvasTransform.Find("EndPanel");
            if (endPanel != null)
                menuPanel.transform.SetSiblingIndex(endPanel.GetSiblingIndex());

            var so = new SerializedObject(mainMenuUI);
            so.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            so.FindProperty("startButtonLabel").objectReferenceValue = startLabel;
            so.FindProperty("soundButtonLabel").objectReferenceValue = soundLabel;
            so.FindProperty("endScreenPanel").objectReferenceValue = endPanel != null ? endPanel.gameObject : null;
            so.FindProperty("showOnStart").boolValue = true;
            so.ApplyModifiedProperties();
        }

        private static Text CreateCrispText(
            Transform parent, string name, string content, Font font, int fontSize, FontStyle style,
            Color color, Vector2 pos, Vector2 size, TextAnchor alignment, bool addOutline)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size * 2f;
            rect.localScale = new Vector3(0.5f, 0.5f, 1f);

            var text = go.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize * 2;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;

            if (addOutline)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0.04f, 0.03f, 0.07f, 0.85f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            return text;
        }

        private static Text CreatePixelButton(
            Transform parent, string name, string label, Font font, Sprite btnSprite, Vector2 pos, Vector2 size,
            out Button button)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = btnObj.AddComponent<Image>();
            img.sprite = btnSprite;
            img.type = Image.Type.Sliced;
            button = btnObj.AddComponent<Button>();

            return CreateCrispText(btnObj.transform, "Text", label, font, 20, FontStyle.Bold,
                Color.white, Vector2.zero, size, TextAnchor.MiddleCenter, addOutline: true);
        }

        private static Sprite CreateSlicedUiSprite(string fileName, int size, int border, Color outer, Color trim, Color fill)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isOuter = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    bool isTrim = x == 1 || y == 1 || x == size - 2 || y == size - 2 || x == 2 || y == 2 || x == size - 3 || y == size - 3;
                    bool isInnerDark = x == 3 || y == 3 || x == size - 4 || y == size - 4;
                    if (isOuter || isInnerDark)
                        tex.SetPixel(x, y, outer);
                    else if (isTrim)
                        tex.SetPixel(x, y, trim);
                    else
                        tex.SetPixel(x, y, fill);
                }
            }
            tex.Apply();
            return SavePixelSprite($"{UiTexFolder}/{fileName}.png", tex, 100f, new Vector4(border, border, border, border));
        }

        private static Sprite CreateButtonSprite(string fileName, Color topHighlight, Color fill, Color bottomShadow)
        {
            const int size = 16;
            Color outline = new Color(0.08f, 0.06f, 0.12f, 1f);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x == 0 || y == 0 || x == size - 1 || y == size - 1)
                        tex.SetPixel(x, y, outline);
                    else if (y >= size - 3)
                        tex.SetPixel(x, y, topHighlight);
                    else if (y <= 2)
                        tex.SetPixel(x, y, bottomShadow);
                    else
                        tex.SetPixel(x, y, fill);
                }
            }
            tex.Apply();
            return SavePixelSprite($"{UiTexFolder}/{fileName}.png", tex, 100f, new Vector4(4, 4, 4, 4));
        }

        private static Sprite CreateHeartSprite(string fileName, bool filled)
        {
            string[] pattern =
            {
                "................",
                "..OOOO....OOOO..",
                ".OHFFFO..OFFFFO.",
                "OHHFFFFOOFFFFFFO",
                "OHFFFFFFFFFFFFFO",
                "OFFFFFFFFFFFFFFO",
                "OFFFFFFFFFFFFFFO",
                ".OFFFFFFFFFFFFO.",
                "..OFFFFFFFFFFO..",
                "...OFFFFFFFFO...",
                "....OFFFFFFO....",
                ".....OFFFFO.....",
                "......OFFO......",
                ".......OO.......",
                "................",
                "................"
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color(0.12f, 0.06f, 0.10f, 1f);
            Color fill = filled ? new Color(0.94f, 0.22f, 0.28f, 1f) : new Color(0.28f, 0.24f, 0.32f, 0.85f);
            Color highlight = filled ? new Color(1f, 0.62f, 0.65f, 1f) : new Color(0.40f, 0.36f, 0.45f, 0.85f);

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            {
                string row = pattern[15 - y];
                for (int x = 0; x < 16; x++)
                {
                    char c = row[x];
                    Color col = c switch
                    {
                        'O' => outline,
                        'F' => fill,
                        'H' => highlight,
                        _ => clear
                    };
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return SavePixelSprite($"{UiTexFolder}/{fileName}.png", tex, 16f, Vector4.zero);
        }

        private static Sprite CreateSolidWhiteSprite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return SavePixelSprite($"{UiTexFolder}/SolidWhite.png", tex, 4f, Vector4.zero);
        }

        private static Sprite CreateCloudSprite()
        {
            var tex = new Texture2D(32, 16, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color white = new Color(0.96f, 0.98f, 1f, 1f);
            Color shade = new Color(0.82f, 0.89f, 0.96f, 1f);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool inMain = Vector2.Distance(new Vector2(x, y), new Vector2(16f, 7f)) <= 6.5f;
                    bool inLeft = Vector2.Distance(new Vector2(x, y), new Vector2(9f, 5.5f)) <= 4.8f;
                    bool inRight = Vector2.Distance(new Vector2(x, y), new Vector2(23f, 5.5f)) <= 4.8f;
                    if (y >= 2 && (inMain || inLeft || inRight))
                        tex.SetPixel(x, y, y <= 4 ? shade : white);
                    else
                        tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();
            return SavePixelSprite($"{UiTexFolder}/BgCloud.png", tex, 16f, Vector4.zero);
        }

        private static Sprite CreateHillSprite()
        {
            var tex = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color hill = new Color(0.75f, 0.88f, 0.92f, 1f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31.5f;
                    float maxH = (1f - dx * dx) * 28f;
                    tex.SetPixel(x, y, y <= maxH ? hill : clear);
                }
            }
            tex.Apply();
            return SavePixelSprite($"{UiTexFolder}/BgHill.png", tex, 32f, Vector4.zero);
        }

        private static Sprite SavePixelSprite(string path, Texture2D tex, float ppu, Vector4 border)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                if (border != Vector4.zero)
                    importer.spriteBorder = border;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ApplyPixelOutlineMaterials()
        {
            Shader shader = Shader.Find("HilmiVSRoti/PixelOutlineSprite");
            if (shader == null)
            {
                Debug.LogWarning("[Hilmi VS Roti] Shader 'HilmiVSRoti/PixelOutlineSprite' belum tercompile. Jalankan ulang menu 8 sekali lagi setelah compile selesai.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            Material charMat = CreateOrUpdateOutlineMat("Assets/Materials/CharPixelOutlineMat.mat", shader, 512f, 64f, 0.60f, 0.30f);
            Material propMat = CreateOrUpdateOutlineMat("Assets/Materials/PropPixelOutlineMat.mat", shader, 0f, 0f, 0.50f, 0f);

            AssignMaterialToRoot("Hilmi", charMat);
            AssignMaterialToRoot("ZakiBoss", charMat);
            AssignMaterialToRoot("Enemies", charMat);
            ConfigureKenyMaterials(propMat, charMat);

            AssignMaterialToRoot("Breads", propMat);
            AssignMaterialToRoot("Checkpoints", propMat);
            AssignMaterialToRoot("FinishLine", propMat);
            AssignMaterialToRoot("DynamicObstacles", propMat);

            AssignMaterialToPrefab("Assets/Prefabs/Enemies/LabutitProjectile.prefab", propMat);
            AssignMaterialToPrefab("Assets/Prefabs/BossShockwave.prefab", propMat);
        }

        private static Material CreateOrUpdateOutlineMat(string path, Shader shader, float stepsX, float stepsY, float cutoff, float innerEdge)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetColor("_Color", Color.white);
            mat.SetColor("_OutlineColor", new Color(0.05f, 0.04f, 0.08f, 1f));
            mat.SetFloat("_OutlineEnabled", 1f);
            mat.SetFloat("_PixelStepsX", stepsX);
            mat.SetFloat("_PixelStepsY", stepsY);
            mat.SetFloat("_AlphaCutoff", cutoff);
            mat.SetFloat("_InnerEdgeThreshold", innerEdge);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AssignMaterialToRoot(string objectName, Material mat)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return;

            foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sharedMaterial = mat;
                EditorUtility.SetDirty(sr);
            }
        }

        private static void AssignMaterialToPrefab(string prefabPath, Material mat)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;

            foreach (SpriteRenderer sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sharedMaterial = mat;
            EditorUtility.SetDirty(prefab);
        }

        private static void ConfigureKenyMaterials(Material disguisedMat, Material revealedMat)
        {
            foreach (KenyEnemy keny in Object.FindObjectsByType<KenyEnemy>(FindObjectsSortMode.None))
            {
                var sr = keny.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sharedMaterial = disguisedMat;
                    EditorUtility.SetDirty(sr);
                }

                var so = new SerializedObject(keny);
                so.FindProperty("disguisedMaterial").objectReferenceValue = disguisedMat;
                so.FindProperty("revealedMaterial").objectReferenceValue = revealedMat;
                so.ApplyModifiedProperties();
            }
        }

        private static void StyleDynamicObstacles()
        {
            GameObject root = GameObject.Find("DynamicObstacles");
            if (root == null)
                return;

            Sprite movingSprite = CreatePlatformSprite("MovingPlatformSprite",
                new Color(0.32f, 0.72f, 0.44f, 1f),
                new Color(0.22f, 0.52f, 0.32f, 1f),
                new Color(0.14f, 0.34f, 0.21f, 1f),
                cracked: false);

            Sprite fallingSprite = CreatePlatformSprite("FallingBlockSprite",
                new Color(0.78f, 0.64f, 0.42f, 1f),
                new Color(0.58f, 0.44f, 0.28f, 1f),
                new Color(0.38f, 0.28f, 0.16f, 1f),
                cracked: true);

            foreach (Transform child in root.transform)
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr == null)
                    continue;

                bool isFalling = child.GetComponent<FallingBlock>() != null;
                sr.sprite = isFalling ? fallingSprite : movingSprite;
                sr.color = Color.white;
                sr.drawMode = SpriteDrawMode.Sliced;

                Vector3 scale = child.localScale;
                if (scale.x > 1.05f || Mathf.Abs(scale.y - 1f) > 0.05f)
                {
                    sr.size = new Vector2(scale.x, scale.y);
                    var col = child.GetComponent<BoxCollider2D>();
                    if (col != null)
                        col.size = new Vector2(scale.x, scale.y);
                    child.localScale = Vector3.one;
                }
                else
                {
                    sr.size = sr.size == Vector2.zero ? new Vector2(1.5f, 0.5f) : sr.size;
                }

                EditorUtility.SetDirty(child.gameObject);
            }
        }

        private static Sprite CreatePlatformSprite(string fileName, Color topColor, Color bodyColor, Color bottomColor, bool cracked)
        {
            const int w = 32;
            const int h = 16;
            Color outline = new Color(0.05f, 0.04f, 0.08f, 1f);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                    bool isCrack = cracked && (
                        (y >= 6 && y <= 12 && x == 10 + (12 - y) / 2) ||
                        (y >= 4 && y <= 10 && x == 22 - (10 - y) / 2));
                    bool isRivet = !cracked && (y == 8) && (x == 4 || x == 27);

                    if (isBorder || isCrack || isRivet)
                        tex.SetPixel(x, y, outline);
                    else if (y >= h - 4)
                        tex.SetPixel(x, y, topColor);
                    else if (y <= 3)
                        tex.SetPixel(x, y, bottomColor);
                    else
                        tex.SetPixel(x, y, bodyColor);
                }
            }

            tex.Apply();
            return SavePixelSprite($"{UiTexFolder}/{fileName}.png", tex, 32f, new Vector4(5, 4, 5, 4));
        }

        private static void ReplaceBrickWallsWithCameraBounds()
        {
            GameObject groundObj = GameObject.Find("GroundTilemap");
            if (groundObj != null)
            {
                var groundMap = groundObj.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                var grassTile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Tiles/GrassTopTile.asset");
                var dirtTile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Tiles/DirtTile.asset");

                if (groundMap != null)
                {
                    for (int y = -6; y <= 12; y++)
                    {
                        groundMap.SetTile(new Vector3Int(-7, y, 0), null);
                        groundMap.SetTile(new Vector3Int(151, y, 0), null);
                    }

                    if (grassTile != null && dirtTile != null)
                    {
                        for (int x = -16; x <= -7; x++)
                        {
                            groundMap.SetTile(new Vector3Int(x, -2, 0), grassTile);
                            for (int y = -6; y <= -3; y++)
                                groundMap.SetTile(new Vector3Int(x, y, 0), dirtTile);
                        }
                        for (int x = 151; x <= 162; x++)
                        {
                            groundMap.SetTile(new Vector3Int(x, -2, 0), grassTile);
                            for (int y = -6; y <= -3; y++)
                                groundMap.SetTile(new Vector3Int(x, y, 0), dirtTile);
                        }
                    }

                    LevelSetupEditor.ApplyTilemapEdgeOutlines();
                }
            }

            GameObject oldBounds = GameObject.Find("LevelBounds");
            if (oldBounds != null)
                Object.DestroyImmediate(oldBounds);

            var boundsRoot = new GameObject("LevelBounds");
            int groundLayer = LayerMask.NameToLayer("Ground");
            CreateInvisibleWall(boundsRoot.transform, "LeftBound", -7f, groundLayer);
            CreateInvisibleWall(boundsRoot.transform, "RightBound", 150.5f, groundLayer);

            var camFollow = Object.FindFirstObjectByType<HilmiVSRoti.CameraSystem.CameraFollow2D>();
            if (camFollow != null)
            {
                var so = new SerializedObject(camFollow);
                so.FindProperty("minX").floatValue = 4.5f;
                so.FindProperty("maxX").floatValue = 139.5f;
                so.ApplyModifiedProperties();
            }
        }

        private static void CreateInvisibleWall(Transform parent, string name, float x, int layer)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.position = new Vector3(x, 4f, 0f);
            if (layer != -1)
                wall.layer = layer;

            var col = wall.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 30f);
        }
    }
}
