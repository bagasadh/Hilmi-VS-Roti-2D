using HilmiVSRoti.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HilmiVSRoti.EditorTools
{
    /// <summary>
    /// Tool Editor untuk Tahap 2:
    /// Menambahkan Rigidbody2D, CapsuleCollider2D, GroundCheck, dan PlayerController ke Hilmi,
    /// serta membuat lantai uji sementara agar gerakan & lompatan langsung bisa dites.
    /// </summary>
    public static class PlayerSetupEditor
    {
        private const string GroundLayerName = "Ground";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Tools/Hilmi VS Roti/2. Setup Player Hilmi & Lantai Uji")]
        public static void SetupTahap2()
        {
            int groundLayer = EnsureLayerExists(GroundLayerName);
            SetupHilmiPlayer(groundLayer);
            SetupTestEnvironment(groundLayer);

            Debug.Log("[Hilmi VS Roti] Tahap 2 selesai: PlayerController Hilmi & lantai uji siap dites! Tekan Play untuk mencoba.");
        }

        private static void SetupHilmiPlayer(int groundLayer)
        {
            GameObject hilmi = GameObject.Find("Hilmi");
            if (hilmi == null)
            {
                Debug.LogError("[Hilmi VS Roti] GameObject 'Hilmi' tidak ditemukan! Jalankan Tahap 1 terlebih dahulu.");
                return;
            }

            hilmi.tag = "Player";
            hilmi.transform.position = new Vector3(0f, 1.5f, 0f);

            // Rigidbody2D
            var rb = hilmi.GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = hilmi.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // CapsuleCollider2D
            var col = hilmi.GetComponent<CapsuleCollider2D>();
            if (col == null)
                col = hilmi.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.offset = new Vector2(0f, -0.30f);
            col.size = new Vector2(0.55f, 0.90f);

            // GroundCheck child object tepat di bawah kaki Hilmi
            Transform groundCheck = hilmi.transform.Find("GroundCheck");
            if (groundCheck == null)
            {
                var gcObj = new GameObject("GroundCheck");
                gcObj.transform.SetParent(hilmi.transform, false);
                groundCheck = gcObj.transform;
            }
            groundCheck.localPosition = new Vector3(0f, -0.775f, 0f);

            // PlayerController
            var controller = hilmi.GetComponent<PlayerController>();
            if (controller == null)
                controller = hilmi.AddComponent<PlayerController>();

            // Assign SerializedFields lewat SerializedObject agar tersimpan rapi di Inspector
            var so = new SerializedObject(controller);
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            so.FindProperty("inputActions").objectReferenceValue = inputAsset;
            so.FindProperty("groundCheck").objectReferenceValue = groundCheck;
            so.FindProperty("groundLayer").intValue = 1 << groundLayer;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(hilmi);
            Selection.activeGameObject = hilmi;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        private static void SetupTestEnvironment(int groundLayer)
        {
            Sprite squareSprite = CreateSquareSprite();

            CreateOrUpdateTestBox(
                name: "TestFloor",
                position: new Vector3(0f, -2f, 0f),
                scale: new Vector3(20f, 1f, 1f),
                color: new Color(0.28f, 0.55f, 0.30f, 1f),
                layer: groundLayer,
                sprite: squareSprite);

            CreateOrUpdateTestBox(
                name: "TestPlatform",
                position: new Vector3(4.5f, 0.5f, 0f),
                scale: new Vector3(4f, 0.6f, 1f),
                color: new Color(0.55f, 0.40f, 0.25f, 1f),
                layer: groundLayer,
                sprite: squareSprite);
        }

        private static void CreateOrUpdateTestBox(string name, Vector3 position, Vector3 scale, Color color, int layer, Sprite sprite)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
                obj = new GameObject(name);

            obj.layer = layer;
            obj.transform.position = position;
            obj.transform.localScale = scale;

            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;

            if (obj.GetComponent<BoxCollider2D>() == null)
                obj.AddComponent<BoxCollider2D>();

            EditorUtility.SetDirty(obj);
        }

        private static Sprite CreateSquareSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }

        private static int EnsureLayerExists(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing != -1)
                return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            // Layer 0..5 adalah built-in Unity, mulai cari slot kosong dari index 6
            for (int i = 6; i < layersProp.arraySize; i++)
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }

            Debug.LogWarning($"[Hilmi VS Roti] Tidak ada slot Layer kosong untuk '{layerName}'.");
            return 0;
        }
    }
}

