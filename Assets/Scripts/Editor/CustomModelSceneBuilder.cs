using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CrimeVR.Managers;
using CrimeVR.Evidence;
using CrimeVR.UI;
using CrimeVR.Player;

namespace CrimeVR.Editor
{
    public class CustomModelSceneBuilder : EditorWindow
    {
        private GameObject customModelAsset;
        private string sceneName = "Caso_Investigacion_MiEscena";
        private string caseTitle = "Caso: Escena del Crimen Principal";
        private int requiredTrueClues = 3;
        private int maxAllowedFalseClues = 2;
        private bool addSampleClues = true;
        private bool addDetectiveNotebook = true;
        private bool enableDesktopDebugMode = true;
        private Vector3 customSpawnOffset = new Vector3(0f, 1.2f, 0f);

        [MenuItem("Tools/Crime VR/Create Scene from 3D Model Wizard...", false, 20)]
        public static void OpenWizard()
        {
            CustomModelSceneBuilder window = GetWindow<CustomModelSceneBuilder>("Crime VR Scene Builder");
            window.minSize = new Vector2(450, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Generador de Escena VR con Modelo 3D Personalizado", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Selecciona o arrastra tu modelo 3D (.blend, .fbx, .obj o prefab) para ensamblar automáticamente una escena completa de investigación VR con manos visibles, libreta, pistas y movimiento.", MessageType.Info);

            EditorGUILayout.Space(10);
            customModelAsset = (GameObject)EditorGUILayout.ObjectField("Modelo 3D del Entorno:", customModelAsset, typeof(GameObject), false);

            EditorGUILayout.Space(5);
            sceneName = EditorGUILayout.TextField("Nombre de la Escena:", sceneName);
            caseTitle = EditorGUILayout.TextField("Título del Caso:", caseTitle);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Configuración de Investigación", EditorStyles.boldLabel);
            requiredTrueClues = EditorGUILayout.IntSlider("Pistas Clave Requeridas:", requiredTrueClues, 1, 10);
            maxAllowedFalseClues = EditorGUILayout.IntSlider("Máx. Pistas Falsas Permitidas:", maxAllowedFalseClues, 0, 5);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Opciones de la Escena", EditorStyles.boldLabel);
            addSampleClues = EditorGUILayout.Toggle("Incluir Pistas de Prueba", addSampleClues);
            addDetectiveNotebook = EditorGUILayout.Toggle("Incluir Libreta del Detective", addDetectiveNotebook);
            enableDesktopDebugMode = EditorGUILayout.Toggle("Habilitar Modo Escritorio (WASD/Mouse)", enableDesktopDebugMode);

            EditorGUILayout.Space(15);
            GUI.backgroundColor = new Color(0.2f, 0.75f, 0.35f);
            if (GUILayout.Button("✨ Construir Escena Completa VR", GUILayout.Height(40)))
            {
                if (customModelAsset == null)
                {
                    EditorUtility.DisplayDialog("Error", "Por favor arrastra o selecciona un modelo 3D (.blend / .fbx) en el campo 'Modelo 3D del Entorno'.", "OK");
                    return;
                }

                BuildSceneWithModel(customModelAsset, sceneName, caseTitle, requiredTrueClues, maxAllowedFalseClues, addSampleClues, addDetectiveNotebook, enableDesktopDebugMode);
            }
            GUI.backgroundColor = Color.white;
        }

        public static void BuildSceneWithModel(
            GameObject modelPrefab,
            string targetSceneName,
            string caseTitleName,
            int requiredClues,
            int maxFalse,
            bool spawnClues,
            bool spawnNotebook,
            bool enableDesktop)
        {
            if (modelPrefab == null)
            {
                Debug.LogError("[CustomModelSceneBuilder] El modelo 3D especificado es nulo.");
                return;
            }

            if (string.IsNullOrWhiteSpace(targetSceneName))
                targetSceneName = "Caso_Investigacion_" + modelPrefab.name;

            string scenePath = $"Assets/Scenes/{targetSceneName}.unity";

            // Crear carpeta si no existe
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // Crear nueva escena limpia
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = targetSceneName;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);

            // 1. Instanciar Entorno y configurar colliders + teleport
            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, scene);
            modelInstance.name = "Environment_" + modelPrefab.name;
            modelInstance.transform.SetParent(environmentRoot, false);
            modelInstance.transform.position = Vector3.zero;
            modelInstance.transform.rotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            Bounds bounds = SetupEnvironmentCollidersAndTeleport(modelInstance);

            // 2. Crear Iluminación
            CreateLighting(scene);

            // 3. Crear Player Rig con manos visuales
            GameObject rig = CreatePlayerRig(scene);

            // 4. Crear Sistemas (CaseManager, Inventory, etc.)
            CrimeSceneSystemsRoot systemsRoot = CreateSystems(scene, rig);
            if (systemsRoot != null && systemsRoot.CaseManager != null)
            {
                systemsRoot.CaseManager.Configure(
                    "case." + targetSceneName.ToLowerInvariant(),
                    caseTitleName,
                    requiredClues,
                    maxFalse);
            }

            // 5. XR Simulator y Modo Escritorio
            CreateXRDeviceSimulator(scene);
            if (enableDesktop && systemsRoot != null)
            {
                EnableDesktopRigComponents(scene, systemsRoot);
            }

            // 6. Posicionar al jugador en el suelo del modelo
            PlacePlayerAtSpawn(rig.transform, bounds);

            // 7. Pistas y Libreta
            if (spawnClues)
            {
                EnsureClueAssets();
                CreateInvestigationClues(scene, rig);
            }

            if (spawnNotebook)
            {
                CreateDetectiveNotebook(scene, rig, caseTitleName, requiredClues, maxFalse);
            }

            // Reparar materiales para URP si vienen con shaders estándar o no asignados
            RepairSceneMaterialsForUrp(scene);

            // 8. Guardar escena y registrar en Build Settings
            EditorSceneManager.SaveScene(scene, scenePath);
            AddSceneToBuildSettings(scenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CustomModelSceneBuilder] ¡Escena '{targetSceneName}' construida con éxito en {scenePath}!");
            EditorUtility.DisplayDialog("Crime VR", $"¡Escena '{targetSceneName}' creada con éxito!\n\nEl modelo 3D, el Rig VR con manos, los sistemas de pistas y la libreta han sido integrados.", "OK");
        }

        private static Bounds SetupEnvironmentCollidersAndTeleport(GameObject environmentObject)
        {
            Bounds combinedBounds = new Bounds(environmentObject.transform.position, Vector3.zero);
            bool hasBounds = false;

            MeshFilter[] meshFilters = environmentObject.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter filter in meshFilters)
            {
                if (filter.sharedMesh == null)
                    continue;

                Renderer rend = filter.GetComponent<Renderer>();
                if (rend != null && !(rend is ParticleSystemRenderer))
                {
                    if (!hasBounds)
                    {
                        combinedBounds = rend.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(rend.bounds);
                    }
                }

                // Asignar MeshCollider asegurando explícitamente el sharedMesh
                MeshCollider meshCollider = filter.GetComponent<MeshCollider>();
                if (meshCollider == null)
                    meshCollider = filter.gameObject.AddComponent<MeshCollider>();

                meshCollider.sharedMesh = filter.sharedMesh;
                meshCollider.convex = false;

                // Asignar TeleportationArea a los suelos, calles y aceras
                string lowerName = filter.gameObject.name.ToLowerInvariant();
                if (lowerName.Contains("floor") || lowerName.Contains("ground") || lowerName.Contains("road") ||
                    lowerName.Contains("street") || lowerName.Contains("piso") || lowerName.Contains("suelo") ||
                    lowerName.Contains("calle") || lowerName.Contains("acera") || lowerName.Contains("plane") ||
                    lowerName.Contains("sidewalk") || lowerName.Contains("asfalto") || lowerName.Contains("pasto") ||
                    lowerName.Contains("terreno"))
                {
                    if (filter.GetComponent<TeleportationArea>() == null)
                        filter.gameObject.AddComponent<TeleportationArea>();
                }
            }

            // También verificar SkinnedMeshRenderers si existen
            SkinnedMeshRenderer[] skinnedRenderers = environmentObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer skinned in skinnedRenderers)
            {
                if (skinned.sharedMesh == null)
                    continue;

                if (!hasBounds)
                {
                    combinedBounds = skinned.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(skinned.bounds);
                }

                MeshCollider meshCollider = skinned.GetComponent<MeshCollider>();
                if (meshCollider == null)
                    meshCollider = skinned.gameObject.AddComponent<MeshCollider>();

                meshCollider.sharedMesh = skinned.sharedMesh;
                meshCollider.convex = false;
            }

            if (!hasBounds)
            {
                CreateFallbackGround(environmentObject.transform.parent, combinedBounds);
            }

            return combinedBounds;
        }

        private static void CreateFallbackGround(Transform environmentRoot, Bounds bounds)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Scene_Ground_Collider";
            ground.transform.SetParent(environmentRoot, false);

            float groundY = bounds.min.y - 0.05f;
            float width = Mathf.Max(bounds.size.x + 40f, 60f);
            float depth = Mathf.Max(bounds.size.z + 40f, 60f);

            ground.transform.position = new Vector3(bounds.center.x, groundY, bounds.center.z);
            ground.transform.localScale = new Vector3(width, 0.1f, depth);

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            if (ground.GetComponent<TeleportationArea>() == null)
                ground.AddComponent<TeleportationArea>();
        }

        private static void PlacePlayerAtSpawn(Transform rigTransform, Bounds bounds)
        {
            Vector3 testCenter = bounds.center;
            Vector3 spawnPosition = new Vector3(testCenter.x, bounds.min.y + 0.1f, testCenter.z);

            RaycastHit[] hits = Physics.RaycastAll(
                new Vector3(testCenter.x, bounds.max.y + 50f, testCenter.z),
                Vector3.down,
                bounds.size.y + 100f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            if (hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.point.y.CompareTo(b.point.y));
                spawnPosition = hits[0].point + Vector3.up * 0.05f;
            }

            rigTransform.position = spawnPosition;
            rigTransform.rotation = Quaternion.identity;
        }

        private static void CreateLighting(Scene scene)
        {
            GameObject lighting = new GameObject("Lighting");
            SceneManager.MoveGameObjectToScene(lighting, scene);

            GameObject dirLightObj = new GameObject("Directional Light");
            dirLightObj.transform.SetParent(lighting.transform, false);
            dirLightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            Light dirLight = dirLightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 1.1f;
            dirLight.color = new Color(0.98f, 0.96f, 0.92f);
            dirLight.shadows = LightShadows.Soft;
        }

        private static void EnsureClueAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Clues"))
                AssetDatabase.CreateFolder("Assets/Resources", "Clues");

            CreateOrLoadClue("Assets/Resources/Clues/Clue_Revolver.asset", "clue.revolver.001",
                "Revolver Calibre .38", "Arma homicida arrojada en la escena del crimen.", true, "Arma");

            CreateOrLoadClue("Assets/Resources/Clues/Clue_Letter.asset", "clue.note.001",
                "Nota Amenazante", "Mensaje manuscrito incriminatorio encontrado en el lugar.", true, "Documento");

            CreateOrLoadClue("Assets/Resources/Clues/Clue_Keycard.asset", "clue.keycard.001",
                "Tarjeta de Acceso VIP", "Credencial de seguridad que permite el ingreso a áreas restringidas.", true, "Acceso");

            CreateOrLoadClue("Assets/Resources/Clues/Clue_CoffeeCup.asset", "clue.coffee.001",
                "Vaso de Cafe Desechable", "Vaso abandonado de un transeúnte, sin relevancia para el caso.", false, "Distraccion");
        }

        private static ClueData CreateOrLoadClue(string assetPath, string id, string name, string desc, bool isTrue, string category)
        {
            ClueData data = AssetDatabase.LoadAssetAtPath<ClueData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ClueData>();
                data.Initialize(id, name, desc, isTrue, null, null, category);
                AssetDatabase.CreateAsset(data, assetPath);
            }
            return data;
        }

        private static void CreateInvestigationClues(Scene scene, GameObject rig)
        {
            Transform interactionRoot = FindOrCreateRoot(scene, "Interaction");
            Transform cluesRoot = FindOrCreateChild(interactionRoot, "CrimeClues");

            Vector3 spawnPos = rig != null ? rig.transform.position : Vector3.zero;

            ClueData revolverData = AssetDatabase.LoadAssetAtPath<ClueData>("Assets/Resources/Clues/Clue_Revolver.asset");
            ClueData letterData = AssetDatabase.LoadAssetAtPath<ClueData>("Assets/Resources/Clues/Clue_Letter.asset");
            ClueData keycardData = AssetDatabase.LoadAssetAtPath<ClueData>("Assets/Resources/Clues/Clue_Keycard.asset");
            ClueData cupData = AssetDatabase.LoadAssetAtPath<ClueData>("Assets/Resources/Clues/Clue_CoffeeCup.asset");

            SpawnPhysicalClue(cluesRoot, "Clue_Revolver_Object", spawnPos + new Vector3(1.2f, 0.8f, 1.8f), new Vector3(0.12f, 0.05f, 0.22f), new Color(0.2f, 0.2f, 0.22f), revolverData);
            SpawnPhysicalClue(cluesRoot, "Clue_ThreatLetter_Object", spawnPos + new Vector3(-1.4f, 0.75f, 2.0f), new Vector3(0.2f, 0.01f, 0.26f), new Color(0.9f, 0.85f, 0.75f), letterData);
            SpawnPhysicalClue(cluesRoot, "Clue_Keycard_Object", spawnPos + new Vector3(0.7f, 0.75f, 2.8f), new Vector3(0.08f, 0.01f, 0.14f), new Color(0.1f, 0.45f, 0.9f), keycardData);
            SpawnPhysicalClue(cluesRoot, "Clue_CoffeeCup_Object", spawnPos + new Vector3(-0.8f, 0.75f, 1.1f), new Vector3(0.1f, 0.14f, 0.1f), new Color(0.85f, 0.85f, 0.85f), cupData);
        }

        private static GameObject SpawnPhysicalClue(Transform parent, string objectName, Vector3 position, Vector3 scale, Color color, ClueData data)
        {
            GameObject clueObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clueObj.name = objectName;
            clueObj.transform.SetParent(parent, false);
            clueObj.transform.position = position;
            clueObj.transform.localScale = scale;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            clueObj.GetComponent<Renderer>().sharedMaterial = mat;

            Rigidbody rb = clueObj.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            XRGrabInteractable grab = clueObj.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

            AudioSource audio = clueObj.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.playOnAwake = false;

            ClueInteractable clueInteractable = clueObj.AddComponent<ClueInteractable>();
            clueInteractable.SetClueData(data);

            return clueObj;
        }

        private static void CreateDetectiveNotebook(Scene scene, GameObject rig, string caseTitle, int requiredTrue, int maxFalse)
        {
            Transform uiRoot = FindOrCreateRoot(scene, "UI_Root");
            GameObject canvasObj = new GameObject("DetectiveNotebook_Canvas");
            canvasObj.transform.SetParent(uiRoot, false);

            Vector3 playerPos = rig != null ? rig.transform.position : Vector3.zero;
            canvasObj.transform.position = playerPos + new Vector3(0f, 1.25f, 1.3f);
            canvasObj.transform.rotation = Quaternion.identity;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(500f, 350f);
            canvasRect.localScale = Vector3.one * 0.002f;

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();

            // Fondo
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Titulo
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(canvasObj.transform, false);
            TMPro.TextMeshProUGUI titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
            titleText.text = caseTitle.ToUpperInvariant();
            titleText.fontSize = 18f;
            titleText.fontStyle = TMPro.FontStyles.Bold;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 135f);
            titleRect.sizeDelta = new Vector2(460f, 40f);

            // Contador progreso
            GameObject progObj = new GameObject("ProgressText");
            progObj.transform.SetParent(canvasObj.transform, false);
            TMPro.TextMeshProUGUI progText = progObj.AddComponent<TMPro.TextMeshProUGUI>();
            progText.text = $"Pistas Clave: 0/{requiredTrue} | Pistas Erradas: 0/{maxFalse}";
            progText.fontSize = 14f;
            progText.alignment = TMPro.TextAlignmentOptions.Center;
            progText.color = new Color(0.9f, 0.8f, 0.3f);
            RectTransform progRect = progObj.GetComponent<RectTransform>();
            progRect.anchoredPosition = new Vector2(0f, 95f);
            progRect.sizeDelta = new Vector2(460f, 30f);

            // Contenedor lista
            GameObject containerObj = new GameObject("CluesContainer");
            containerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchoredPosition = new Vector2(0f, -5f);
            containerRect.sizeDelta = new Vector2(460f, 150f);
            UnityEngine.UI.VerticalLayoutGroup layout = containerObj.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;

            // Boton Concluir
            GameObject btnObj = new GameObject("SolveButton");
            btnObj.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image btnImg = btnObj.AddComponent<UnityEngine.UI.Image>();
            btnImg.color = new Color(0.2f, 0.6f, 0.3f);
            UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0f, -125f);
            btnRect.sizeDelta = new Vector2(220f, 42f);

            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            TMPro.TextMeshProUGUI btnText = btnTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            btnText.text = "CONCLUIR CASO";
            btnText.fontSize = 16f;
            btnText.fontStyle = TMPro.FontStyles.Bold;
            btnText.alignment = TMPro.TextAlignmentOptions.Center;
            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            // Mensaje de estado
            GameObject statusObj = new GameObject("StatusMessage");
            statusObj.transform.SetParent(canvasObj.transform, false);
            TMPro.TextMeshProUGUI statusText = statusObj.AddComponent<TMPro.TextMeshProUGUI>();
            statusText.text = $"Reúne las {requiredTrue} pistas clave para emitir la acusación.";
            statusText.fontSize = 12f;
            statusText.alignment = TMPro.TextAlignmentOptions.Center;
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(0f, -160f);
            statusRect.sizeDelta = new Vector2(460f, 25f);

            DetectiveNotebookUI notebookUI = canvasObj.AddComponent<DetectiveNotebookUI>();
            SerializedObject serializedUI = new SerializedObject(notebookUI);
            serializedUI.FindProperty("caseTitleText").objectReferenceValue = titleText;
            serializedUI.FindProperty("progressText").objectReferenceValue = progText;
            serializedUI.FindProperty("statusMessageText").objectReferenceValue = statusText;
            serializedUI.FindProperty("clueListContainer").objectReferenceValue = containerObj.transform;
            serializedUI.FindProperty("solveCaseButton").objectReferenceValue = btn;
            serializedUI.ApplyModifiedProperties();

            CrimeSceneSystemsRoot systemsRoot = Object.FindAnyObjectByType<CrimeSceneSystemsRoot>();
            if (systemsRoot != null)
                systemsRoot.SetDetectiveNotebookUI(notebookUI);
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

        private static Transform FindOrCreateChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
                return existing;

            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static GameObject CreatePlayerRig(Scene scene)
        {
            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("CreatePlayerRig", BindingFlags.NonPublic | BindingFlags.Static);
            return (GameObject)method.Invoke(null, new object[] { scene });
        }

        private static CrimeSceneSystemsRoot CreateSystems(Scene scene, GameObject rig)
        {
            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("CreateSystems", BindingFlags.NonPublic | BindingFlags.Static);
            return (CrimeSceneSystemsRoot)method.Invoke(null, new object[] { scene, rig });
        }

        private static void CreateXRDeviceSimulator(Scene scene)
        {
            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("CreateXRDeviceSimulator", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { scene });
        }

        private static void EnableDesktopRigComponents(Scene scene, CrimeSceneSystemsRoot systemsRoot)
        {
            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("EnableDesktopRigComponents", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { scene, systemsRoot });
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene s in existing)
            {
                if (s.path == scenePath)
                    return;
            }

            EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[existing.Length + 1];
            for (int i = 0; i < existing.Length; i++)
                updated[i] = existing[i];

            updated[existing.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = updated;
        }

        private static void RepairSceneMaterialsForUrp(Scene scene)
        {
            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("RepairSceneMaterialsForUrp", BindingFlags.NonPublic | BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, new object[] { scene });
            }
        }

        [MenuItem("Tools/Crime VR/Fix & Solidify Current Scene Ground & Player", false, 30)]
        public static void FixCurrentSceneGroundAndPlayer()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject environmentRoot = GameObject.Find("Environment");
            if (environmentRoot == null)
            {
                GameObject[] rootObjects = activeScene.GetRootGameObjects();
                foreach (GameObject go in rootObjects)
                {
                    if (go.name.StartsWith("Environment") || go.name.Contains("Map") || go.name.Contains("Ciudad"))
                    {
                        environmentRoot = go;
                        break;
                    }
                }
            }

            if (environmentRoot != null)
            {
                SetupEnvironmentCollidersAndTeleport(environmentRoot);
            }
            else
            {
                MeshFilter[] filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
                foreach (MeshFilter filter in filters)
                {
                    if (filter.sharedMesh == null) continue;
                    MeshCollider mc = filter.GetComponent<MeshCollider>();
                    if (mc == null) mc = filter.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = filter.sharedMesh;
                    mc.convex = false;
                }
            }

            VRPlayerRigReferences rig = Object.FindAnyObjectByType<VRPlayerRigReferences>();
            if (rig != null)
            {
                CharacterController cc = rig.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.center = new Vector3(0f, 0.9f, 0f);
                    cc.height = 1.8f;
                    cc.radius = 0.3f;
                    cc.skinWidth = 0.02f;
                    cc.stepOffset = 0.3f;
                    cc.minMoveDistance = 0f;
                }
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[CustomModelSceneBuilder] ¡Escena actual asegurada y colisionadores de suelo reparados!");
            EditorUtility.DisplayDialog("Crime VR", "¡Colisionadores de suelo y físicas del jugador reparados con éxito en la escena actual!", "OK");
        }
    }
}
