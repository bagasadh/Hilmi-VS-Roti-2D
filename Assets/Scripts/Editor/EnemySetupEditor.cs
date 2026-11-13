using System.IO;
using System.Linq;
using HilmiVSRoti.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HilmiVSRoti.EditorTools
{
    /// <summary>
    /// Tool Editor untuk Tahap 5:
    /// Membuat Animator & prefab proyektil untuk musuh (jek, labutit, KenyGK),
    /// serta menempatkan mereka di stage sesuai alur game.
    /// </summary>
    public static class EnemySetupEditor
    {
        private const string EnemyAnimFolder = "Assets/Animations/Enemies";
        private const string PrefabsFolder = "Assets/Prefabs";

        [MenuItem("Tools/Hilmi VS Roti/5. Setup Musuh (jek, labutit, KenyGK)")]
        public static void SetupTahap5()
        {
            EnsureFolders();

            AnimatorController jekCtrl = CreateJekAnimator();
            AnimatorController labutitCtrl = CreateSingleLoopAnimator("LabutitAnimator", "Assets/Sprites/labutit/labutit_Idle_sheet.png", 8f);
            AnimatorController kenyCtrl = CreateSingleLoopAnimator("KenyAnimator", "Assets/Sprites/KenyGK/KenyGK_ArmatureAction_sheet.png", 10f);

            Sprite jekSprite = GetFirstSprite("Assets/Sprites/jek/jek_Idle_sheet.png");
            Sprite labutitSprite = GetFirstSprite("Assets/Sprites/labutit/labutit_Idle_sheet.png");
            Sprite kenySprite = GetFirstSprite("Assets/Sprites/KenyGK/KenyGK_ArmatureAction_sheet.png");
            Sprite crateSprite = CreateCrateDisguiseSprite();
            LabutitProjectile projPrefab = CreateLabutitProjectilePrefab();

            GameObject existing = GameObject.Find("Enemies");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var root = new GameObject("Enemies");

            // 0:20 - 0:50: Musuh jek
            SpawnJek("Jek_1", root.transform, new Vector3(29f, -0.3f, 0f), jekSprite, jekCtrl);
            SpawnJek("Jek_2", root.transform, new Vector3(53f, -0.3f, 0f), jekSprite, jekCtrl);

            // 0:50 - 1:20: labutit & KenyGK (diberi jarak aman dari duri, roti, dan atap bata)
            SpawnLabutit("Labutit_1", root.transform, new Vector3(67.8f, -0.3f, 0f), labutitSprite, labutitCtrl, projPrefab);
            SpawnKeny("KenyGK_1", root.transform, new Vector3(87.5f, -0.3f, 0f), crateSprite, kenySprite, kenyCtrl);

            // 1:20 - 1:40: Area ujian
            SpawnLabutit("Labutit_2", root.transform, new Vector3(97.5f, -0.3f, 0f), labutitSprite, labutitCtrl, projPrefab);
            SpawnJek("Jek_3", root.transform, new Vector3(117f, -0.3f, 0f), jekSprite, jekCtrl);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Hilmi VS Roti] Tahap 5 selesai: Musuh jek, labutit, dan KenyGK siap dites!");
        }

        private static void SpawnJek(string name, Transform parent, Vector3 pos, Sprite sprite, AnimatorController ctrl)
        {
            GameObject go = CreateBaseEnemyObject(name, parent, pos, sprite, ctrl, new Vector2(1.0f, 1.1f), new Vector2(0f, -0.05f));
            go.AddComponent<JekEnemy>();
        }

        private static void SpawnLabutit(string name, Transform parent, Vector3 pos, Sprite sprite, AnimatorController ctrl, LabutitProjectile projPrefab)
        {
            GameObject go = CreateBaseEnemyObject(name, parent, pos, sprite, ctrl, new Vector2(1.1f, 1.1f), new Vector2(0f, -0.1f));
            var labutit = go.AddComponent<LabutitEnemy>();
            var so = new SerializedObject(labutit);
            so.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
            so.FindProperty("throwArcHeight").floatValue = 3.6f;
            so.ApplyModifiedProperties();
        }

        private static void SpawnKeny(string name, Transform parent, Vector3 pos, Sprite disguise, Sprite revealed, AnimatorController ctrl)
        {
            GameObject go = CreateBaseEnemyObject(name, parent, pos, disguise, ctrl, new Vector2(0.9f, 1.25f), new Vector2(0f, -0.05f));
            var keny = go.AddComponent<KenyEnemy>();
            var so = new SerializedObject(keny);
            so.FindProperty("disguiseSprite").objectReferenceValue = disguise;
            so.FindProperty("revealedSprite").objectReferenceValue = revealed;
            so.ApplyModifiedProperties();
        }

        private static GameObject CreateBaseEnemyObject(string name, Transform parent, Vector3 pos, Sprite sprite, AnimatorController ctrl, Vector2 colSize, Vector2 colOffset)
        {
            int enemyLayer = EnsureEnemyLayerExists();
            var go = new GameObject(name);
            go.layer = enemyLayer;
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = colSize;
            col.offset = colOffset;

            return go;
        }

        private static AnimatorController CreateJekAnimator()
        {
            string ctrlPath = $"{EnemyAnimFolder}/JekAnimator.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null)
                AssetDatabase.DeleteAsset(ctrlPath);

            AnimationClip idle = CreateClip("Assets/Sprites/jek/jek_Idle_sheet.png", $"{EnemyAnimFolder}/jek_Idle.anim", 8f);
            AnimationClip run = CreateClip("Assets/Sprites/jek/jek_Run_sheet.png", $"{EnemyAnimFolder}/jek_Run.anim", 12f);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            var sm = ctrl.layers[0].stateMachine;
            var idleState = sm.AddState("Idle");
            idleState.motion = idle;
            sm.defaultState = idleState;

            var runState = sm.AddState("Run");
            runState.motion = run;

            var t1 = idleState.AddTransition(runState);
            t1.hasExitTime = false;
            t1.duration = 0.05f;
            t1.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

            var t2 = runState.AddTransition(idleState);
            t2.hasExitTime = false;
            t2.duration = 0.05f;
            t2.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

            return ctrl;
        }

        private static AnimatorController CreateSingleLoopAnimator(string name, string sheetPath, float fps)
        {
            string ctrlPath = $"{EnemyAnimFolder}/{name}.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null)
                AssetDatabase.DeleteAsset(ctrlPath);

            AnimationClip clip = CreateClip(sheetPath, $"{EnemyAnimFolder}/{name}_Loop.anim", fps);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var state = ctrl.layers[0].stateMachine.AddState("Loop");
            state.motion = clip;
            ctrl.layers[0].stateMachine.defaultState = state;
            return ctrl;
        }

        private static AnimationClip CreateClip(string sheetPath, string clipPath, float fps)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .OrderBy(s => int.TryParse(s.name.Split('_')[^1], out int i) ? i : 0)
                .ToArray();

            var clip = new AnimationClip { frameRate = fps };
            var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
            var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe { time = i / fps, value = s }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
                AssetDatabase.DeleteAsset(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static LabutitProjectile CreateLabutitProjectilePrefab()
        {
            Sprite projSprite = CreateProjectileSprite();
            string prefabPath = $"{PrefabsFolder}/LabutitProjectile.prefab";

            var temp = new GameObject("LabutitProjectile");
            var sr = temp.AddComponent<SpriteRenderer>();
            sr.sprite = projSprite;
            sr.sortingOrder = 2;

            var rb = temp.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.8f;

            var col = temp.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            temp.AddComponent<LabutitProjectile>();
            GameObject prefabObj = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            Object.DestroyImmediate(temp);
            return prefabObj.GetComponent<LabutitProjectile>();
        }

        public static Sprite RefreshCrateDisguiseSprite()
        {
            const string path = "Assets/Tiles/Textures/CrateDisguise.png";
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color(0.05f, 0.04f, 0.08f, 1f);
            Color woodLight = new Color(0.72f, 0.44f, 0.20f, 1f);
            Color wood = new Color(0.56f, 0.32f, 0.14f, 1f);
            Color woodDark = new Color(0.40f, 0.22f, 0.09f, 1f);
            Color gold = new Color(0.95f, 0.78f, 0.24f, 1f);
            Color goldDark = new Color(0.70f, 0.50f, 0.12f, 1f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (x < 2 || x > 29 || y < 1 || y > 28)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    bool isCornerCut = (x == 2 || x == 29) && (y == 1 || y == 28);
                    if (isCornerCut)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    bool isOuterBorder = x == 2 || x == 29 || y == 1 || y == 28 ||
                                         ((x == 3 || x == 28) && (y == 2 || y == 27));
                    bool isLidSeam = y == 18 || y == 19;
                    bool isGoldStrap = (x >= 6 && x <= 8) || (x >= 23 && x <= 25);
                    bool isLock = x >= 13 && x <= 18 && y >= 14 && y <= 21;
                    bool isLockHole = x >= 15 && x <= 16 && y >= 16 && y <= 18;

                    if (isOuterBorder || isLidSeam)
                        tex.SetPixel(x, y, outline);
                    else if (isLockHole)
                        tex.SetPixel(x, y, outline);
                    else if (isLock)
                        tex.SetPixel(x, y, (x == 13 || x == 18 || y == 14 || y == 21) ? outline : gold);
                    else if (isGoldStrap)
                        tex.SetPixel(x, y, y < 18 ? goldDark : gold);
                    else if (y > 19)
                        tex.SetPixel(x, y, (y == 27 || y == 23) ? woodLight : wood);
                    else
                        tex.SetPixel(x, y, (y == 10 || y == 2) ? woodDark : wood);
                }
            }
            SaveTextureAsSprite(tex, path, 32f, new Vector2(0.5f, 0.96f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateCrateDisguiseSprite()
        {
            const string path = "Assets/Tiles/Textures/CrateDisguise.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
                return RefreshCrateDisguiseSprite();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateProjectileSprite()
        {
            const string path = "Assets/Tiles/Textures/LabutitHand.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
            {
                var tex = new Texture2D(12, 12, TextureFormat.RGBA32, false);
                Color clear = new Color(0f, 0f, 0f, 0f);
                Color dark = new Color(0.18f, 0.18f, 0.22f, 1f);
                Color accent = new Color(0.95f, 0.55f, 0.15f, 1f);
                for (int y = 0; y < 12; y++)
                    for (int x = 0; x < 12; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(5.5f, 5.5f));
                        tex.SetPixel(x, y, d <= 2.5f ? accent : (d <= 5f ? dark : clear));
                    }
                SaveTextureAsSprite(tex, path, 16f);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SaveTextureAsSprite(Texture2D tex, string path, float ppu, Vector2? customPivot = null)
        {
            tex.Apply();
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
                if (customPivot.HasValue)
                {
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = customPivot.Value;
                    importer.SetTextureSettings(settings);
                }
                importer.SaveAndReimport();
            }
        }

        private static Sprite GetFirstSprite(string sheetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().FirstOrDefault();
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder(EnemyAnimFolder))
                AssetDatabase.CreateFolder("Assets/Animations", "Enemies");
            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
                AssetDatabase.CreateFolder("Assets", "Tiles");
            if (!AssetDatabase.IsValidFolder("Assets/Tiles/Textures"))
                AssetDatabase.CreateFolder("Assets/Tiles", "Textures");
        }

        private static int EnsureEnemyLayerExists()
        {
            int existing = LayerMask.NameToLayer("Enemy");
            if (existing != -1)
                return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            for (int i = 6; i < layersProp.arraySize; i++)
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = "Enemy";
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }
            return 0;
        }
    }
}

