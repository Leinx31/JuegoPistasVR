using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using CrimeVR.Managers;
using System.Collections.Generic;

namespace CrimeVR.Editor
{
    public static class ExpoMapImporter
    {
        private const string ExpoMapPath = "Assets/Models/Environment/ExpoMap/Mapa expo.fbx";
        private const string ExpoScenePath = "Assets/Scenes/ExpoMap_Exploration.unity";
        private const string QuestCleanScenePath = "Assets/Scenes/Quest_Clean_Test.unity";
        private const string MaterialsRoot = "Assets/Models/Environment/ExpoMap/GeneratedMaterials";

        [MenuItem("Tools/Crime VR/Reimport Expo Map With Textures")]
        public static void ReimportExpoMapWithTextures()
        {
            EnsureExpoMaterials();

            ModelImporter importer = AssetImporter.GetAtPath(ExpoMapPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"No se encontro el importador del mapa en {ExpoMapPath}");
                return;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.Everywhere;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            ApplyModelMaterialRemaps(importer);
            importer.SaveAndReimport();

            AssetDatabase.Refresh();
            Debug.Log("Expo Map reimportado con materiales externos URP.");
        }

        [MenuItem("Tools/Crime VR/Build Expo Map Exploration Scene")]
        public static void BuildExpoMapExplorationScene()
        {
            ReimportExpoMapWithTextures();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ExpoMap_Exploration";

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.54f, 0.58f, 0.62f);

            GameObject rig = CreatePlayerRig(scene);
            CreateSystems(scene, rig);
            CreateXRDeviceSimulator(scene);
            EnableDesktopRigComponents(scene);

            CreateLighting(scene);
            CreateEnvironment(scene);
            ApplySceneMaterialAssignments();

            EditorSceneManager.SaveScene(scene, ExpoScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Escena ExpoMap_Exploration creada correctamente.");
        }

        [MenuItem("Tools/Crime VR/Add Expo Map To Clean Quest Scene")]
        public static void AddExpoMapToCleanQuestScene()
        {
            ReimportExpoMapWithTextures();

            Scene scene = EditorSceneManager.OpenScene(QuestCleanScenePath, OpenSceneMode.Single);
            RemoveExistingExpoMap(scene);
            CreateEnvironment(scene);
            ApplySceneMaterialAssignments();

            EditorSceneManager.SaveScene(scene, QuestCleanScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Mapa expo agregado a Quest_Clean_Test correctamente.");
        }

        public static void ReimportExpoMapWithTexturesBatch()
        {
            ReimportExpoMapWithTextures();
            EditorApplication.Exit(0);
        }

        public static void BuildExpoMapExplorationSceneBatch()
        {
            BuildExpoMapExplorationScene();
            EditorApplication.Exit(0);
        }

        public static void AddExpoMapToCleanQuestSceneBatch()
        {
            AddExpoMapToCleanQuestScene();
            EditorApplication.Exit(0);
        }

        [MenuItem("Tools/Crime VR/Fix Expo Scene Material Assignments")]
        public static void FixExpoSceneMaterialAssignments()
        {
            Scene scene = EditorSceneManager.OpenScene(ExpoScenePath, OpenSceneMode.Single);
            ApplySceneMaterialAssignments();
            EditorSceneManager.SaveScene(scene, ExpoScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Materiales de ExpoMap_Exploration reasignados en escena.");
        }

        public static void FixExpoSceneMaterialAssignmentsBatch()
        {
            FixExpoSceneMaterialAssignments();
            EditorApplication.Exit(0);
        }

        private static void EnsureExpoMaterials()
        {
            EnsureFolder("Assets/Models");
            EnsureFolder("Assets/Models/Environment");
            EnsureFolder("Assets/Models/Environment/ExpoMap");
            EnsureFolder(MaterialsRoot);

            CreateMaterial("MAT_Expo_Asphalt", "Material",
                "Assets/Models/Environment/ExpoMap/Texturas/Asfalto/calle caca/Road009C_2K-JPG_Color.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/Asfalto/calle caca/Road009C_2K-JPG_NormalGL.jpg",
                null, 0f, 0.18f);

            CreateMaterial("MAT_Expo_YellowPlastic", "Material.002",
                "Assets/Models/Environment/ExpoMap/Texturas/coitos amarillos/Plastic016A_2K-JPG_Color.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/coitos amarillos/Plastic016A_2K-JPG_NormalGL.jpg",
                null, 0f, 0.35f);

            CreateMaterial("MAT_Expo_Metal", "Material.010",
                "Assets/Models/Environment/ExpoMap/Texturas/metal/Metal062C_2K-JPG_Color.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/metal/Metal062C_2K-JPG_NormalGL.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/metal/Metal062C_2K-JPG_Metalness.jpg", 0.8f, 0.28f);

            CreateMaterial("MAT_Expo_RedPavers", "Material.009",
                "Assets/Models/Environment/ExpoMap/Texturas/ladrillos/acera .ladrillos/textures/brick_pavement_03_diff_2k.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/ladrillos/acera .ladrillos/textures/brick_pavement_03_nor_gl_2k.exr",
                null, 0f, 0.22f);

            CreateMaterial("MAT_Expo_Cobble", "Material.011",
                "Assets/Models/Environment/ExpoMap/Texturas/stones/cobble/PavingStones006_2K-JPG_Color.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/stones/cobble/PavingStones006_2K-JPG_NormalGL.jpg",
                null, 0f, 0.2f);

            CreateMaterial("MAT_Expo_PlasterBrick", "Material.008",
                "Assets/Models/Environment/ExpoMap/Texturas/ladrillos/mas larillos/textures/plaster_brick_pattern_diff_2k.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/ladrillos/mas larillos/textures/plaster_brick_pattern_nor_gl_2k.exr",
                null, 0f, 0.18f);

            CreateMaterial("MAT_Expo_Wood", "Material.004",
                "Assets/Models/Environment/ExpoMap/Texturas/Madera/Wood026_2K-JPG_Color.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/Madera/Wood026_2K-JPG_NormalGL.jpg",
                null, 0f, 0.28f);

            CreateMaterial("MAT_Expo_Concrete", "Material.006",
                "Assets/Models/Environment/ExpoMap/Texturas/pintura pelaa/textures/rebar_reinforced_concrete_diff_2k.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/pintura pelaa/textures/rebar_reinforced_concrete_nor_gl_2k.exr",
                null, 0f, 0.15f);

            CreateMaterial("MAT_Expo_RockWall", "Material.007",
                "Assets/Models/Environment/ExpoMap/Texturas/ladrillos/Pared de roca/rock_wall_08_diff_2k.jpg",
                "Assets/Models/Environment/ExpoMap/Texturas/ladrillos/Pared de roca/rock_wall_08_nor_gl_2k.exr",
                null, 0f, 0.14f);
        }

        private static void CreateMaterial(string assetName, string materialName, string baseMapPath, string normalMapPath, string metallicMapPath, float metallic, float smoothness)
        {
            string materialPath = $"{MaterialsRoot}/{assetName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.name = materialName;
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);

            Texture baseMap = LoadTexture(baseMapPath, false);
            Texture normalMap = LoadTexture(normalMapPath, true);
            Texture metallicMap = LoadTexture(metallicMapPath, false);

            if (baseMap != null)
                material.SetTexture("_BaseMap", baseMap);

            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", normalMap);
                material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_BumpScale", 1f);
            }

            if (metallicMap != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicMap);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            EditorUtility.SetDirty(material);
        }

        private static Texture LoadTexture(string texturePath, bool normalMap)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
                return null;

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null && normalMap && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
        }

        private static void CreateLighting(Scene scene)
        {
            GameObject lighting = new GameObject("Lighting");
            SceneManager.MoveGameObjectToScene(lighting, scene);

            GameObject directionalObject = new GameObject("Directional Light");
            directionalObject.transform.SetParent(lighting.transform, false);
            directionalObject.transform.rotation = Quaternion.Euler(44f, -30f, 0f);

            Light directional = directionalObject.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1f;
            directional.shadows = LightShadows.Soft;
        }

        private static void CreateEnvironment(Scene scene)
        {
            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExpoMapPath);
            if (mapPrefab == null)
                throw new FileNotFoundException("No se pudo cargar el FBX del mapa expo.", ExpoMapPath);

            GameObject mapInstance = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab, scene);
            mapInstance.name = "ExpoMap_Root";
            mapInstance.transform.SetParent(environmentRoot, false);
            mapInstance.transform.position = Vector3.zero;
            mapInstance.transform.rotation = Quaternion.identity;
            mapInstance.transform.localScale = Vector3.one;

            Bounds combinedBounds = new Bounds(mapInstance.transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (Renderer renderer in mapInstance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer)
                    continue;

                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }

                MeshCollider collider = renderer.GetComponent<MeshCollider>();
                if (collider == null)
                    collider = renderer.gameObject.AddComponent<MeshCollider>();

                collider.convex = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                string lowerName = renderer.gameObject.name.ToLowerInvariant();
                if (lowerName.Contains("floor") || lowerName.Contains("road") || lowerName.Contains("street") || lowerName.Contains("ground") || lowerName.Contains("plane"))
                {
                    if (renderer.GetComponent<TeleportationArea>() == null)
                        renderer.gameObject.AddComponent<TeleportationArea>();
                }
            }

            if (hasBounds)
            {
                CreateFallbackGround(environmentRoot, combinedBounds);
                GameObject playerRig = GameObject.Find("XR_PlayerRig");
                if (playerRig != null)
                    PlacePlayerAtExpoSpawn(playerRig.transform, combinedBounds);
            }
        }

        private static void CreateFallbackGround(Transform environmentRoot, Bounds bounds)
        {
            Transform existing = environmentRoot.Find("Expo_FallbackGround");
            GameObject ground = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Expo_FallbackGround";
            ground.transform.SetParent(environmentRoot, false);

            float groundY = bounds.min.y - 0.15f;
            float width = Mathf.Max(bounds.size.x + 6f, 20f);
            float depth = Mathf.Max(bounds.size.z + 6f, 20f);

            ground.transform.position = new Vector3(bounds.center.x, groundY, bounds.center.z);
            ground.transform.localScale = new Vector3(width, 0.2f, depth);

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            Collider collider = ground.GetComponent<Collider>();
            if (collider == null)
                collider = ground.AddComponent<BoxCollider>();

            collider.enabled = true;
        }

        private static void PlacePlayerAtExpoSpawn(Transform rigTransform, Bounds bounds)
        {
            Vector3 spawnPosition = new Vector3(bounds.center.x, bounds.min.y + 1.25f, bounds.center.z);

            if (Physics.Raycast(
                    new Vector3(bounds.center.x, bounds.max.y + 5f, bounds.center.z),
                    Vector3.down,
                    out RaycastHit hitInfo,
                    bounds.size.y + 12f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                spawnPosition = hitInfo.point + Vector3.up * 0.1f;
            }

            rigTransform.position = spawnPosition;
            rigTransform.rotation = Quaternion.identity;
        }

        private static void ApplySceneMaterialAssignments()
        {
            Dictionary<string, Material> materialMap = new Dictionary<string, Material>
            {
                { "Material", LoadGeneratedMaterial("MAT_Expo_Asphalt") },
                { "Material.002", LoadGeneratedMaterial("MAT_Expo_YellowPlastic") },
                { "Material.004", LoadGeneratedMaterial("MAT_Expo_Wood") },
                { "Material.006", LoadGeneratedMaterial("MAT_Expo_Concrete") },
                { "Material.007", LoadGeneratedMaterial("MAT_Expo_RockWall") },
                { "Material.009", LoadGeneratedMaterial("MAT_Expo_RedPavers") },
                { "Material.010", LoadGeneratedMaterial("MAT_Expo_Metal") },
                { "Material.011", LoadGeneratedMaterial("MAT_Expo_Cobble") },
                { "No Name", LoadGeneratedMaterial("MAT_Expo_Concrete") },
                { "Material.008", LoadGeneratedMaterial("MAT_Expo_PlasterBrick") }
            };

            GameObject expoRoot = GameObject.Find("ExpoMap_Root");
            if (expoRoot == null)
                return;

            foreach (Renderer renderer in expoRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material[] current = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < current.Length; i++)
                {
                    string currentName = current[i] != null ? current[i].name : string.Empty;
                    if (materialMap.TryGetValue(currentName, out Material mapped) && mapped != null && current[i] != mapped)
                    {
                        current[i] = mapped;
                        changed = true;
                    }
                }

                if (changed)
                {
                    renderer.sharedMaterials = current;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static Material LoadGeneratedMaterial(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsRoot}/{assetName}.mat");
        }

        private static void ApplyModelMaterialRemaps(AssetImporter importer)
        {
            if (importer == null)
                return;

            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material"));
            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.002"));
            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.004"));
            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.006"));
            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.007"));
            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.009"));
            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.010"));
            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.011"));
            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "No Name"));

            AddMaterialRemap(importer, "Material", "MAT_Expo_Asphalt");
            AddMaterialRemap(importer, "Material.002", "MAT_Expo_YellowPlastic");
            AddMaterialRemap(importer, "Material.004", "MAT_Expo_Wood");
            AddMaterialRemap(importer, "Material.006", "MAT_Expo_Concrete");
            AddMaterialRemap(importer, "Material.007", "MAT_Expo_RockWall");
            AddMaterialRemap(importer, "Material.009", "MAT_Expo_RedPavers");
            AddMaterialRemap(importer, "Material.010", "MAT_Expo_Metal");
            AddMaterialRemap(importer, "Material.011", "MAT_Expo_Cobble");
            AddMaterialRemap(importer, "No Name", "MAT_Expo_Concrete");

            EditorUtility.SetDirty(importer);
        }

        private static void AddMaterialRemap(AssetImporter importer, string sourceMaterialName, string generatedMaterialAssetName)
        {
            Material targetMaterial = LoadGeneratedMaterial(generatedMaterialAssetName);
            if (targetMaterial == null)
                return;

            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceMaterialName), targetMaterial);
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in existing)
            {
                if (scene.path == ExpoScenePath)
                    return;
            }

            EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[existing.Length + 1];
            for (int i = 0; i < existing.Length; i++)
                updated[i] = existing[i];

            updated[existing.Length] = new EditorBuildSettingsScene(ExpoScenePath, true);
            EditorBuildSettings.scenes = updated;
        }

        private static void RemoveExistingExpoMap(Scene scene)
        {
            GameObject existingRoot = GameObject.Find("ExpoMap_Root");
            if (existingRoot != null)
                Object.DestroyImmediate(existingRoot);

            GameObject existingFallbackGround = GameObject.Find("Expo_FallbackGround");
            if (existingFallbackGround != null)
                Object.DestroyImmediate(existingFallbackGround);

            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            for (int i = environmentRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = environmentRoot.GetChild(i);
                if (child.name.StartsWith("ExpoMap_", System.StringComparison.OrdinalIgnoreCase) ||
                    child.name.StartsWith("Expo_", System.StringComparison.OrdinalIgnoreCase))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Transform FindOrCreateRoot(Scene scene, string rootName)
        {
            GameObject root = GameObject.Find(rootName);
            if (root != null)
                return root.transform;

            root = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root.transform;
        }

        private static GameObject CreatePlayerRig(Scene scene)
        {
            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("CreatePlayerRig", BindingFlags.NonPublic | BindingFlags.Static);
            return (GameObject)method.Invoke(null, new object[] { scene });
        }

        private static void CreateSystems(Scene scene, GameObject rig)
        {
            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("CreateSystems", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { scene, rig });
        }

        private static void CreateXRDeviceSimulator(Scene scene)
        {
            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("CreateXRDeviceSimulator", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { scene });
        }

        private static void EnableDesktopRigComponents(Scene scene)
        {
            CrimeSceneSystemsRoot systemsRoot = Object.FindAnyObjectByType<CrimeSceneSystemsRoot>();
            if (systemsRoot == null)
                return;

            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("EnableDesktopRigComponents", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { scene, systemsRoot });
        }
    }
}
