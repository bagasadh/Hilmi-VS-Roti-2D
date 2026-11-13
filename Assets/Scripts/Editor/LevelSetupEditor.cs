using System.IO;
using HilmiVSRoti.CameraSystem;
using HilmiVSRoti.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HilmiVSRoti.EditorTools
{
    /// <summary>
    /// Tool Editor untuk Tahap 3:
    /// Membuat asset Tile (rumput, tanah, bata, duri), membangun layout Tilemap dasar
    /// sepanjang stage (±2 menit), platform bergerak, balok jatuh, death zone, dan kamera follow.
    /// </summary>
    public static class LevelSetupEditor
    {
        private const string TilesFolder = "Assets/Tiles";
        private const string TileTexturesFolder = "Assets/Tiles/Textures";

        [MenuItem("Tools/Hilmi VS Roti/3. Setup Level Tilemap, Rintangan & Kamera")]
        public static void SetupTahap3()
        {
            EnsureFolders();

            Tile grassTile = CreateOrLoadTile("GrassTopTile", CreateGrassTexture());
            Tile dirtTile = CreateOrLoadTile("DirtTile", CreateDirtTexture());
            Tile brickTile = CreateOrLoadTile("BrickTile", CreateBrickTexture());
            Tile spikeTile = CreateOrLoadTile("SpikeTile", CreateSpikeTexture());

            // Hapus objek uji sementara dari Tahap 2
            DestroyIfExists("TestFloor");
            DestroyIfExists("TestPlatform");

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1) groundLayer = 0;

            // Buat Grid & Tilemaps
            DestroyIfExists("LevelGrid");
            var gridObj = new GameObject("LevelGrid");
            gridObj.AddComponent<Grid>();

            Tilemap groundMap = CreateTilemapChild(gridObj.transform, "GroundTilemap", groundLayer, isTrigger: false, sortingOrder: 0);
            Tilemap spikeMap = CreateTilemapChild(gridObj.transform, "SpikeTilemap", 0, isTrigger: true, sortingOrder: 2);
            spikeMap.transform.position = new Vector3(0f, -0.35f, 0f);

            var spikeHazard = spikeMap.gameObject.AddComponent<Hazard>();
            var spikeSo = new SerializedObject(spikeHazard);
            spikeSo.FindProperty("damage").intValue = 1;
            spikeSo.FindProperty("instantKill").boolValue = false;
            spikeSo.ApplyModifiedProperties();

            BuildStageLayout(groundMap, spikeMap, grassTile, dirtTile, brickTile, spikeTile);

            // Rintangan Dinamis (Moving Platform & Falling Blocks)
            DestroyIfExists("DynamicObstacles");
            var obstaclesRoot = new GameObject("DynamicObstacles");
            CreateDynamicObstacles(obstaclesRoot.transform, groundLayer, brickTile.sprite);

            // Jurang / Death Zone
            CreateDeathZone();

            // Kamera Follow
            SetupCamera();

            // Posisikan Hilmi di titik start & pastikan jumpForce terbaru terpasang
            GameObject hilmi = GameObject.Find("Hilmi");
            if (hilmi != null)
            {
                hilmi.transform.SetParent(null);
                hilmi.transform.position = new Vector3(0f, 1.5f, 0f);
                var hilmiSr = hilmi.GetComponent<SpriteRenderer>();
                if (hilmiSr != null)
                    hilmiSr.sortingOrder = 5;
                var pc = hilmi.GetComponent<HilmiVSRoti.Player.PlayerController>();
                if (pc != null)
                {
                    var pcSo = new SerializedObject(pc);
                    pcSo.FindProperty("jumpForce").floatValue = 16.5f;
                    pcSo.ApplyModifiedProperties();
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Hilmi VS Roti] Tahap 3 selesai: Level Tilemap, duri, platform bergerak, balok jatuh, dan kamera follow siap dites!");
        }

        private static void BuildStageLayout(Tilemap ground, Tilemap spike, Tile grass, Tile dirt, Tile brick, Tile spikeTile)
        {
            // 0:00 - 0:20 Area Pembuka (x = -16..25, diperluas ke kiri agar ujung kamera penuh rumput)
            FillGround(ground, grass, dirt, -16, 12, -2, -6);
            FillRect(ground, brick, 5, 7, 1, 1);
            // Jurang kecil pertama (x = 13..15)
            FillGround(ground, grass, dirt, 16, 25, -2, -6);

            // 0:20 - 0:50 Area jek & Jurang + Duri (x = 26..58)
            FillGround(ground, grass, dirt, 26, 40, -2, -6);
            FillRect(spike, spikeTile, 31, 32, -1, -1); // Duri di tanah
            FillRect(ground, brick, 34, 37, 1, 1);
            // Jurang lebar (x = 41..47) -> dilewati pakai MovingPlatform naik-turun / horizontal
            FillGround(ground, grass, dirt, 48, 58, -2, -6);

            // 0:50 - 1:20 Area labutit & KenyGK + Balok Jatuh (x = 59..90)
            FillGround(ground, grass, dirt, 59, 68, -2, -6);
            FillRect(ground, brick, 63, 66, 1, 1);
            // Jurang jembatan balok jatuh (x = 69..78)
            FillGround(ground, grass, dirt, 79, 90, -2, -6);
            FillRect(spike, spikeTile, 83, 84, -1, -1);

            // 1:20 - 1:40 Area Ujian Campuran (x = 91..120)
            FillGround(ground, grass, dirt, 91, 98, -2, -6);
            FillRect(spike, spikeTile, 95, 96, -1, -1);
            // Jurang kombinasi (x = 99..112) -> platform bergerak + balok jatuh
            FillGround(ground, grass, dirt, 113, 120, -2, -6);

            // 1:40 - 2:00 Arena Final Boss & Finish (x = 121..162, diperluas ke kanan agar ujung kamera penuh rumput)
            FillGround(ground, grass, dirt, 121, 162, -2, -6);
            FillRect(ground, brick, 127, 130, 1, 1);
            FillRect(ground, brick, 137, 140, 1, 1);
        }

        private static void CreateDynamicObstacles(Transform parent, int groundLayer, Sprite blockSprite)
        {
            // Platform bergerak naik-turun di area 0:20-0:50
            CreateMovingPlatform("MovingPlatform_1", parent, new Vector3(44.5f, -1.5f, 0f), new Vector3(3f, 0.6f, 1f),
                new Vector3(0f, 2.5f, 0f), 2.5f, groundLayer, blockSprite);

            // Jembatan balok jatuh di area 0:50-1:20
            for (int i = 0; i < 3; i++)
            {
                CreateFallingBlock($"FallingBlock_{i + 1}", parent, new Vector3(70.5f + i * 3f, -1.35f, 0f),
                    new Vector3(2f, 0.7f, 1f), groundLayer, blockSprite);
            }

            // Area Ujian: kombinasi platform bergerak + balok jatuh
            CreateMovingPlatform("MovingPlatform_Test1", parent, new Vector3(101.5f, -1.2f, 0f), new Vector3(2.8f, 0.6f, 1f),
                new Vector3(3.5f, 0f, 0f), 2.8f, groundLayer, blockSprite);

            CreateFallingBlock("FallingBlock_Test1", parent, new Vector3(107.5f, -0.5f, 0f),
                new Vector3(2f, 0.7f, 1f), groundLayer, blockSprite);

            CreateMovingPlatform("MovingPlatform_Test2", parent, new Vector3(110.5f, -1.5f, 0f), new Vector3(2.5f, 0.6f, 1f),
                new Vector3(0f, 2.5f, 0f), 3.0f, groundLayer, blockSprite);
        }

        private static void CreateMovingPlatform(string name, Transform parent, Vector3 pos, Vector3 scale, Vector3 offset, float speed, int layer, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.layer = layer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.35f, 0.75f, 0.90f, 1f);

            go.AddComponent<BoxCollider2D>();
            var mp = go.AddComponent<MovingPlatform>();
            var so = new SerializedObject(mp);
            so.FindProperty("moveOffset").vector3Value = offset;
            so.FindProperty("speed").floatValue = speed;
            so.ApplyModifiedProperties();
        }

        private static void CreateFallingBlock(string name, Transform parent, Vector3 pos, Vector3 scale, int layer, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.layer = layer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.90f, 0.65f, 0.30f, 1f);

            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<FallingBlock>();
        }

        private static void CreateDeathZone()
        {
            DestroyIfExists("DeathZone");
            var dz = new GameObject("DeathZone");
            dz.transform.position = new Vector3(72f, -9f, 0f);
            var col = dz.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(180f, 3f);

            var hazard = dz.AddComponent<Hazard>();
            var so = new SerializedObject(hazard);
            so.FindProperty("instantKill").boolValue = true;
            so.ApplyModifiedProperties();
        }

        private static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            var follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<CameraFollow2D>();

            GameObject hilmi = GameObject.Find("Hilmi");
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = hilmi != null ? hilmi.transform : null;
            so.ApplyModifiedProperties();
        }

        private static Tilemap CreateTilemapChild(Transform parent, string name, int layer, bool isTrigger, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;

            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sortingOrder;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var tmc = go.AddComponent<TilemapCollider2D>();
            tmc.compositeOperation = Collider2D.CompositeOperation.Merge;

            var comp = go.AddComponent<CompositeCollider2D>();
            comp.isTrigger = isTrigger;

            return tm;
        }

        private static void FillGround(Tilemap map, Tile topTile, Tile fillTile, int xMin, int xMax, int yTop, int yBottom)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                map.SetTile(new Vector3Int(x, yTop, 0), topTile);
                for (int y = yTop - 1; y >= yBottom; y--)
                    map.SetTile(new Vector3Int(x, y, 0), fillTile);
            }
        }

        private static void FillRect(Tilemap map, Tile tile, int xMin, int xMax, int yMin, int yMax)
        {
            for (int x = xMin; x <= xMax; x++)
                for (int y = yMin; y <= yMax; y++)
                    map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(TilesFolder))
                AssetDatabase.CreateFolder("Assets", "Tiles");
            if (!AssetDatabase.IsValidFolder(TileTexturesFolder))
                AssetDatabase.CreateFolder(TilesFolder, "Textures");
        }

        public static void RefreshTileTextures()
        {
            CreateOrLoadTile("GrassTopTile", CreateGrassTexture());
            CreateOrLoadTile("DirtTile", CreateDirtTexture());
            CreateOrLoadTile("BrickTile", CreateBrickTexture());
            CreateOrLoadTile("SpikeTile", CreateSpikeTexture());
            ApplyTilemapEdgeOutlines();
        }

        public static void ApplyTilemapEdgeOutlines()
        {
            GameObject groundObj = GameObject.Find("GroundTilemap");
            if (groundObj == null)
                return;

            Tilemap groundMap = groundObj.GetComponent<Tilemap>();
            if (groundMap == null)
                return;

            Tile grassCenter = CreateOrLoadTile("GrassTopTile", CreateGrassTexture());
            Tile grassL = CreateEdgeTile("GrassTopTile_EdgeL", CreateGrassTexture(), true, false, true);
            Tile grassR = CreateEdgeTile("GrassTopTile_EdgeR", CreateGrassTexture(), false, true, true);
            Tile grassLR = CreateEdgeTile("GrassTopTile_EdgeLR", CreateGrassTexture(), true, true, true);

            Tile dirtCenter = CreateOrLoadTile("DirtTile", CreateDirtTexture());
            Tile dirtL = CreateEdgeTile("DirtTile_EdgeL", CreateDirtTexture(), true, false, false);
            Tile dirtR = CreateEdgeTile("DirtTile_EdgeR", CreateDirtTexture(), false, true, false);
            Tile dirtLR = CreateEdgeTile("DirtTile_EdgeLR", CreateDirtTexture(), true, true, false);

            Tile brickCenter = CreateOrLoadTile("BrickTile", CreateBrickTexture());
            Tile brickL = CreateEdgeTile("BrickTile_EdgeL", CreateBrickTexture(), true, false, true);
            Tile brickR = CreateEdgeTile("BrickTile_EdgeR", CreateBrickTexture(), false, true, true);
            Tile brickLR = CreateEdgeTile("BrickTile_EdgeLR", CreateBrickTexture(), true, true, true);

            BoundsInt bounds = groundMap.cellBounds;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    TileBase current = groundMap.GetTile(pos);
                    if (current == null)
                        continue;

                    bool exposedL = groundMap.GetTile(pos + Vector3Int.left) == null;
                    bool exposedR = groundMap.GetTile(pos + Vector3Int.right) == null;

                    string tName = current.name;
                    if (tName.StartsWith("GrassTopTile"))
                        groundMap.SetTile(pos, PickEdgeVariant(exposedL, exposedR, grassCenter, grassL, grassR, grassLR));
                    else if (tName.StartsWith("DirtTile"))
                        groundMap.SetTile(pos, PickEdgeVariant(exposedL, exposedR, dirtCenter, dirtL, dirtR, dirtLR));
                    else if (tName.StartsWith("BrickTile"))
                        groundMap.SetTile(pos, PickEdgeVariant(exposedL, exposedR, brickCenter, brickL, brickR, brickLR));
                }
            }

            groundMap.RefreshAllTiles();
            EditorUtility.SetDirty(groundMap);
        }

        private static Tile PickEdgeVariant(bool exposedL, bool exposedR, Tile center, Tile left, Tile right, Tile both)
        {
            if (exposedL && exposedR) return both;
            if (exposedL) return left;
            if (exposedR) return right;
            return center;
        }

        private static Tile CreateEdgeTile(string tileName, Texture2D tex, bool leftEdge, bool rightEdge, bool roundTopCorners)
        {
            Color outline = new Color(0.05f, 0.04f, 0.08f, 1f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < 32; y++)
            {
                if (leftEdge)
                {
                    if (roundTopCorners && y == 31)
                    {
                        tex.SetPixel(0, 31, clear);
                        tex.SetPixel(1, 31, outline);
                        tex.SetPixel(0, 30, outline);
                    }
                    else
                    {
                        tex.SetPixel(0, y, outline);
                    }
                }

                if (rightEdge)
                {
                    if (roundTopCorners && y == 31)
                    {
                        tex.SetPixel(31, 31, clear);
                        tex.SetPixel(30, 31, outline);
                        tex.SetPixel(31, 30, outline);
                    }
                    else
                    {
                        tex.SetPixel(31, y, outline);
                    }
                }
            }

            tex.Apply();
            return CreateOrLoadTile(tileName, tex);
        }

        private static Tile CreateOrLoadTile(string tileName, Texture2D tex)
        {
            string texPath = $"{TileTexturesFolder}/{tileName}.png";
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            string tilePath = $"{TilesFolder}/{tileName}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Texture2D CreateGrassTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color grassOutline = new Color(0.05f, 0.10f, 0.07f, 1f);
            Color grassLight = new Color(0.36f, 0.80f, 0.34f, 1f);
            Color grass = new Color(0.24f, 0.66f, 0.26f, 1f);
            Color grassDark = new Color(0.15f, 0.48f, 0.19f, 1f);
            Color dirt = new Color(0.45f, 0.29f, 0.17f, 1f);
            Color dirtDark = new Color(0.35f, 0.22f, 0.13f, 1f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int edgeOffset = ((x * 5 + 3) % 4 == 0) ? 5 : ((x * 3) % 5 == 0 ? 7 : 6);
                    if (y == 31 || y == edgeOffset)
                        tex.SetPixel(x, y, grassOutline);
                    else if (y >= 29)
                        tex.SetPixel(x, y, (x + y) % 4 == 0 ? grass : grassLight);
                    else if (y > edgeOffset)
                        tex.SetPixel(x, y, (x * 7 + y * 11) % 6 == 0 ? grassDark : ((x + y) % 5 == 0 ? grassLight : grass));
                    else
                        tex.SetPixel(x, y, (x * 5 + y * 9) % 7 == 0 ? dirtDark : dirt);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateDirtTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color dirt = new Color(0.45f, 0.29f, 0.17f, 1f);
            Color dirtLight = new Color(0.53f, 0.35f, 0.21f, 1f);
            Color speck = new Color(0.33f, 0.20f, 0.12f, 1f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int h = (x * 13 + y * 29) % 17;
                    if (h == 0 || h == 1)
                        tex.SetPixel(x, y, speck);
                    else if (h == 7)
                        tex.SetPixel(x, y, dirtLight);
                    else
                        tex.SetPixel(x, y, dirt);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBrickTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color brickTop = new Color(0.86f, 0.52f, 0.32f, 1f);
            Color brickTopLight = new Color(0.94f, 0.63f, 0.42f, 1f);
            Color mortarTop = new Color(0.16f, 0.09f, 0.07f, 1f);
            Color brick = new Color(0.68f, 0.34f, 0.19f, 1f);
            Color brickDark = new Color(0.56f, 0.27f, 0.15f, 1f);
            Color mortar = new Color(0.10f, 0.06f, 0.05f, 1f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (y >= 24)
                    {
                        bool isTopMortar = (y == 31) || (x == 16);
                        tex.SetPixel(x, y, isTopMortar ? mortarTop : ((x + y) % 7 == 0 ? brickTopLight : brickTop));
                    }
                    else if (y == 22 || y == 23)
                    {
                        tex.SetPixel(x, y, brickTopLight);
                    }
                    else if (y == 21)
                    {
                        tex.SetPixel(x, y, mortar);
                    }
                    else
                    {
                        bool isHorizMortar = y == 0 || y == 10;
                        bool isVertMortar = (y < 10 && x == 0) || (y >= 10 && x == 16);
                        if (isHorizMortar || isVertMortar)
                            tex.SetPixel(x, y, mortar);
                        else
                            tex.SetPixel(x, y, (y == 1 || y == 11) ? brickDark : brick);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSpikeTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color(0.05f, 0.04f, 0.08f, 1f);
            Color metalLight = new Color(0.92f, 0.94f, 0.98f, 1f);
            Color metal = new Color(0.70f, 0.74f, 0.80f, 1f);
            Color tip = new Color(0.94f, 0.24f, 0.24f, 1f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int localX = x % 16;
                    int dist = Mathf.Abs(localX - 7);
                    int height = 27 - dist * 3;
                    if (y > height)
                    {
                        tex.SetPixel(x, y, clear);
                    }
                    else if (y == 0 || y >= height - 1 || localX == 0 || localX == 15)
                    {
                        tex.SetPixel(x, y, outline);
                    }
                    else if (y >= height - 5)
                    {
                        tex.SetPixel(x, y, tip);
                    }
                    else
                    {
                        tex.SetPixel(x, y, localX < 7 ? metalLight : metal);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static void DestroyIfExists(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
                Object.DestroyImmediate(obj);
        }
    }
}

