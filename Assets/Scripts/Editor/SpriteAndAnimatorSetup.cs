using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace HilmiVSRoti.EditorTools
{
    /// <summary>
    /// Tool Editor untuk Tahap 1:
    /// 1) Mengatur import settings & slicing semua sprite sheet (256x256, 8 frame per sheet).
    /// 2) Membuat AnimationClip (Idle, Run, Jump) dan AnimatorController untuk Hilmi.
    /// 3) Menyiapkan GameObject preview Hilmi di scene aktif.
    /// </summary>
    public static class SpriteAndAnimatorSetup
    {
        private const int FrameWidth = 256;
        private const int FrameHeight = 256;
        private const float PixelsPerUnit = 100f;

        private const string SpritesRoot = "Assets/Sprites";
        private const string HilmiAnimFolder = "Assets/Animations/Hilmi";
        private const string HilmiControllerPath = "Assets/Animations/Hilmi/HilmiAnimator.controller";

        [MenuItem("Tools/Hilmi VS Roti/1. Setup Sprites & Hilmi Animator")]
        public static void SetupTahap1()
        {
            SliceAllSpriteSheets();
            AnimatorController controller = CreateHilmiAnimator();
            SetupHilmiPreviewInScene(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Hilmi VS Roti] Tahap 1 selesai: Semua sprite sheet sudah di-slice dan HilmiAnimator siap diuji!");
        }

        private static void SliceAllSpriteSheets()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesRoot });
            var factory = new SpriteDataProviderFactories();
            factory.Init();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("_sheet.png"))
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                // Hilmi bergaya pixel art -> Point; karakter lain render 3D halus -> Bilinear
                bool isHilmi = path.Contains("/Hilmi/");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.filterMode = isHilmi ? FilterMode.Point : FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;

                // Baca dimensi asli tekstur untuk menentukan jumlah kolom
                importer.SaveAndReimport();
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null)
                    continue;

                int cols = Mathf.Max(1, tex.width / FrameWidth);
                string baseName = Path.GetFileNameWithoutExtension(path);

                var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
                dataProvider.InitSpriteEditorDataProvider();

                var rects = new List<SpriteRect>(cols);
                for (int i = 0; i < cols; i++)
                {
                    rects.Add(new SpriteRect
                    {
                        name = $"{baseName}_{i}",
                        rect = new Rect(i * FrameWidth, 0, FrameWidth, FrameHeight),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        spriteID = GUID.Generate()
                    });
                }

                dataProvider.SetSpriteRects(rects.ToArray());
                dataProvider.Apply();
                importer.SaveAndReimport();
            }
        }

        private static AnimatorController CreateHilmiAnimator()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder(HilmiAnimFolder))
                AssetDatabase.CreateFolder("Assets/Animations", "Hilmi");

            AnimationClip idleClip = CreateSpriteAnimationClip(
                "Assets/Sprites/Hilmi/Hilmi_Idle_sheet.png",
                $"{HilmiAnimFolder}/Hilmi_Idle.anim",
                frameRate: 8f,
                loop: true);

            AnimationClip runClip = CreateSpriteAnimationClip(
                "Assets/Sprites/Hilmi/Hilmi_Run_sheet.png",
                $"{HilmiAnimFolder}/Hilmi_Run.anim",
                frameRate: 12f,
                loop: true);

            AnimationClip jumpClip = CreateSpriteAnimationClip(
                "Assets/Sprites/Hilmi/Hilmi_Jump_sheet.png",
                $"{HilmiAnimFolder}/Hilmi_Jump.anim",
                frameRate: 10f,
                loop: false);

            // Hapus controller lama jika ada agar selalu bersih
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(HilmiControllerPath) != null)
                AssetDatabase.DeleteAsset(HilmiControllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(HilmiControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);

            // Default IsGrounded = true saat preview
            var parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == "IsGrounded")
                    parameters[i].defaultBool = true;
            }
            controller.parameters = parameters;

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idleClip;
            sm.defaultState = idleState;

            AnimatorState runState = sm.AddState("Run");
            runState.motion = runClip;

            AnimatorState jumpState = sm.AddState("Jump");
            jumpState.motion = jumpClip;

            // Idle <-> Run
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.05f;
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.05f;
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // AnyState -> Jump saat tidak di tanah
            var anyToJump = sm.AddAnyStateTransition(jumpState);
            anyToJump.hasExitTime = false;
            anyToJump.canTransitionToSelf = false;
            anyToJump.duration = 0.05f;
            anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");

            // Jump -> Idle / Run saat mendarat
            var jumpToIdle = jumpState.AddTransition(idleState);
            jumpToIdle.hasExitTime = false;
            jumpToIdle.duration = 0.05f;
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            jumpToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            var jumpToRun = jumpState.AddTransition(runState);
            jumpToRun.hasExitTime = false;
            jumpToRun.duration = 0.05f;
            jumpToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            jumpToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip CreateSpriteAnimationClip(string sheetPath, string clipPath, float frameRate, bool loop)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .OrderBy(s =>
                {
                    // Urutkan berdasarkan indeks angka di akhir nama sprite
                    string[] parts = s.name.Split('_');
                    return int.TryParse(parts[^1], out int idx) ? idx : 0;
                })
                .ToArray();

            var clip = new AnimationClip { frameRate = frameRate };
            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
                AssetDatabase.DeleteAsset(clipPath);

            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static void SetupHilmiPreviewInScene(AnimatorController controller)
        {
            GameObject hilmi = GameObject.Find("Hilmi");
            if (hilmi == null)
                hilmi = new GameObject("Hilmi");

            var sr = hilmi.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = hilmi.AddComponent<SpriteRenderer>();

            Sprite[] idleSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Hilmi/Hilmi_Idle_sheet.png")
                .OfType<Sprite>()
                .ToArray();
            if (idleSprites.Length > 0)
                sr.sprite = idleSprites[0];

            var anim = hilmi.GetComponent<Animator>();
            if (anim == null)
                anim = hilmi.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            Selection.activeGameObject = hilmi;
        }
    }
}

