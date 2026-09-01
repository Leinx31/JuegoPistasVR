using System.IO;
using System.Reflection;
using CrimeVR.Evidence;
using CrimeVR.Managers;
using CrimeVR.Player;
using CrimeVR.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace CrimeVR.Editor
{
    public static class CrimeVRTestLabBuilder
    {
        private const string ScenePath = "Assets/Scenes/Scene_CrimeVR_TestLab.unity";

        [MenuItem("Tools/Crime VR/🔬 Crear Escena de Prueba (Manos + Pistas + Sonido)", false, 10)]
        public static void BuildTestLabSceneMenu()
        {
            BuildTestLabScene();
            EditorUtility.DisplayDialog(
                "Crime VR Test Lab",
                "¡Escena de Prueba creada con éxito!\n\n" +
                "• Suelo y mesa sólidos (sin caídas ni bugs).\n" +
                "• Manos VR visibles y funcionales.\n" +
                "• 4 pistas físicas interactivas en la mesa.\n" +
                "• Audio 3D espacial al tomar cada pista.\n" +
                "• Libreta del detective en tiempo real.\n\n" +
                "¡Presiona Play (▶️) para probarla de inmediato!",
                "¡Entendido!");
        }

        public static void BuildTestLabScene()
        {
            EnsureScenesFolder();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Scene_CrimeVR_TestLab";

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.63f, 0.68f);

            // 1. Crear Sala de Pruebas Sólida (Suelo, Paredes, Mesa)
            CreateSolidRoom(scene, out Transform tableTop);

            // 2. Iluminación
            CreateLighting(scene);

            // 3. Crear Player Rig con Manos frente a la mesa
            GameObject rig = CreatePlayerRig(scene);
            rig.transform.position = new Vector3(0f, 0.05f, -1.2f);
            rig.transform.rotation = Quaternion.identity;

            // 4. Crear Sistemas (CaseManager, Inventory, etc.)
            CrimeSceneSystemsRoot systemsRoot = CreateSystems(scene, rig);
            if (systemsRoot != null && systemsRoot.CaseManager != null)
            {
                systemsRoot.CaseManager.Configure(
                    "case.test_lab.001",
                    "Caso: Laboratorio de Balística",
                    3,
                    2);
            }

            // 5. XR Simulator y Modo Escritorio
            CreateXRDeviceSimulator(scene);
            if (systemsRoot != null)
            {
                EnableDesktopRigComponents(scene, systemsRoot);
            }

            // 6. Pistas Físicas sobre la Mesa con Audio 3D y Carteles
            EnsureClueAssets();
            CreatePhysicalCluesOnTable(scene, tableTop);

            // 7. Libreta del Detective
            CreateDetectiveNotebook(scene, rig, "Caso: Laboratorio de Balística", 3, 2);

            // 8. Reparar materiales para URP
            RepairSceneMaterialsForUrp(scene);

            // 9. Guardar y registrar en Build Settings
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CrimeVRTestLabBuilder] ¡Escena de prueba creada con éxito en {ScenePath}!");
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        private static void CreateSolidRoom(Scene scene, out Transform tableTop)
        {
            GameObject environment = new GameObject("Environment");
            SceneManager.MoveGameObjectToScene(environment, scene);

            Material floorMat = CreateOrLoadMaterial("MAT_Test_Floor", new Color(0.2f, 0.22f, 0.26f), 0.2f, 0.4f);
            Material wallMat = CreateOrLoadMaterial("MAT_Test_Wall", new Color(0.65f, 0.68f, 0.72f), 0.1f, 0.1f);
            Material tableMat = CreateOrLoadMaterial("MAT_Test_Table", new Color(0.88f, 0.88f, 0.86f), 0.05f, 0.6f);

            // Suelo Sólido
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Solid_Floor";
            floor.transform.SetParent(environment.transform, false);
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(16f, 0.5f, 16f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.AddComponent<TeleportationArea>();

            // Paredes
            CreateWall(environment.transform, "Wall_North", new Vector3(0f, 2f, 8f), new Vector3(16f, 4f, 0.5f), wallMat);
            CreateWall(environment.transform, "Wall_South", new Vector3(0f, 2f, -8f), new Vector3(16f, 4f, 0.5f), wallMat);
            CreateWall(environment.transform, "Wall_East", new Vector3(8f, 2f, 0f), new Vector3(0.5f, 4f, 16f), wallMat);
            CreateWall(environment.transform, "Wall_West", new Vector3(-8f, 2f, 0f), new Vector3(0.5f, 4f, 16f), wallMat);

            // Mesa de Evidencia Blanca de Alto Contraste
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Evidence_Table";
            table.transform.SetParent(environment.transform, false);
            table.transform.position = new Vector3(0f, 0.45f, 0f);
            table.transform.localScale = new Vector3(2.6f, 0.9f, 1.1f);
            table.GetComponent<Renderer>().sharedMaterial = tableMat;

            // Marcador de superficie de la mesa (Y = 0.90m)
            GameObject topMarker = new GameObject("TableSurface");
            topMarker.transform.SetParent(table.transform, false);
            topMarker.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            tableTop = topMarker.transform;
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateLighting(Scene scene)
        {
            GameObject lighting = new GameObject("Lighting");
            SceneManager.MoveGameObjectToScene(lighting, scene);

            GameObject sun = new GameObject("Directional Light");
            sun.transform.SetParent(lighting.transform, false);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light dirLight = sun.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 1.25f;
            dirLight.color = new Color(0.98f, 0.97f, 0.94f);
            dirLight.shadows = LightShadows.Soft;

            // Foco de estudio directo sobre la mesa de evidencias
            GameObject spot = new GameObject("Table_SpotLight");
            spot.transform.SetParent(lighting.transform, false);
            spot.transform.position = new Vector3(0f, 2.8f, -0.2f);
            spot.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            Light sLight = spot.AddComponent<Light>();
            sLight.type = LightType.Spot;
            sLight.spotAngle = 75f;
            sLight.range = 8f;
            sLight.intensity = 3.5f;
            sLight.color = new Color(1f, 0.98f, 0.92f);
            sLight.shadows = LightShadows.Soft;
        }

        private static void CreatePhysicalCluesOnTable(Scene scene, Transform tableTop)
        {
            Transform interactionRoot = FindOrCreateRoot(scene, "Interaction");
            Transform cluesRoot = FindOrCreateChild(interactionRoot, "EvidenceClues");

            ClueData revolverData = AssetDatabase.LoadAssetAtPath<ClueData>("Assets/Resources/Clues/Clue_Revolver.asset");
            ClueData letterData = AssetDatabase.LoadAssetAtPath<ClueData>("Assets/Resources/Clues/Clue_Letter.asset");
            ClueData keycardData = AssetDatabase.LoadAssetAtPath<ClueData>("Assets/Resources/Clues/Clue_Keycard.asset");
            ClueData cupData = AssetDatabase.LoadAssetAtPath<ClueData>("Assets/Resources/Clues/Clue_CoffeeCup.asset");

            Vector3 tablePos = tableTop != null ? tableTop.position : new Vector3(0f, 0.9f, 0f);

            // 1. REVÓLVER .38 (Arma Homicida) con Marcador CSI [1]
            CreateRevolverClue(cluesRoot, tablePos + new Vector3(-0.75f, 0.08f, 0.05f), revolverData);
            CreateCsiEvidenceMarker(cluesRoot, "1", tablePos + new Vector3(-0.95f, 0.05f, -0.12f));

            // 2. NOTA AMENAZANTE con Marcador CSI [2]
            CreateLetterClue(cluesRoot, tablePos + new Vector3(-0.25f, 0.04f, 0.1f), letterData);
            CreateCsiEvidenceMarker(cluesRoot, "2", tablePos + new Vector3(-0.45f, 0.05f, -0.12f));

            // 3. TARJETA DE ACCESO VIP con Marcador CSI [3]
            CreateKeycardClue(cluesRoot, tablePos + new Vector3(0.25f, 0.04f, 0.05f), keycardData);
            CreateCsiEvidenceMarker(cluesRoot, "3", tablePos + new Vector3(0.08f, 0.05f, -0.12f));

            // 4. VASO DE CAFÉ (Distracción) con Marcador CSI [4]
            CreateCoffeeCupClue(cluesRoot, tablePos + new Vector3(0.75f, 0.12f, 0.05f), cupData);
            CreateCsiEvidenceMarker(cluesRoot, "4", tablePos + new Vector3(0.55f, 0.05f, -0.12f));
        }

        private static void CreateRevolverClue(Transform parent, Vector3 position, ClueData data)
        {
            GameObject gunRoot = new GameObject("Clue_Revolver_38");
            gunRoot.transform.SetParent(parent, false);
            gunRoot.transform.position = position;

            Material steelMat = CreateOrLoadMaterial("MAT_Gun_Steel", new Color(0.22f, 0.24f, 0.28f), 0.9f, 0.8f);
            Material woodMat = CreateOrLoadMaterial("MAT_Gun_WoodGrip", new Color(0.45f, 0.22f, 0.1f), 0.1f, 0.4f);

            // Cañón
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(gunRoot.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.03f, 0.12f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.045f, 0.14f, 0.045f);
            barrel.GetComponent<Renderer>().sharedMaterial = steelMat;
            Object.DestroyImmediate(barrel.GetComponent<Collider>());

            // Tambor / Cilindro
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Cylinder";
            cylinder.transform.SetParent(gunRoot.transform, false);
            cylinder.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            cylinder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cylinder.transform.localScale = new Vector3(0.075f, 0.055f, 0.075f);
            cylinder.GetComponent<Renderer>().sharedMaterial = steelMat;
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());

            // Empuñadura
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(gunRoot.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.06f, -0.08f);
            grip.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.045f, 0.14f, 0.065f);
            grip.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(grip.GetComponent<Collider>());

            // Colisionador principal y físicas
            BoxCollider col = gunRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.01f, 0.02f);
            col.size = new Vector3(0.12f, 0.18f, 0.36f);

            SetupClueComponents(gunRoot, data, "🔫 REVÓLVER .38\n(Arma Homicida)");
        }

        private static void CreateLetterClue(Transform parent, Vector3 position, ClueData data)
        {
            GameObject letterRoot = new GameObject("Clue_ThreatLetter");
            letterRoot.transform.SetParent(parent, false);
            letterRoot.transform.position = position;

            Material paperMat = CreateOrLoadMaterial("MAT_Evidence_Paper", new Color(0.96f, 0.94f, 0.88f), 0f, 0.1f);
            Material redStampMat = CreateOrLoadMaterial("MAT_Stamp_Red", new Color(0.85f, 0.12f, 0.12f), 0.1f, 0.3f);

            // Papel principal
            GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paper.name = "PaperSheet";
            paper.transform.SetParent(letterRoot.transform, false);
            paper.transform.localScale = new Vector3(0.24f, 0.006f, 0.32f);
            paper.GetComponent<Renderer>().sharedMaterial = paperMat;
            Object.DestroyImmediate(paper.GetComponent<Collider>());

            // Sello rojo superior
            GameObject stamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stamp.name = "Stamp";
            stamp.transform.SetParent(letterRoot.transform, false);
            stamp.transform.localPosition = new Vector3(0f, 0.005f, 0.11f);
            stamp.transform.localScale = new Vector3(0.18f, 0.004f, 0.04f);
            stamp.GetComponent<Renderer>().sharedMaterial = redStampMat;
            Object.DestroyImmediate(stamp.GetComponent<Collider>());

            BoxCollider col = letterRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(0.26f, 0.04f, 0.34f);

            SetupClueComponents(letterRoot, data, "✉️ NOTA AMENAZANTE\n(Documento Incriminatorio)");
        }

        private static void CreateKeycardClue(Transform parent, Vector3 position, ClueData data)
        {
            GameObject cardRoot = new GameObject("Clue_AccessKeycard");
            cardRoot.transform.SetParent(parent, false);
            cardRoot.transform.position = position;

            Material cardMat = CreateOrLoadMaterial("MAT_Keycard_Blue", new Color(0.08f, 0.45f, 0.95f), 0.4f, 0.7f);
            Material goldChipMat = CreateOrLoadMaterial("MAT_GoldChip", new Color(0.95f, 0.8f, 0.18f), 0.95f, 0.9f);

            // Cuerpo de tarjeta
            GameObject card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = "CardBody";
            card.transform.SetParent(cardRoot.transform, false);
            card.transform.localScale = new Vector3(0.12f, 0.008f, 0.18f);
            card.GetComponent<Renderer>().sharedMaterial = cardMat;
            Object.DestroyImmediate(card.GetComponent<Collider>());

            // Chip dorado
            GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.name = "GoldChip";
            chip.transform.SetParent(cardRoot.transform, false);
            chip.transform.localPosition = new Vector3(-0.025f, 0.006f, 0.03f);
            chip.transform.localScale = new Vector3(0.035f, 0.004f, 0.035f);
            chip.GetComponent<Renderer>().sharedMaterial = goldChipMat;
            Object.DestroyImmediate(chip.GetComponent<Collider>());

            BoxCollider col = cardRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(0.14f, 0.04f, 0.2f);

            SetupClueComponents(cardRoot, data, "💳 TARJETA VIP\n(Acceso Restringido)");
        }

        private static void CreateCoffeeCupClue(Transform parent, Vector3 position, ClueData data)
        {
            GameObject cupRoot = new GameObject("Clue_CoffeeCup");
            cupRoot.transform.SetParent(parent, false);
            cupRoot.transform.position = position;

            Material cupWhiteMat = CreateOrLoadMaterial("MAT_Cup_White", new Color(0.92f, 0.92f, 0.92f), 0.1f, 0.4f);
            Material sleeveMat = CreateOrLoadMaterial("MAT_Cup_Sleeve", new Color(0.6f, 0.42f, 0.25f), 0f, 0.1f);
            Material lidMat = CreateOrLoadMaterial("MAT_Cup_Lid", new Color(0.12f, 0.12f, 0.14f), 0.3f, 0.5f);

            // Vaso principal
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "CupBody";
            body.transform.SetParent(cupRoot.transform, false);
            body.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            body.GetComponent<Renderer>().sharedMaterial = cupWhiteMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Banda marrón
            GameObject sleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleeve.name = "Sleeve";
            sleeve.transform.SetParent(cupRoot.transform, false);
            sleeve.transform.localScale = new Vector3(0.125f, 0.05f, 0.125f);
            sleeve.GetComponent<Renderer>().sharedMaterial = sleeveMat;
            Object.DestroyImmediate(sleeve.GetComponent<Collider>());

            // Tapa negra
            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lid.name = "Lid";
            lid.transform.SetParent(cupRoot.transform, false);
            lid.transform.localPosition = new Vector3(0f, 0.125f, 0f);
            lid.transform.localScale = new Vector3(0.13f, 0.015f, 0.13f);
            lid.GetComponent<Renderer>().sharedMaterial = lidMat;
            Object.DestroyImmediate(lid.GetComponent<Collider>());

            BoxCollider col = cupRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(0.16f, 0.28f, 0.16f);

            SetupClueComponents(cupRoot, data, "☕ VASO DE CAFÉ\n(Distracción)");
        }

        private static void CreateCsiEvidenceMarker(Transform parent, string number, Vector3 position)
        {
            GameObject marker = new GameObject($"CSI_Marker_{number}");
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;

            Material yellowMat = CreateOrLoadMaterial("MAT_CSI_Yellow", new Color(0.98f, 0.85f, 0.08f), 0.1f, 0.6f);

            // Cono / Carpa triangular amarilla
            GameObject tent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tent.name = "TentBody";
            tent.transform.SetParent(marker.transform, false);
            tent.transform.localScale = new Vector3(0.09f, 0.09f, 0.07f);
            tent.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
            tent.GetComponent<Renderer>().sharedMaterial = yellowMat;

            // Texto con el número
            GameObject labelObj = new GameObject("NumberText");
            labelObj.transform.SetParent(marker.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            labelObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            TMPro.TextMeshPro label = labelObj.AddComponent<TMPro.TextMeshPro>();
            label.text = number;
            label.fontSize = 7f;
            label.fontStyle = TMPro.FontStyles.Bold;
            label.color = Color.black;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(0.3f, 0.3f);
        }

        private static void SetupClueComponents(GameObject clueObj, ClueData clueData, string badgeText)
        {
            Rigidbody rb = clueObj.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            XRGrabInteractable grab = clueObj.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;

            AudioSource audioSource = clueObj.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;

            ClueInteractable clueInteractable = clueObj.AddComponent<ClueInteractable>();
            clueInteractable.SetClueData(clueData);

            // Cartel flotante 3D encima de la pista
            GameObject badgeObj = new GameObject("FloatingBadge");
            badgeObj.transform.SetParent(clueObj.transform, false);
            badgeObj.transform.localPosition = new Vector3(0f, 0.22f, 0f);

            TMPro.TextMeshPro badge = badgeObj.AddComponent<TMPro.TextMeshPro>();
            badge.text = badgeText;
            badge.fontSize = 2.4f;
            badge.fontStyle = TMPro.FontStyles.Bold;
            badge.color = Color.white;
            badge.alignment = TMPro.TextAlignmentOptions.Center;
            badge.rectTransform.sizeDelta = new Vector2(2f, 1f);
        }

        private static void CreateDetectiveNotebook(Scene scene, GameObject rig, string caseTitle, int requiredTrue, int maxFalse)
        {
            Transform uiRoot = FindOrCreateRoot(scene, "UI_Root");
            GameObject canvasObj = new GameObject("DetectiveNotebook_Canvas");
            canvasObj.transform.SetParent(uiRoot, false);

            Vector3 playerPos = rig != null ? rig.transform.position : Vector3.zero;
            canvasObj.transform.position = playerPos + new Vector3(1.1f, 1.25f, 1.4f);
            canvasObj.transform.rotation = Quaternion.Euler(0f, -25f, 0f);

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
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6f;

            // Mensaje de estado
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(canvasObj.transform, false);
            TMPro.TextMeshProUGUI statusText = statusObj.AddComponent<TMPro.TextMeshProUGUI>();
            statusText.text = "Inspecciona los objetos de la mesa para reunir evidencia...";
            statusText.fontSize = 12f;
            statusText.alignment = TMPro.TextAlignmentOptions.Center;
            statusText.color = new Color(0.7f, 0.75f, 0.8f);
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(0f, -95f);
            statusRect.sizeDelta = new Vector2(460f, 25f);

            // Botón Concluir Caso
            GameObject buttonObj = new GameObject("SolveButton");
            buttonObj.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image btnImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
            btnImage.color = new Color(0.18f, 0.55f, 0.34f);
            UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
            RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0f, -135f);
            btnRect.sizeDelta = new Vector2(260f, 38f);

            GameObject btnLabelObj = new GameObject("ButtonLabel");
            btnLabelObj.transform.SetParent(buttonObj.transform, false);
            TMPro.TextMeshProUGUI btnLabel = btnLabelObj.AddComponent<TMPro.TextMeshProUGUI>();
            btnLabel.text = "CONCLUIR CASO";
            btnLabel.fontSize = 15f;
            btnLabel.fontStyle = TMPro.FontStyles.Bold;
            btnLabel.alignment = TMPro.TextAlignmentOptions.Center;
            RectTransform labelRect = btnLabelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            // Prefab de entrada de pista
            GameObject entryPrefab = new GameObject("ClueEntry_Template");
            entryPrefab.transform.SetParent(containerObj.transform, false);
            TMPro.TextMeshProUGUI entryText = entryPrefab.AddComponent<TMPro.TextMeshProUGUI>();
            entryText.fontSize = 13f;
            entryText.text = "• [Pista]";
            RectTransform entryRect = entryPrefab.GetComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(440f, 24f);
            entryPrefab.SetActive(false);

            // Conectar el componente DetectiveNotebookUI
            DetectiveNotebookUI notebookUI = canvasObj.AddComponent<DetectiveNotebookUI>();
            SerializedObject serializedUI = new SerializedObject(notebookUI);
            serializedUI.FindProperty("caseTitleText").objectReferenceValue = titleText;
            serializedUI.FindProperty("progressText").objectReferenceValue = progText;
            serializedUI.FindProperty("statusMessageText").objectReferenceValue = statusText;
            serializedUI.FindProperty("clueListContainer").objectReferenceValue = containerObj.transform;
            serializedUI.FindProperty("clueEntryPrefab").objectReferenceValue = entryPrefab;
            serializedUI.FindProperty("solveCaseButton").objectReferenceValue = button;
            serializedUI.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureClueAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Clues"))
                AssetDatabase.CreateFolder("Assets/Resources", "Clues");

            CreateOrLoadClue("Assets/Resources/Clues/Clue_Revolver.asset", "clue.revolver.001",
                "Revólver Calibre .38", "Arma homicida arrojada en la escena del crimen.", true, "Arma");

            CreateOrLoadClue("Assets/Resources/Clues/Clue_Letter.asset", "clue.note.001",
                "Nota Amenazante", "Mensaje manuscrito incriminatorio encontrado en el lugar.", true, "Documento");

            CreateOrLoadClue("Assets/Resources/Clues/Clue_Keycard.asset", "clue.keycard.001",
                "Tarjeta de Acceso VIP", "Credencial de seguridad que permite el ingreso a áreas restringidas.", true, "Acceso");

            CreateOrLoadClue("Assets/Resources/Clues/Clue_CoffeeCup.asset", "clue.coffee.001",
                "Vaso de Café Desechable", "Vaso abandonado de un transeúnte, sin relevancia para el caso.", false, "Distracción");
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

        private static Material CreateOrLoadMaterial(string name, Color color, float metallic = 0f, float smoothness = 0.5f)
        {
            string path = $"Assets/Materials/{name}.mat";
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null)
                    urpShader = Shader.Find("Standard");

                mat = new Material(urpShader);
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", metallic);
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", smoothness);

                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
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

        private static void RepairSceneMaterialsForUrp(Scene scene)
        {
            MethodInfo method = typeof(CrimeVRProjectSetup).GetMethod("RepairSceneMaterialsForUrp", BindingFlags.NonPublic | BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, new object[] { scene });
            }
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
    }
}
