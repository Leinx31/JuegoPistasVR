using System.IO;
using CrimeVR.Inventory;
using CrimeVR.Managers;
using CrimeVR.Player;
using CrimeVR.Interaction;
using CrimeVR.Evidence;
using CrimeVR.Tools;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using TMPro;
using CrimeVR.UI;

namespace CrimeVR.Editor
{
    public static class CrimeVRProjectSetup
    {
        private const string StarterAssetsRoot = "Assets/XR/Starter Assets";
        private const string SimulatorRoot = "Assets/XR/XR Interaction Simulator";
        private const string CaseSelectionScenePath = "Assets/Scenes/CaseSelection_Map.unity";
        private const string ScenePath = "Assets/Scenes/CrimeScene_Prototype.unity";
        private const string OpenCityScenePath = "Assets/Scenes/OpenCity_Exploration.unity";
        private const string HorrorScenePath = "Assets/Scenes/HorrorMansion_Investigation.unity";
        private const string RigPrefabPath = "Assets/Prefabs/Player/PF_XR_PlayerRig.prefab";
        private const string UVToolPrefabPath = "Assets/Prefabs/Interaction/PF_UVFlashlight.prefab";

        [MenuItem("Tools/Crime VR/Setup Project Foundation")]
        public static void SetupProjectFoundation()
        {
            EnsureFolders();
            ConfigurePlayerSettings();
            ConfigureXRManagement();
            ConfigureOpenXR();
            EnsureInputAssetCopy();

            Scene scene = CreatePrototypeScene();
            GameObject rig = CreatePlayerRig(scene);
            CrimeSceneSystemsRoot systemsRoot = CreateSystems(scene, rig);
            CreateEnvironment(scene);
            CreateEvidenceSample(scene);
            CreateXRDeviceSimulator(scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Base XR configurada correctamente.", "OK");
        }

        [MenuItem("Tools/Crime VR/Upgrade Prototype Layer 2")]
        public static void UpgradePrototypeLayer2()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CrimeSceneSystemsRoot systemsRoot = Object.FindFirstObjectByType<CrimeSceneSystemsRoot>();
            if (systemsRoot == null)
                throw new MissingReferenceException("No se encontro CrimeSceneSystemsRoot en la escena prototipo.");

            VRPlayerRigReferences rigReferences = systemsRoot.PlayerRig;
            if (rigReferences == null)
                throw new MissingReferenceException("No se encontro VRPlayerRigReferences en la escena prototipo.");

            InventoryPanelView inventoryPanelView = EnsureInventoryPanel(scene, systemsRoot, rigReferences, systemsRoot.InventorySystem);
            ObjectInspectionController inspectionController = EnsureInspectionController(scene, systemsRoot, rigReferences, inventoryPanelView);

            systemsRoot.SetInventoryPanelView(inventoryPanelView);
            systemsRoot.SetObjectInspectionController(inspectionController);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(rigReferences.gameObject, RigPrefabPath, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Segunda capa aplicada correctamente.", "OK");
        }

        [MenuItem("Tools/Crime VR/Upgrade Prototype UV Tool")]
        public static void UpgradePrototypeUVTool()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CrimeSceneSystemsRoot systemsRoot = Object.FindFirstObjectByType<CrimeSceneSystemsRoot>();
            if (systemsRoot == null)
                throw new MissingReferenceException("No se encontro CrimeSceneSystemsRoot en la escena prototipo.");

            VRPlayerRigReferences rigReferences = systemsRoot.PlayerRig;
            if (rigReferences == null)
                throw new MissingReferenceException("No se encontro VRPlayerRigReferences en la escena prototipo.");

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/XR/Input/XRI Default Input Actions.inputactions");
            InputActionReference leftActivateAction = FindActionReference(inputActions, "XRI Left Interaction", "Activate");
            InputActionReference rightActivateAction = FindActionReference(inputActions, "XRI Right Interaction", "Activate");

            EnsureUVFlashlightPrefab(leftActivateAction, rightActivateAction);
            EnsureUVToolInScene(scene, rigReferences, leftActivateAction, rightActivateAction);
            EnsureUVReactiveClue(scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(rigReferences.gameObject, RigPrefabPath, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Linterna UV aplicada correctamente.", "OK");
        }

        [MenuItem("Tools/Crime VR/Stage Crime Scene Dressing")]
        public static void StageCrimeSceneDressing()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            Transform interactionRoot = FindOrCreateRoot(scene, "Interaction");
            Transform decorRoot = FindOrCreateChild(environmentRoot, "SetDressing");
            Transform clueRoot = FindOrCreateChild(interactionRoot, "CrimeClues");

            CreateRoomPartitions(decorRoot);
            DressOfficeArea(scene, decorRoot);
            DressStorageArea(scene, decorRoot);
            DressBathroomArea(scene, decorRoot);
            CreateCrimeClues(scene, clueRoot);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Escena vestida con props y pistas base.", "OK");
        }

        [MenuItem("Tools/Crime VR/Enhance Crime Scene Mood")]
        public static void EnhanceCrimeSceneMood()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            Transform decorRoot = FindOrCreateChild(environmentRoot, "SetDressing");
            Transform lightingRoot = FindOrCreateRoot(scene, "Lighting");
            Transform clueRoot = FindOrCreateRoot(scene, "Interaction");

            UpgradeArchitectureMood(environmentRoot, decorRoot);
            UpgradeLightingMood(lightingRoot);
            AddNarrativeProps(scene, decorRoot);
            AddAdditionalCrimeSceneClues(scene, clueRoot);
            ApplyFallbackMaterialsToScene(scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Escena ampliada con atmosfera y decoracion.", "OK");
        }

        [MenuItem("Tools/Crime VR/Fix Scene Materials For URP")]
        public static void FixSceneMaterialsForURP()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RepairSceneMaterialsForUrp(scene);
            ForceKnownProblemSceneMaterials(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Materiales de escena reparados para URP.", "OK");
        }

        [MenuItem("Tools/Crime VR/Replace Office Zone Assets")]
        public static void ReplaceOfficeZoneAssets()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            Transform decorRoot = FindOrCreateChild(environmentRoot, "SetDressing");
            ReplaceOfficeZoneWithStableAssets(scene, decorRoot);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "OfficeZone reemplazada con assets estables.", "OK");
        }

        [MenuItem("Tools/Crime VR/Expand Investigation Scene")]
        public static void ExpandInvestigationScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            Transform decorRoot = FindOrCreateChild(environmentRoot, "SetDressing");
            Transform interactionRoot = FindOrCreateRoot(scene, "Interaction");

            ExpandArchitecture(environmentRoot, decorRoot);
            ReplaceOfficeZoneWithStableAssets(scene, decorRoot);
            ExpandStorageZone(scene, decorRoot);
            ExpandBathroomZone(scene, decorRoot);
            CreateExtendedCrimeClues(scene, interactionRoot);
            RefreshStatusCanvasLayout();
            ApplyFallbackMaterialsToScene(scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Escena ampliada y jugable para investigacion.", "OK");
        }

        [MenuItem("Tools/Crime VR/Enable Desktop Debug Mode")]
        public static void EnableDesktopDebugMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CrimeSceneSystemsRoot systemsRoot = Object.FindFirstObjectByType<CrimeSceneSystemsRoot>();
            if (systemsRoot == null)
                throw new MissingReferenceException("No se encontro CrimeSceneSystemsRoot en la escena prototipo.");

            VRPlayerRigReferences rigReferences = systemsRoot.PlayerRig;
            if (rigReferences == null)
                throw new MissingReferenceException("No se encontro VRPlayerRigReferences en la escena prototipo.");

            GameObject simulatorRoot = GameObject.Find("XR Device Simulator");
            if (simulatorRoot != null)
                simulatorRoot.SetActive(false);

            DesktopDebugController desktopDebugController = rigReferences.GetComponent<DesktopDebugController>();
            if (desktopDebugController == null)
                desktopDebugController = rigReferences.gameObject.AddComponent<DesktopDebugController>();

            desktopDebugController.Configure(
                rigReferences,
                rigReferences.GetComponent<CharacterController>(),
                simulatorRoot);

            DesktopInteractionController desktopInteractionController = rigReferences.GetComponent<DesktopInteractionController>();
            if (desktopInteractionController == null)
                desktopInteractionController = rigReferences.gameObject.AddComponent<DesktopInteractionController>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(rigReferences.gameObject, RigPrefabPath, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Modo escritorio de depuracion activado.", "OK");
        }

        [MenuItem("Tools/Crime VR/Build Expediente 506 Flow")]
        public static void BuildExpediente506Flow()
        {
            Scene menuScene = CreateCaseSelectionScene();
            EditorSceneManager.SaveScene(menuScene, CaseSelectionScenePath);

            Scene investigationScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AdaptCrimeSceneToExpediente506(investigationScene);
            EditorSceneManager.SaveScene(investigationScene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(CaseSelectionScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Flujo Expediente 506 preparado.", "OK");
        }

        [MenuItem("Tools/Crime VR/Build Open City Exploration Scene")]
        public static void BuildOpenCityExplorationScene()
        {
            EnsureFolders();
            EnsureInputAssetCopy();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "OpenCity_Exploration";

            GameObject rig = CreatePlayerRig(scene);
            CrimeSceneSystemsRoot systemsRoot = CreateSystems(scene, rig);
            CreateXRDeviceSimulator(scene);
            CreateOpenCityEnvironment(scene, systemsRoot);
            EnableDesktopRigComponents(scene, systemsRoot);

            EditorSceneManager.SaveScene(scene, OpenCityScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(CaseSelectionScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(OpenCityScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Nueva escena urbana abierta creada.", "OK");
        }

        [MenuItem("Tools/Crime VR/Update Open City Evidence And Collisions")]
        public static void UpdateOpenCityEvidenceAndCollisions()
        {
            Scene scene = EditorSceneManager.OpenScene(OpenCityScenePath, OpenSceneMode.Single);
            CrimeSceneSystemsRoot systemsRoot = Object.FindAnyObjectByType<CrimeSceneSystemsRoot>();

            Transform interactionRoot = FindOrCreateRoot(scene, "Interaction");
            CreateOpenCityWeaponEvidence(scene, interactionRoot, systemsRoot);
            EnsureOpenCitySceneCollisions(scene);

            EditorSceneManager.SaveScene(scene, OpenCityScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Evidencias y colisiones urbanas actualizadas.", "OK");
        }

        [MenuItem("Tools/Crime VR/Optimize Open City Collisions For Quest")]
        public static void OptimizeOpenCityCollisionsForQuest()
        {
            Scene scene = EditorSceneManager.OpenScene(OpenCityScenePath, OpenSceneMode.Single);
            OptimizeOpenCitySceneCollisions(scene);
            EditorSceneManager.SaveScene(scene, OpenCityScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Colisiones optimizadas para Quest.", "OK");
        }

        [MenuItem("Tools/Crime VR/Build Horror Mansion Investigation Scene")]
        public static void BuildHorrorMansionInvestigationScene()
        {
            EnsureFolders();
            EnsureInputAssetCopy();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "HorrorMansion_Investigation";

            GameObject rig = CreatePlayerRig(scene);
            CrimeSceneSystemsRoot systemsRoot = CreateSystems(scene, rig);
            CreateXRDeviceSimulator(scene);
            CreateHorrorEnvironment(scene, systemsRoot);
            EnableDesktopRigComponents(scene, systemsRoot);

            EditorSceneManager.SaveScene(scene, HorrorScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(CaseSelectionScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(OpenCityScenePath, true),
                new EditorBuildSettingsScene(HorrorScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Nueva escena de horror creada.", "OK");
        }

        [MenuItem("Tools/Crime VR/Rebuild Case Selection Menu")]
        public static void RebuildCaseSelectionMenu()
        {
            Scene menuScene = CreateCaseSelectionScene();
            EditorSceneManager.SaveScene(menuScene, CaseSelectionScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(CaseSelectionScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(OpenCityScenePath, true),
                new EditorBuildSettingsScene(HorrorScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Menu principal reconstruido.", "OK");
        }

        [MenuItem("Tools/Crime VR/Fix Meta XR Standalone Validation")]
        public static void FixMetaXRStandaloneValidation()
        {
            ConfigureStandaloneGraphicsApi();
            ConfigureOpenXRStandalone();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Validaciones Standalone de Meta XR corregidas.", "OK");
        }

        [MenuItem("Tools/Crime VR/Enable Return To Menu In All Scenes")]
        public static void EnableReturnToMenuInAllScenes()
        {
            EnsureReturnToMenuInScene(CaseSelectionScenePath);
            EnsureReturnToMenuInScene(ScenePath);
            EnsureReturnToMenuInScene(OpenCityScenePath);
            EnsureReturnToMenuInScene(HorrorScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Crime VR", "Retorno al menu principal activado en todas las escenas.", "OK");
        }

        public static void UpgradePrototypeUVToolBatch()
        {
            UpgradePrototypeUVTool();
            EditorApplication.Exit(0);
        }

        public static void EnableDesktopDebugModeBatch()
        {
            EnableDesktopDebugMode();
            EditorApplication.Exit(0);
        }

        public static void FixMetaXRStandaloneValidationBatch()
        {
            FixMetaXRStandaloneValidation();
            EditorApplication.Exit(0);
        }

        public static void StageCrimeSceneDressingBatch()
        {
            StageCrimeSceneDressing();
            EditorApplication.Exit(0);
        }

        public static void EnhanceCrimeSceneMoodBatch()
        {
            EnhanceCrimeSceneMood();
            EditorApplication.Exit(0);
        }

        public static void FixSceneMaterialsForURPBatch()
        {
            FixSceneMaterialsForURP();
            EditorApplication.Exit(0);
        }

        public static void ReplaceOfficeZoneAssetsBatch()
        {
            ReplaceOfficeZoneAssets();
            EditorApplication.Exit(0);
        }

        public static void ExpandInvestigationSceneBatch()
        {
            ExpandInvestigationScene();
            EditorApplication.Exit(0);
        }

        public static void UpgradePrototypeLayer2Batch()
        {
            UpgradePrototypeLayer2();
            EditorApplication.Exit(0);
        }

        public static void SetupProjectFoundationBatch()
        {
            SetupProjectFoundation();
            EditorApplication.Exit(0);
        }

        public static void BuildExpediente506FlowBatch()
        {
            BuildExpediente506Flow();
            EditorApplication.Exit(0);
        }

        public static void BuildOpenCityExplorationSceneBatch()
        {
            BuildOpenCityExplorationScene();
            EditorApplication.Exit(0);
        }

        public static void UpdateOpenCityEvidenceAndCollisionsBatch()
        {
            UpdateOpenCityEvidenceAndCollisions();
            EditorApplication.Exit(0);
        }

        public static void OptimizeOpenCityCollisionsForQuestBatch()
        {
            OptimizeOpenCityCollisionsForQuest();
            EditorApplication.Exit(0);
        }

        public static void BuildHorrorMansionInvestigationSceneBatch()
        {
            BuildHorrorMansionInvestigationScene();
            EditorApplication.Exit(0);
        }

        public static void RebuildCaseSelectionMenuBatch()
        {
            RebuildCaseSelectionMenu();
            EditorApplication.Exit(0);
        }

        public static void EnableReturnToMenuInAllScenesBatch()
        {
            EnableReturnToMenuInAllScenes();
            EditorApplication.Exit(0);
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Scripts/Core",
                "Assets/Scripts/Player",
                "Assets/Scripts/Interaction",
                "Assets/Scripts/Inventory",
                "Assets/Scripts/Evidence",
                "Assets/Scripts/Tools",
                "Assets/Scripts/UI",
                "Assets/Scripts/Managers",
                "Assets/Scripts/Editor",
                "Assets/Prefabs/Player",
                "Assets/Prefabs/Interaction",
                "Assets/Prefabs/Evidence",
                "Assets/Scenes",
                "Assets/Materials",
                "Assets/Models",
                "Assets/Audio",
                "Assets/Animations",
                "Assets/XR",
                "Assets/XR/Input",
                "Assets/XR/Settings"
            };

            for (int i = 0; i < folders.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(folders[i]))
                {
                    string parent = Path.GetDirectoryName(folders[i])?.Replace("\\", "/");
                    string name = Path.GetFileName(folders[i]);
                    if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                        AssetDatabase.CreateFolder(parent, name);
                }
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.crimevr.juegopistasvr");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard);
        }

        private static void ConfigureXRManagement()
        {
            UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget generalSettings =
                AssetDatabase.LoadAssetAtPath<UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget>(
                    "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");

            if (generalSettings == null)
                return;

            SerializedObject serializedGeneralSettings = new SerializedObject(generalSettings);
            SerializedProperty valuesProperty = serializedGeneralSettings.FindProperty("Values");
            if (valuesProperty == null)
                return;

            for (int i = 0; i < valuesProperty.arraySize; i++)
            {
                Object settingsObject = valuesProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                if (settingsObject is XRGeneralSettings xrGeneralSettings)
                {
                    xrGeneralSettings.Manager.automaticLoading = true;
                    xrGeneralSettings.Manager.automaticRunning = true;
                    EditorUtility.SetDirty(xrGeneralSettings.Manager);
                    EditorUtility.SetDirty(xrGeneralSettings);
                }
            }
        }

        private static void ConfigureOpenXR()
        {
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
                return;

            OculusTouchControllerProfile oculusTouch = settings.GetFeature<OculusTouchControllerProfile>();
            if (oculusTouch != null)
            {
                oculusTouch.enabled = true;
                EditorUtility.SetDirty(oculusTouch);
            }

            MetaQuestFeature metaQuestFeature = settings.GetFeature<MetaQuestFeature>();
            if (metaQuestFeature != null)
            {
                metaQuestFeature.enabled = true;
                EditorUtility.SetDirty(metaQuestFeature);
            }

            EditorUtility.SetDirty(settings);
        }

        private static void ConfigureOpenXRStandalone()
        {
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (settings == null)
                return;

            OculusTouchControllerProfile oculusTouch = settings.GetFeature<OculusTouchControllerProfile>();
            if (oculusTouch != null)
            {
                oculusTouch.enabled = true;
                EditorUtility.SetDirty(oculusTouch);
            }

            EditorUtility.SetDirty(settings);
        }

        private static void ConfigureStandaloneGraphicsApi()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[]
            {
                GraphicsDeviceType.Direct3D11
            });
        }

        private static void EnsureInputAssetCopy()
        {
            string source = $"{StarterAssetsRoot}/XRI Default Input Actions.inputactions";
            string target = "Assets/XR/Input/XRI Default Input Actions.inputactions";

            if (!File.Exists(target) && File.Exists(source))
                AssetDatabase.CopyAsset(source, target);
        }

        private static Scene CreatePrototypeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CrimeScene_Prototype";

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.68f, 0.7f, 0.74f);
            return scene;
        }

        private static GameObject CreatePlayerRig(Scene scene)
        {
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{StarterAssetsRoot}/Prefabs/XR Origin (XR Rig).prefab");
            if (rigPrefab == null)
                throw new FileNotFoundException("No se encontro el prefab XR Origin (XR Rig) de Starter Assets.");

            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
            rig.name = "XR_PlayerRig";
            rig.transform.position = Vector3.zero;

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/XR/Input/XRI Default Input Actions.inputactions");
            InputActionManager inputActionManager = rig.GetComponent<InputActionManager>();
            if (inputActionManager != null && inputActions != null)
                inputActionManager.actionAssets = new System.Collections.Generic.List<InputActionAsset> { inputActions };

            XROrigin origin = rig.GetComponent<XROrigin>();
            if (origin != null)
            {
                origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
                origin.CameraYOffset = 0f;
            }

            CharacterController characterController = rig.GetComponent<CharacterController>();
            if (characterController == null)
                characterController = rig.AddComponent<CharacterController>();

            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.slopeLimit = 45f;
            characterController.stepOffset = 0.2f;

            LocomotionMediator locomotionMediator = rig.GetComponent<LocomotionMediator>();
            XRBodyTransformer bodyTransformer = rig.GetComponent<XRBodyTransformer>();

            if (bodyTransformer == null)
                bodyTransformer = rig.AddComponent<XRBodyTransformer>();

            bodyTransformer.xrOrigin = origin;
            bodyTransformer.useCharacterControllerIfExists = true;

            if (locomotionMediator == null)
                locomotionMediator = rig.AddComponent<LocomotionMediator>();

            ContinuousMoveProvider moveProvider = rig.GetComponent<ContinuousMoveProvider>();
            if (moveProvider == null)
                moveProvider = rig.AddComponent<ContinuousMoveProvider>();

            moveProvider.mediator = locomotionMediator;
            moveProvider.moveSpeed = 1.5f;
            moveProvider.enableFly = false;
            moveProvider.forwardSource = origin.Camera.transform;

            ContinuousTurnProvider turnProvider = rig.GetComponent<ContinuousTurnProvider>();
            if (turnProvider == null)
                turnProvider = rig.AddComponent<ContinuousTurnProvider>();

            turnProvider.mediator = locomotionMediator;
            turnProvider.turnSpeed = 45f;
            turnProvider.enableTurnAround = false;

            ConfigureQuestJoystickLocomotion(moveProvider, turnProvider, inputActions);

            TeleportationProvider teleportationProvider = rig.GetComponent<TeleportationProvider>();
            if (teleportationProvider == null)
                teleportationProvider = rig.AddComponent<TeleportationProvider>();

            teleportationProvider.mediator = locomotionMediator;
            teleportationProvider.delayTime = 0f;

            Transform leftController = rig.transform.Find("Camera Offset/Left Controller");
            Transform rightController = rig.transform.Find("Camera Offset/Right Controller");
            Camera xrCamera = rig.GetComponentInChildren<Camera>(true);
            Transform cameraOffset = rig.transform.Find("Camera Offset");

            if (leftController == null || rightController == null || xrCamera == null || cameraOffset == null)
                throw new MissingReferenceException("El prefab XR importado no contiene la jerarquia esperada.");

            GameObject leftDirect = EnsureInteractorPrefab(scene, $"{StarterAssetsRoot}/Prefabs/Interactors/Direct Interactor.prefab", leftController, "Left Direct Interactor");
            GameObject rightDirect = EnsureInteractorPrefab(scene, $"{StarterAssetsRoot}/Prefabs/Interactors/Direct Interactor.prefab", rightController, "Right Direct Interactor");
            GameObject leftRay = EnsureInteractorPrefab(scene, $"{StarterAssetsRoot}/Prefabs/Interactors/Ray Interactor.prefab", leftController, "Left Ray Interactor");
            GameObject rightRay = EnsureInteractorPrefab(scene, $"{StarterAssetsRoot}/Prefabs/Interactors/Teleport Interactor.prefab", rightController, "Right Teleport Interactor");

            ConfigureInteractorActivity(leftRay, false);
            ConfigureInteractorActivity(rightRay, true);

            CreateGameplayAnchors(rig, xrCamera.transform, out Transform holsterAnchor, out Transform inspectionAnchor);

            VRPlayerRigReferences rigReferences = rig.GetComponent<VRPlayerRigReferences>();
            if (rigReferences == null)
                rigReferences = rig.AddComponent<VRPlayerRigReferences>();

            rigReferences.Configure(
                rig.transform,
                cameraOffset,
                xrCamera,
                leftController,
                rightController,
                leftDirect.GetComponent<XRDirectInteractor>(),
                rightDirect.GetComponent<XRDirectInteractor>(),
                leftRay.GetComponent<XRRayInteractor>(),
                rightRay.GetComponent<XRRayInteractor>(),
                holsterAnchor,
                inspectionAnchor);

            PrefabUtility.SaveAsPrefabAssetAndConnect(rig, RigPrefabPath, InteractionMode.AutomatedAction);
            return rig;
        }

        private static void ConfigureQuestJoystickLocomotion(
            ContinuousMoveProvider moveProvider,
            ContinuousTurnProvider turnProvider,
            InputActionAsset inputActions)
        {
            if (moveProvider == null || turnProvider == null || inputActions == null)
                return;

            InputActionReference leftMoveAction = FindActionReference(inputActions, "XRI Left Locomotion", "Move");
            InputActionReference rightTurnAction = FindActionReference(inputActions, "XRI Right Locomotion", "Turn");

            SerializedObject moveProviderSerialized = new SerializedObject(moveProvider);
            moveProviderSerialized.FindProperty("m_LeftHandMoveInput.m_InputActionReference").objectReferenceValue = leftMoveAction;
            moveProviderSerialized.FindProperty("m_RightHandMoveInput.m_InputActionReference").objectReferenceValue = null;
            moveProviderSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject turnProviderSerialized = new SerializedObject(turnProvider);
            turnProviderSerialized.FindProperty("m_LeftHandTurnInput.m_InputActionReference").objectReferenceValue = null;
            turnProviderSerialized.FindProperty("m_RightHandTurnInput.m_InputActionReference").objectReferenceValue = rightTurnAction;
            turnProviderSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(moveProvider);
            EditorUtility.SetDirty(turnProvider);
        }

        private static void CreateGameplayAnchors(GameObject rig, Transform xrCamera, out Transform holsterAnchor, out Transform inspectionAnchor)
        {
            GameObject holster = new GameObject("InventoryHolster_Anchor");
            holster.transform.SetParent(rig.transform, false);
            holster.transform.localPosition = new Vector3(0f, 1.0f, 0.22f);
            holsterAnchor = holster.transform;

            SphereCollider holsterCollider = holster.AddComponent<SphereCollider>();
            holsterCollider.isTrigger = true;
            holsterCollider.radius = 0.18f;

            InventoryCollectorZone collectorZone = holster.AddComponent<InventoryCollectorZone>();
            collectorZone.SetInventorySystem(null);

            GameObject inspection = new GameObject("Inspection_Anchor");
            inspection.transform.SetParent(xrCamera, false);
            inspection.transform.localPosition = new Vector3(0f, -0.05f, 0.55f);
            inspection.transform.localRotation = Quaternion.identity;
            inspectionAnchor = inspection.transform;

            GameObject toolMount = new GameObject("ToolMount_Primary");
            toolMount.transform.SetParent(rig.transform, false);
            toolMount.transform.localPosition = new Vector3(0.24f, 1.0f, 0.16f);
            toolMount.AddComponent<ForensicToolMount>();
        }

        private static GameObject EnsureInteractorPrefab(Scene scene, string prefabPath, Transform parent, string instanceName)
        {
            Transform existing = parent.Find(instanceName);
            if (existing != null)
                return existing.gameObject;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"No se encontro el prefab de interactor: {prefabPath}");

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return instance;
        }

        private static void ConfigureInteractorActivity(GameObject interactorRoot, bool active)
        {
            XRRayInteractor rayInteractor = interactorRoot.GetComponent<XRRayInteractor>();
            XRInteractorLineVisual lineVisual = interactorRoot.GetComponent<XRInteractorLineVisual>();
            if (rayInteractor != null)
                rayInteractor.enabled = active;
            if (lineVisual != null)
                lineVisual.enabled = active;
        }

        private static CrimeSceneSystemsRoot CreateSystems(Scene scene, GameObject rig)
        {
            GameObject systems = new GameObject("Systems");
            SceneManager.MoveGameObjectToScene(systems, scene);

            GameObject interactionManagerObject = new GameObject("XR Interaction Manager");
            interactionManagerObject.AddComponent<XRInteractionManager>();
            interactionManagerObject.transform.SetParent(systems.transform, false);

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
            eventSystemObject.transform.SetParent(systems.transform, false);

            GameObject managers = new GameObject("GameManagers");
            managers.transform.SetParent(systems.transform, false);

            VRInventorySystem inventorySystem = managers.AddComponent<VRInventorySystem>();
            CrimeSceneSystemsRoot systemsRoot = managers.AddComponent<CrimeSceneSystemsRoot>();
            if (managers.GetComponent<ReturnToMenuController>() == null)
                managers.AddComponent<ReturnToMenuController>();
            systemsRoot.Configure(rig.GetComponent<VRPlayerRigReferences>(), inventorySystem);

            InventoryCollectorZone collectorZone = rig.GetComponentInChildren<InventoryCollectorZone>(true);
            if (collectorZone != null)
                collectorZone.SetInventorySystem(inventorySystem);

            rig.transform.SetParent(systems.transform, false);
            return systemsRoot;
        }

        private static void EnsureReturnToMenuInScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject managers = GameObject.Find("GameManagers");
            if (managers == null)
            {
                managers = new GameObject("GameManagers");
                SceneManager.MoveGameObjectToScene(managers, scene);

                GameObject systems = GameObject.Find("Systems");
                if (systems != null)
                    managers.transform.SetParent(systems.transform, false);
            }

            if (managers.GetComponent<ReturnToMenuController>() == null)
                managers.AddComponent<ReturnToMenuController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateEnvironment(Scene scene)
        {
            GameObject environment = new GameObject("Environment");
            SceneManager.MoveGameObjectToScene(environment, scene);

            GameObject lighting = new GameObject("Lighting");
            SceneManager.MoveGameObjectToScene(lighting, scene);

            GameObject directionalLightObject = new GameObject("Directional Light");
            directionalLightObject.transform.SetParent(lighting.transform, false);
            directionalLightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light directionalLight = directionalLightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.1f;
            directionalLight.shadows = LightShadows.Soft;

            Material wallMaterial = CreateOrLoadMaterial("MAT_Wall_Concrete", new Color(0.78f, 0.8f, 0.82f));
            Material floorMaterial = CreateOrLoadMaterial("MAT_Floor_Concrete", new Color(0.38f, 0.4f, 0.42f));

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor_Main";
            floor.transform.SetParent(environment.transform, false);
            floor.transform.localScale = new Vector3(4f, 1f, 4f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            floor.AddComponent<TeleportationArea>();

            CreateWall(environment.transform, "Wall_North", new Vector3(0f, 1.5f, 4f), new Vector3(8f, 3f, 0.2f), wallMaterial);
            CreateWall(environment.transform, "Wall_South", new Vector3(0f, 1.5f, -4f), new Vector3(8f, 3f, 0.2f), wallMaterial);
            CreateWall(environment.transform, "Wall_East", new Vector3(4f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f), wallMaterial);
            CreateWall(environment.transform, "Wall_West", new Vector3(-4f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f), wallMaterial);
        }

        private static void CreateWall(Transform parent, string wallName, Vector3 position, Vector3 scale, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateEvidenceSample(Scene scene)
        {
            GameObject interactionRoot = new GameObject("Interaction");
            SceneManager.MoveGameObjectToScene(interactionRoot, scene);

            GameObject evidence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            evidence.name = "Evidence_Glove_01";
            evidence.transform.SetParent(interactionRoot.transform, false);
            evidence.transform.position = new Vector3(0f, 1.1f, 1.8f);
            evidence.transform.localScale = new Vector3(0.12f, 0.04f, 0.22f);

            Rigidbody rigidbody = evidence.AddComponent<Rigidbody>();
            rigidbody.mass = 0.35f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            XRGrabInteractable grabInteractable = evidence.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grabInteractable.throwOnDetach = false;

            evidence.AddComponent<InspectableObject>();
            evidence.AddComponent<EvidenceCollectible>();
        }

        private static void CreateXRDeviceSimulator(Scene scene)
        {
            GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SimulatorRoot}/XR Interaction Simulator.prefab");
            if (simulatorPrefab == null)
                return;

            GameObject simulatorInstance = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab, scene);
            simulatorInstance.name = "XR Device Simulator";
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

        private static void CreateRoomPartitions(Transform decorRoot)
        {
            Material wallMaterial = CreateOrLoadMaterial("MAT_Wall_Concrete", new Color(0.78f, 0.8f, 0.82f));
            CreatePartition(decorRoot, "Divider_OfficeToStorage", new Vector3(1.25f, 1.5f, 0.4f), new Vector3(0.15f, 3f, 4.7f), wallMaterial);
            CreatePartition(decorRoot, "Divider_BathroomFront", new Vector3(-2.45f, 1.5f, -0.6f), new Vector3(0.15f, 3f, 2.9f), wallMaterial);
            CreatePartition(decorRoot, "Divider_BathroomSide", new Vector3(-1.4f, 1.5f, -1.95f), new Vector3(2.2f, 3f, 0.15f), wallMaterial);
        }

        private static void UpgradeArchitectureMood(Transform environmentRoot, Transform decorRoot)
        {
            Material wallMaterial = CreateOrLoadMaterial("MAT_Wall_Concrete_Dark", new Color(0.42f, 0.44f, 0.47f));
            Material floorMaterial = CreateOrLoadMaterial("MAT_Floor_Concrete_Dark", new Color(0.16f, 0.16f, 0.17f));
            Material ceilingMaterial = CreateOrLoadMaterial("MAT_Ceiling_Stucco", new Color(0.2f, 0.21f, 0.22f));

            Transform floor = environmentRoot.Find("Floor_Main");
            if (floor != null && floor.TryGetComponent(out Renderer floorRenderer))
            {
                floorRenderer.sharedMaterial = floorMaterial;
                floor.localScale = new Vector3(5f, 1f, 5f);
            }

            string[] baseWalls = { "Wall_North", "Wall_South", "Wall_East", "Wall_West" };
            for (int i = 0; i < baseWalls.Length; i++)
            {
                Transform wall = environmentRoot.Find(baseWalls[i]);
                if (wall != null && wall.TryGetComponent(out Renderer wallRenderer))
                    wallRenderer.sharedMaterial = wallMaterial;
            }

            ApplyMaterialIfPresent(decorRoot.Find("Divider_OfficeToStorage"), wallMaterial);
            ApplyMaterialIfPresent(decorRoot.Find("Divider_BathroomFront"), wallMaterial);
            ApplyMaterialIfPresent(decorRoot.Find("Divider_BathroomSide"), wallMaterial);

            CreatePartition(decorRoot, "Divider_StorageBack", new Vector3(2.9f, 1.5f, 3.85f), new Vector3(2.3f, 3f, 0.15f), wallMaterial);
            CreatePartition(decorRoot, "Divider_OfficeHalfWall", new Vector3(-0.65f, 1.2f, 0.15f), new Vector3(1.4f, 2.4f, 0.12f), wallMaterial);
            CreateCeiling(environmentRoot, ceilingMaterial);
        }

        private static void ApplyMaterialIfPresent(Transform target, Material material)
        {
            if (target == null || material == null)
                return;

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        private static void CreateCeiling(Transform environmentRoot, Material ceilingMaterial)
        {
            Transform existing = environmentRoot.Find("Ceiling_Main");
            GameObject ceiling = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling_Main";
            ceiling.transform.SetParent(environmentRoot, false);
            ceiling.transform.position = new Vector3(0f, 3.05f, 0f);
            ceiling.transform.localScale = new Vector3(10f, 0.15f, 10f);
            if (ceiling.TryGetComponent(out Renderer renderer))
                renderer.sharedMaterial = ceilingMaterial;
        }

        private static void UpgradeLightingMood(Transform lightingRoot)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.075f);

            Transform directionalTransform = lightingRoot.Find("Directional Light");
            Light directionalLight;
            if (directionalTransform == null)
            {
                GameObject directionalLightObject = new GameObject("Directional Light");
                directionalLightObject.transform.SetParent(lightingRoot, false);
                directionalTransform = directionalLightObject.transform;
            }

            directionalTransform.rotation = Quaternion.Euler(18f, -42f, 0f);
            directionalLight = directionalTransform.GetComponent<Light>();
            if (directionalLight == null)
                directionalLight = directionalTransform.gameObject.AddComponent<Light>();

            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 0.18f;
            directionalLight.color = new Color(0.58f, 0.64f, 0.8f);
            directionalLight.shadows = LightShadows.Soft;

            CreateMoodPointLight(lightingRoot, "Light_OfficeLamp", new Vector3(-0.55f, 1.45f, 1.35f), new Color(1f, 0.77f, 0.55f), 2.2f, 4.2f);
            CreateMoodPointLight(lightingRoot, "Light_BathroomCold", new Vector3(-1.95f, 2.1f, -1.1f), new Color(0.62f, 0.75f, 1f), 1.4f, 3.6f);
            CreateMoodPointLight(lightingRoot, "Light_StorageLow", new Vector3(2.8f, 2.35f, 2.55f), new Color(0.95f, 0.7f, 0.48f), 1.2f, 4.8f);
        }

        private static void CreateMoodPointLight(Transform parent, string lightName, Vector3 position, Color color, float intensity, float range)
        {
            Transform existing = parent.Find(lightName);
            GameObject lightObject = existing != null ? existing.gameObject : new GameObject(lightName);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;

            Light light = lightObject.GetComponent<Light>();
            if (light == null)
                light = lightObject.AddComponent<Light>();

            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void AddNarrativeProps(Scene scene, Transform decorRoot)
        {
            Transform officeRoot = FindOrCreateChild(decorRoot, "OfficeZone");
            Transform storageRoot = FindOrCreateChild(decorRoot, "StorageZone");
            Transform bathroomRoot = FindOrCreateChild(decorRoot, "BathroomZone");

            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Sofa2.prefab",
                "Office_Sofa_Waiting", new Vector3(-3.0f, 0f, 2.65f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)CoffeTable.prefab",
                "Office_CoffeeTable", new Vector3(-2.15f, 0f, 2.7f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)TrashCan.prefab",
                "Office_TrashCan", new Vector3(0.65f, 0f, 0.78f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)WaterDispenser.prefab",
                "Office_WaterDispenser", new Vector3(-3.45f, 0f, 3.25f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Plant1.prefab",
                "Office_Plant_01", new Vector3(-3.45f, 0f, 1.0f), Quaternion.identity, Vector3.one);

            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/3 shelves.prefab",
                "Storage_Shelves_Side", new Vector3(3.55f, 0f, -0.95f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Hose.prefab",
                "Storage_Hose_Wall", new Vector3(3.72f, 1.1f, 1.95f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Small stool.prefab",
                "Storage_Stool", new Vector3(2.9f, 0f, 2.35f), Quaternion.Euler(0f, 15f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Machinery parts.on the floor.prefab",
                "Storage_Debris", new Vector3(3.2f, 0f, 3.15f), Quaternion.Euler(0f, 25f, 0f), Vector3.one);

            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_dryer Variant.prefab",
                "Bathroom_Dryer", new Vector3(-1.72f, 1.35f, -0.55f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_toilet_paper Variant.prefab",
                "Bathroom_ToiletPaper", new Vector3(-2.78f, 0.78f, -0.86f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        }

        private static void AddAdditionalCrimeSceneClues(Scene scene, Transform interactionRoot)
        {
            Transform clueRoot = FindOrCreateChild(interactionRoot, "CrimeClues");

            CreateBloodMarker(clueRoot, "Clue_BloodSmear_Bathroom", new Vector3(-1.86f, 0.85f, -1.28f), new Vector3(0.3f, 0.02f, 0.12f));
            CreateBloodMarker(clueRoot, "Clue_BloodPrint_Doorway", new Vector3(1.55f, 0.02f, 0.42f), new Vector3(0.22f, 0.01f, 0.12f));

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Simple Garage/Prefabs/Black suitcase.prefab",
                "Evidence_Suitcase_02",
                new Vector3(3.25f, 0.02f, 2.2f),
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one,
                "evidence.suitcase.002",
                "Black Suitcase",
                "Container",
                "Maletin secundario cerrado ubicado en el fondo del almacen. Podria contener ropa o herramientas.");
        }

        private static void ApplyFallbackMaterialsToScene(Scene scene)
        {
            Material propMetal = CreateOrLoadMaterial("MAT_Prop_Metal", new Color(0.32f, 0.33f, 0.35f));
            Material propPlastic = CreateOrLoadMaterial("MAT_Prop_Plastic", new Color(0.14f, 0.14f, 0.15f));
            Material propFabric = CreateOrLoadMaterial("MAT_Prop_Fabric", new Color(0.28f, 0.21f, 0.16f));

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Renderer[] renderers = roots[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    Renderer renderer = renderers[j];
                    if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                        continue;

                    Material[] materials = renderer.sharedMaterials;
                    bool changed = false;
                    for (int k = 0; k < materials.Length; k++)
                    {
                        if (materials[k] != null)
                            continue;

                        materials[k] = ChooseFallbackMaterial(renderer.gameObject.name, propMetal, propPlastic, propFabric);
                        changed = true;
                    }

                    if (changed)
                        renderer.sharedMaterials = materials;
                }
            }
        }

        private static Material ChooseFallbackMaterial(string objectName, Material propMetal, Material propPlastic, Material propFabric)
        {
            string lowerName = objectName.ToLowerInvariant();
            if (lowerName.Contains("sofa") || lowerName.Contains("chair") || lowerName.Contains("carpet"))
                return propFabric;
            if (lowerName.Contains("mug") || lowerName.Contains("soap") || lowerName.Contains("trash"))
                return propPlastic;

            return propMetal;
        }

        private static void RepairSceneMaterialsForUrp(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Renderer[] renderers = roots[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                    RepairRendererMaterials(renderers[j]);
            }
        }

        private static void RepairRendererMaterials(Renderer renderer)
        {
            if (renderer == null)
                return;

            Material[] sharedMaterials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                Material sourceMaterial = sharedMaterials[i];
                Material repairedMaterial = GetUrpCompatibleMaterial(sourceMaterial, renderer.gameObject.name);
                if (repairedMaterial != sourceMaterial)
                {
                    sharedMaterials[i] = repairedMaterial;
                    changed = true;
                }
            }

            if (changed)
                renderer.sharedMaterials = sharedMaterials;
        }

        private static Material GetUrpCompatibleMaterial(Material sourceMaterial, string objectName)
        {
            if (sourceMaterial == null)
                return ChooseFallbackMaterial(objectName,
                    CreateOrLoadMaterial("MAT_Prop_Metal", new Color(0.32f, 0.33f, 0.35f)),
                    CreateOrLoadMaterial("MAT_Prop_Plastic", new Color(0.14f, 0.14f, 0.15f)),
                    CreateOrLoadMaterial("MAT_Prop_Fabric", new Color(0.28f, 0.21f, 0.16f)));

            if (sourceMaterial.shader != null && sourceMaterial.shader.name.Contains("Universal Render Pipeline"))
                return sourceMaterial;

            string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
            if (string.IsNullOrEmpty(sourcePath) || !sourcePath.StartsWith("Assets/"))
                return CreateUrpMaterialClone(sourceMaterial, $"Assets/Materials/AutoFix_{SanitizeName(objectName)}.mat");

            string directory = Path.GetDirectoryName(sourcePath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(directory) || !directory.StartsWith("Assets/"))
                return CreateUrpMaterialClone(sourceMaterial, $"Assets/Materials/AutoFix_{SanitizeName(objectName)}.mat");

            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            string targetPath = $"{directory}/{fileName}_URP.mat";

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
            if (existing != null)
                return existing;

            return CreateUrpMaterialClone(sourceMaterial, targetPath);
        }

        private static Material CreateUrpMaterialClone(Material sourceMaterial, string targetPath)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = new Material(urpShader);

            if (sourceMaterial.HasProperty("_Color"))
                material.color = sourceMaterial.GetColor("_Color");
            else if (sourceMaterial.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", sourceMaterial.GetColor("_BaseColor"));

            Texture mainTexture = null;
            if (sourceMaterial.HasProperty("_MainTex"))
                mainTexture = sourceMaterial.GetTexture("_MainTex");
            else if (sourceMaterial.HasProperty("_BaseMap"))
                mainTexture = sourceMaterial.GetTexture("_BaseMap");

            if (mainTexture != null)
                material.SetTexture("_BaseMap", mainTexture);

            if (sourceMaterial.HasProperty("_BumpMap"))
            {
                Texture normalMap = sourceMaterial.GetTexture("_BumpMap");
                if (normalMap != null)
                    material.SetTexture("_BumpMap", normalMap);
            }

            string directory = Path.GetDirectoryName(targetPath)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                string parent = Path.GetDirectoryName(directory)?.Replace("\\", "/");
                string leaf = Path.GetFileName(directory);
                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                    AssetDatabase.CreateFolder(parent, leaf);
            }

            AssetDatabase.CreateAsset(material, targetPath);
            return material;
        }

        private static string SanitizeName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Material";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = input;
            for (int i = 0; i < invalidChars.Length; i++)
                sanitized = sanitized.Replace(invalidChars[i], '_');

            sanitized = sanitized.Replace(' ', '_');
            return sanitized;
        }

        private static void ForceKnownProblemSceneMaterials(Scene scene)
        {
            Material fabricMaterial = CreateOrLoadMaterial("MAT_Prop_Fabric", new Color(0.28f, 0.21f, 0.16f));
            Material metalMaterial = CreateOrLoadMaterial("MAT_Prop_Metal", new Color(0.32f, 0.33f, 0.35f));
            Material plasticMaterial = CreateOrLoadMaterial("MAT_Prop_Plastic", new Color(0.14f, 0.14f, 0.15f));

            ForceObjectMaterial("Office_Chair_Main", fabricMaterial);
            ForceObjectMaterial("Office_Drawer_Cabinet", metalMaterial);
            ForceObjectMaterial("Office_Shelves_Back", metalMaterial);
            ForceObjectMaterial("Storage_Shelves_Side", metalMaterial);
            ForceObjectMaterial("Storage_Locker_Open", metalMaterial);
            ForceObjectMaterial("Storage_Locker_Closed", metalMaterial);
            ForceObjectMaterial("Office_PC_Main", plasticMaterial);
            ForceObjectMaterial("Bathroom_Cubicle", metalMaterial);
        }

        private static void ForceObjectMaterial(string objectName, Material material)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null || material == null)
                return;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                    continue;

                for (int j = 0; j < materials.Length; j++)
                    materials[j] = material;

                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ReplaceOfficeZoneWithStableAssets(Scene scene, Transform decorRoot)
        {
            Transform officeRoot = FindOrCreateChild(decorRoot, "OfficeZone");

            ClearOfficeZoneChildren(officeRoot);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Furniture/TableRectangleMedium.prefab",
                "Office_Desk_Main",
                new Vector3(-0.35f, 0f, 1.65f),
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Furniture/ChairDinningA.prefab",
                "Office_Chair_Main",
                new Vector3(-0.35f, 0f, 0.95f),
                Quaternion.Euler(0f, 0f, 0f),
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Lights/LampSmall.prefab",
                "Office_DeskLamp_Main",
                new Vector3(-0.72f, 0.78f, 1.52f),
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Props/Books.prefab",
                "Office_Documents_Main",
                new Vector3(0.08f, 0.78f, 1.44f),
                Quaternion.Euler(0f, 30f, 0f),
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Props/Mug.prefab",
                "Office_Mug_Main",
                new Vector3(-0.38f, 0.78f, 1.16f),
                Quaternion.Euler(0f, 10f, 0f),
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Furniture/ShelvesTallA.prefab",
                "Office_Shelves_Back",
                new Vector3(1.05f, 0f, 3.08f),
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Furniture/ShelfWallSmall.prefab",
                "Office_Shelf_Wall",
                new Vector3(-0.85f, 1.45f, 3.82f),
                Quaternion.identity,
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Furniture/SofaDouble.prefab",
                "Office_Sofa_Waiting",
                new Vector3(-3.0f, 0f, 2.65f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Furniture/TableRectangleSmall.prefab",
                "Office_CoffeeTable",
                new Vector3(-2.12f, 0f, 2.7f),
                Quaternion.identity,
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Props/PlantPotMedium.prefab",
                "Office_Plant_01",
                new Vector3(-3.45f, 0f, 1.05f),
                Quaternion.identity,
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Props/PlantPotRoundMedium.prefab",
                "Office_Plant_02",
                new Vector3(0.9f, 0f, 0.85f),
                Quaternion.identity,
                Vector3.one);

            InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Floor/RugRectangleMedium.prefab",
                "Office_Rug_Main",
                new Vector3(-2.15f, 0.01f, 2.55f),
                Quaternion.identity,
                Vector3.one);

            CreateSimpleOfficeClock(officeRoot);
            CreateOfficeDeskEvidence(scene, officeRoot);
        }

        private static void ExpandArchitecture(Transform environmentRoot, Transform decorRoot)
        {
            Transform floor = environmentRoot.Find("Floor_Main");
            if (floor != null)
                floor.localScale = new Vector3(7f, 1f, 6.5f);

            Transform ceiling = environmentRoot.Find("Ceiling_Main");
            if (ceiling != null)
                ceiling.localScale = new Vector3(14f, 0.15f, 13f);

            Material wallMaterial = CreateOrLoadMaterial("MAT_Wall_Concrete_Dark", new Color(0.42f, 0.44f, 0.47f));
            CreatePartition(decorRoot, "Divider_Archive", new Vector3(5.05f, 1.5f, 0.2f), new Vector3(0.15f, 3f, 5.8f), wallMaterial);
            CreatePartition(decorRoot, "Divider_Hallway", new Vector3(0.45f, 1.5f, -3.3f), new Vector3(5.2f, 3f, 0.15f), wallMaterial);
            CreatePartition(decorRoot, "Divider_ArchiveBack", new Vector3(5.65f, 1.5f, 3.2f), new Vector3(1.3f, 3f, 0.15f), wallMaterial);
        }

        private static void ExpandStorageZone(Scene scene, Transform decorRoot)
        {
            Transform storageRoot = FindOrCreateChild(decorRoot, "StorageZone");
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Big shelf.prefab",
                "Storage_BigShelf_Back", new Vector3(4.5f, 0f, 3.05f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/White suitcase.prefab",
                "Storage_Suitcase_White", new Vector3(4.25f, 0f, 2.1f), Quaternion.Euler(0f, 145f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Drilling machine.prefab",
                "Storage_DrillingMachine", new Vector3(4.2f, 0f, 1.2f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/3 shelves.prefab",
                "Storage_Shelves_Archive", new Vector3(5.55f, 0f, 1.55f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
        }

        private static void ExpandBathroomZone(Scene scene, Transform decorRoot)
        {
            Transform bathroomRoot = FindOrCreateChild(decorRoot, "BathroomZone");
            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_first_aid_kit.prefab",
                "Bathroom_FirstAidKit", new Vector3(-1.7f, 1.55f, -0.45f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_fire_extinguisher.prefab",
                "Bathroom_FireExtinguisher", new Vector3(-1.25f, 0f, -0.3f), Quaternion.identity, Vector3.one);
        }

        private static void ClearOfficeZoneChildren(Transform officeRoot)
        {
            for (int i = officeRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = officeRoot.GetChild(i);
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void CreateSimpleOfficeClock(Transform officeRoot)
        {
            GameObject clock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            clock.name = "Office_Clock_Wall";
            clock.transform.SetParent(officeRoot, false);
            clock.transform.position = new Vector3(-0.05f, 2.25f, 3.84f);
            clock.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            clock.transform.localScale = new Vector3(0.18f, 0.02f, 0.18f);

            Material clockMaterial = CreateOrLoadMaterial("MAT_Clock_Face", new Color(0.9f, 0.9f, 0.88f));
            Renderer renderer = clock.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = clockMaterial;
        }

        private static void CreateOfficeDeskEvidence(Scene scene, Transform officeRoot)
        {
            GameObject deskEvidence = InstantiateDecorPrefab(scene, officeRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Props/Books.prefab",
                "Office_BookOpen_Main",
                new Vector3(0.18f, 0.78f, 1.22f),
                Quaternion.Euler(0f, -15f, 0f),
                Vector3.one);

            if (deskEvidence != null)
                ForceMaterialOnObject(deskEvidence, CreateOrLoadMaterial("MAT_Prop_Fabric", new Color(0.28f, 0.21f, 0.16f)));
        }

        private static void ForceMaterialOnObject(GameObject target, Material material)
        {
            if (target == null || material == null)
                return;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                for (int j = 0; j < materials.Length; j++)
                    materials[j] = material;
                renderers[i].sharedMaterials = materials;
            }
        }

        private static void CreatePartition(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            Transform existing = parent.Find(name);
            GameObject wall = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void DressOfficeArea(Scene scene, Transform decorRoot)
        {
            Transform officeRoot = FindOrCreateChild(decorRoot, "OfficeZone");

            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Desk1.prefab",
                "Office_Desk_Main", new Vector3(-0.3f, 0f, 1.55f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)OfficeChair.prefab",
                "Office_Chair_Main", new Vector3(-0.3f, 0f, 0.75f), Quaternion.Euler(0f, 0f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)PC.prefab",
                "Office_PC_Main", new Vector3(-0.15f, 0.75f, 1.4f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)DeskLight.prefab",
                "Office_DeskLamp_Main", new Vector3(-0.55f, 0.75f, 1.45f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)DocumentHolder.prefab",
                "Office_Documents_Main", new Vector3(0.2f, 0.75f, 1.45f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)BookOpen.prefab",
                "Office_BookOpen_Main", new Vector3(0.05f, 0.75f, 1.2f), Quaternion.Euler(0f, 25f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
                "Office_Mug_Main", new Vector3(-0.42f, 0.75f, 1.12f), Quaternion.Euler(0f, 15f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)BigDrawer.prefab",
                "Office_Drawer_Cabinet", new Vector3(0.95f, 0f, 1.4f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Shelves2.prefab",
                "Office_Shelves_Back", new Vector3(0.95f, 0f, 3.15f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, officeRoot, "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Clock.prefab",
                "Office_Clock_Wall", new Vector3(-0.05f, 2.2f, 3.88f), Quaternion.identity, Vector3.one);
        }

        private static void DressStorageArea(Scene scene, Transform decorRoot)
        {
            Transform storageRoot = FindOrCreateChild(decorRoot, "StorageZone");

            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Large corner shelf.prefab",
                "Storage_Shelf_Corner", new Vector3(3.0f, 0f, 2.85f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Locker.prefab",
                "Storage_Locker_Closed", new Vector3(3.4f, 0f, 1.0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Opened locker.prefab",
                "Storage_Locker_Open", new Vector3(2.55f, 0f, 1.0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Table.prefab",
                "Storage_Table_Work", new Vector3(2.35f, 0f, 3.0f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Red suitcase.prefab",
                "Storage_Suitcase_Red", new Vector3(2.2f, 0.86f, 3.0f), Quaternion.Euler(0f, 8f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Saw.prefab",
                "Storage_Saw_Tool", new Vector3(2.55f, 0.88f, 2.85f), Quaternion.Euler(0f, 35f, 90f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Bench Grinder.prefab",
                "Storage_BenchGrinder", new Vector3(2.8f, 0.86f, 3.15f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, storageRoot, "Assets/Simple Garage/Prefabs/Black suitcase.prefab",
                "Storage_Suitcase_Black", new Vector3(3.3f, 0f, 2.2f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        }

        private static void DressBathroomArea(Scene scene, Transform decorRoot)
        {
            Transform bathroomRoot = FindOrCreateChild(decorRoot, "BathroomZone");

            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_toilet_cubicle Variant.prefab",
                "Bathroom_Cubicle", new Vector3(-3.1f, 0f, -1.45f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_toilet_bowl Variant.prefab",
                "Bathroom_Toilet", new Vector3(-3.15f, 0f, -0.95f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_sink Variant.prefab",
                "Bathroom_Sink", new Vector3(-1.8f, 0f, -1.25f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_mirror.prefab",
                "Bathroom_Mirror", new Vector3(-1.78f, 1.35f, -1.25f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_trash_can Variant.prefab",
                "Bathroom_TrashCan", new Vector3(-1.95f, 0f, -0.45f), Quaternion.Euler(0f, 25f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, bathroomRoot, "Assets/WC/Prefabs/props/SM_soap Variant.prefab",
                "Bathroom_Soap", new Vector3(-1.64f, 1.0f, -1.27f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        }

        private static void CreateCrimeClues(Scene scene, Transform clueRoot)
        {
            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Simple Garage/Prefabs/Red suitcase.prefab",
                "Evidence_Suitcase_01",
                new Vector3(2.35f, 0.86f, 3.0f),
                Quaternion.Euler(0f, 12f, 0f),
                Vector3.one,
                "evidence.suitcase.001",
                "Red Suitcase",
                "Container",
                "Maletin rojo hallado sobre mesa de trabajo. Posible contenedor de evidencia o transporte.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab",
                "Evidence_Mug_01",
                new Vector3(-0.42f, 0.76f, 1.12f),
                Quaternion.Euler(0f, 15f, 0f),
                Vector3.one,
                "evidence.mug.001",
                "Coffee Mug",
                "Trace",
                "Taza abandonada sobre el escritorio. Posible fuente de huellas o ADN.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Simple Garage/Prefabs/Saw.prefab",
                "Evidence_Saw_01",
                new Vector3(2.55f, 0.88f, 2.85f),
                Quaternion.Euler(0f, 35f, 90f),
                Vector3.one,
                "evidence.saw.001",
                "Handsaw",
                "Tool",
                "Serrucho localizado junto al maletin. Debe inspeccionarse por rastros y origen.");

            CreateBloodMarker(clueRoot, "Clue_BloodDrop_Office", new Vector3(-0.08f, 0.02f, 0.92f), new Vector3(0.35f, 0.01f, 0.22f));
            CreateBloodMarker(clueRoot, "Clue_BloodDrag_Storage", new Vector3(2.52f, 0.02f, 2.2f), new Vector3(0.6f, 0.01f, 0.18f));
        }

        private static void CreateExtendedCrimeClues(Scene scene, Transform interactionRoot)
        {
            Transform clueRoot = FindOrCreateChild(interactionRoot, "CrimeClues");

            CreateBloodMarker(clueRoot, "Clue_BloodPool_Archive", new Vector3(4.8f, 0.02f, 1.9f), new Vector3(0.75f, 0.01f, 0.45f));
            CreateBloodMarker(clueRoot, "Clue_BloodSmear_Desk", new Vector3(-0.25f, 0.78f, 1.3f), new Vector3(0.22f, 0.01f, 0.08f));

            ConfigureInspectableMetadata("Clue_BloodDrop_Office", "clue.blood.office.drop", "Blood Drop", "Forensic", "Gota de sangre localizada frente al escritorio.", true);
            ConfigureInspectableMetadata("Clue_BloodDrag_Storage", "clue.blood.storage.drag", "Blood Drag Mark", "Forensic", "Rastro de arrastre hematico hacia el area de almacenamiento.", true);
            ConfigureInspectableMetadata("Clue_BloodSmear_Bathroom", "clue.blood.bathroom.smear", "Blood Smear", "Forensic", "Mancha de limpieza incompleta detectada junto al lavabo.", true);
            ConfigureInspectableMetadata("Clue_BloodPrint_Doorway", "clue.blood.doorway.print", "Blood Print", "Forensic", "Marca parcial de paso encontrada entre zonas.", true);
            ConfigureInspectableMetadata("Clue_BloodPool_Archive", "clue.blood.archive.pool", "Blood Pool", "Forensic", "Acumulacion de sangre en zona secundaria del almacen.", true);
            ConfigureInspectableMetadata("Clue_BloodSmear_Desk", "clue.blood.desk.smear", "Desk Blood Smear", "Forensic", "Rastro de sangre sobre el borde del escritorio.", true);
            ConfigureInspectableMetadata("Evidence_Suitcase_01", "evidence.suitcase.001", "Red Suitcase", "Container", "Maletin rojo hallado sobre mesa de trabajo. Posible contenedor de evidencia o transporte.", false);
            ConfigureInspectableMetadata("Evidence_Suitcase_02", "evidence.suitcase.002", "Black Suitcase", "Container", "Maletin secundario cerrado ubicado en el fondo del almacen. Podria contener ropa o herramientas.", false);
            ConfigureInspectableMetadata("Evidence_Saw_01", "evidence.saw.001", "Handsaw", "Tool", "Serrucho localizado junto al maletin. Debe inspeccionarse por rastros y origen.", false);
        }

        private static void ConfigureInspectableMetadata(string objectName, string clueId, string displayName, string category, string description, bool registerOnInspect)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null)
                return;

            InspectableObject inspectable = target.GetComponent<InspectableObject>();
            if (inspectable == null)
                inspectable = target.AddComponent<InspectableObject>();

            inspectable.SetMetadata(clueId, displayName, category, description, registerOnInspect);
            EditorUtility.SetDirty(inspectable);
        }

        private static GameObject InstantiateDecorPrefab(Scene scene, Transform parent, string prefabPath, string objectName, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
                return existing.gameObject;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return null;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = objectName;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;
            return instance;
        }

        private static void EnsureEvidenceObject(Scene scene, Transform parent, string prefabPath, string objectName, Vector3 position, Quaternion rotation, Vector3 scale,
            string evidenceId, string displayName, string category, string description)
        {
            GameObject instance = InstantiateDecorPrefab(scene, parent, prefabPath, objectName, position, rotation, scale);
            if (instance == null)
                return;

            ReplaceDynamicEvidenceColliders(instance.transform);

            Rigidbody rigidbody = instance.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = instance.AddComponent<Rigidbody>();
            rigidbody.mass = 0.35f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Collider collider = instance.GetComponent<Collider>();
            if (collider == null)
            {
                BoxCollider rootCollider = instance.AddComponent<BoxCollider>();
                Bounds bounds = CalculateHierarchyBounds(instance.transform);
                rootCollider.center = instance.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = instance.transform.InverseTransformVector(bounds.size);
                rootCollider.size = new Vector3(
                    Mathf.Max(Mathf.Abs(localSize.x), 0.05f),
                    Mathf.Max(Mathf.Abs(localSize.y), 0.05f),
                    Mathf.Max(Mathf.Abs(localSize.z), 0.05f));
                collider = rootCollider;
            }

            XRGrabInteractable grabInteractable = instance.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
                grabInteractable = instance.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grabInteractable.throwOnDetach = false;

            if (instance.GetComponent<InspectableObject>() == null)
                instance.AddComponent<InspectableObject>();

            EvidenceCollectible collectible = instance.GetComponent<EvidenceCollectible>();
            if (collectible == null)
                collectible = instance.AddComponent<EvidenceCollectible>();

            SetSerializedField(collectible, "evidenceId", evidenceId);
            SetSerializedField(collectible, "displayName", displayName);
            SetSerializedField(collectible, "category", category);
            SetSerializedField(collectible, "description", description);
        }

        private static void ReplaceDynamicEvidenceColliders(Transform root)
        {
            if (root == null)
                return;

            Collider[] childColliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < childColliders.Length; i++)
            {
                if (childColliders[i] == null)
                    continue;

                if (childColliders[i].transform != root)
                    Object.DestroyImmediate(childColliders[i]);
            }

            Collider rootCollider = root.GetComponent<Collider>();
            if (rootCollider is MeshCollider meshCollider)
                Object.DestroyImmediate(meshCollider);
        }

        private static void CreateBloodMarker(Transform parent, string objectName, Vector3 position, Vector3 scale)
        {
            Transform existing = parent.Find(objectName);
            GameObject marker = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = objectName;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = scale;

            Material material = CreateOrLoadEmissiveMaterial("MAT_Clue_Blood_Dark", new Color(0.18f, 0.02f, 0.02f), Color.black);
            marker.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider = marker.GetComponent<Collider>();
            if (collider == null)
                collider = marker.AddComponent<BoxCollider>();

            InspectableObject inspectable = marker.GetComponent<InspectableObject>();
            if (inspectable == null)
                inspectable = marker.AddComponent<InspectableObject>();
        }

        private static void RefreshStatusCanvasLayout()
        {
            CrimeSceneSystemsRoot systemsRoot = Object.FindFirstObjectByType<CrimeSceneSystemsRoot>();
            if (systemsRoot == null || systemsRoot.PlayerRig == null || systemsRoot.PlayerRig.PlayerCamera == null)
                return;

            Transform statusCanvas = systemsRoot.PlayerRig.PlayerCamera.transform.Find("InspectionStatusCanvas");
            if (statusCanvas == null)
                return;

            statusCanvas.localPosition = new Vector3(0f, 0.16f, 0.55f);
            statusCanvas.localScale = Vector3.one * 0.0007f;

            RectTransform rect = statusCanvas.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(380f, 52f);

            Image background = statusCanvas.GetComponentInChildren<Image>(true);
            if (background != null)
                background.color = new Color(0f, 0f, 0f, 0.28f);
        }

        private static void SetSerializedField(Object target, string fieldName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.stringValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static Material CreateOrLoadMaterial(string materialName, Color baseColor)
        {
            string materialPath = $"Assets/Materials/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader);
            material.color = baseColor;
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static Material CreateOrLoadEmissiveMaterial(string materialName, Color baseColor, Color emissionColor)
        {
            string materialPath = $"Assets/Materials/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader);
            material.color = baseColor;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static InventoryPanelView EnsureInventoryPanel(Scene scene, CrimeSceneSystemsRoot systemsRoot, VRPlayerRigReferences rigReferences, VRInventorySystem inventorySystem)
        {
            Transform existing = rigReferences.LeftControllerRoot.Find("InventoryCanvas_Wrist");
            if (existing != null)
            {
                InventoryPanelView existingView = existing.GetComponent<InventoryPanelView>();
                if (existingView != null)
                    return existingView;
            }

            GameObject canvasObject = new GameObject("InventoryCanvas_Wrist");
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.transform.SetParent(rigReferences.LeftControllerRoot, false);
            canvasObject.transform.localPosition = new Vector3(0.045f, 0.03f, 0.08f);
            canvasObject.transform.localRotation = Quaternion.Euler(15f, -90f, 90f);
            canvasObject.transform.localScale = Vector3.one * 0.00055f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = rigReferences.PlayerCamera;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(700f, 460f);

            GameObject panelObject = new GameObject("Panel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.08f, 0.1f, 0.92f);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            TMP_Text titleText = CreateTextElement(panelObject.transform, "Title", 28, FontStyles.Bold,
                new Vector2(30f, -28f), new Vector2(640f, 56f), TextAlignmentOptions.TopLeft);
            TMP_Text bodyText = CreateTextElement(panelObject.transform, "Body", 24, FontStyles.Normal,
                new Vector2(30f, -92f), new Vector2(640f, 300f), TextAlignmentOptions.TopLeft);
            TMP_Text footerText = CreateTextElement(panelObject.transform, "Footer", 18, FontStyles.Italic,
                new Vector2(30f, -390f), new Vector2(640f, 42f), TextAlignmentOptions.TopLeft);

            footerText.text = "Trigger izquierdo: mostrar u ocultar inventario";

            InventoryPanelView panelView = canvasObject.AddComponent<InventoryPanelView>();
            panelView.Configure(inventorySystem, canvas, titleText, bodyText);
            panelView.SetVisible(true);
            panelView.Refresh();

            return panelView;
        }

        private static ObjectInspectionController EnsureInspectionController(Scene scene, CrimeSceneSystemsRoot systemsRoot, VRPlayerRigReferences rigReferences, InventoryPanelView panelView)
        {
            ObjectInspectionController controller = systemsRoot.GetComponent<ObjectInspectionController>();
            if (controller == null)
                controller = systemsRoot.gameObject.AddComponent<ObjectInspectionController>();

            GameObject statusRoot = EnsureStatusCanvas(scene, rigReferences, out TMP_Text statusText);

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/XR/Input/XRI Default Input Actions.inputactions");
            InputActionReference inspectAction = FindActionReference(inputActions, "XRI Right Interaction", "Activate");
            InputActionReference inventoryAction = FindActionReference(inputActions, "XRI Left Interaction", "Activate");

            controller.Configure(rigReferences, inspectAction, inventoryAction, panelView, statusText);
            return controller;
        }

        private static GameObject EnsureStatusCanvas(Scene scene, VRPlayerRigReferences rigReferences, out TMP_Text statusText)
        {
            Transform existing = rigReferences.PlayerCamera.transform.Find("InspectionStatusCanvas");
            if (existing != null)
            {
                statusText = existing.GetComponentInChildren<TextMeshProUGUI>(true);
                return existing.gameObject;
            }

            GameObject canvasObject = new GameObject("InspectionStatusCanvas");
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.transform.SetParent(rigReferences.PlayerCamera.transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, 0.16f, 0.55f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * 0.0007f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = rigReferences.PlayerCamera;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(380f, 52f);

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            Image background = backgroundObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.28f);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            statusText = CreateTextElement(backgroundObject.transform, "Status", 16, FontStyles.Normal,
                new Vector2(12f, -8f), new Vector2(340f, 30f), TextAlignmentOptions.MidlineLeft);
            statusText.text = "Trigger derecho: inspeccionar evidencia en mano";
            return canvasObject;
        }

        private static TMP_Text CreateTextElement(Transform parent, string objectName, int fontSize, FontStyles fontStyle, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;

            RectTransform rectTransform = text.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            return text;
        }

        private static GameObject CreateImage(Transform parent, string objectName, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject imageObject = new GameObject(objectName);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            return imageObject;
        }

        private static TMP_Text CreateText(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, int fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;

            RectTransform rectTransform = text.rectTransform;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return text;
        }

        private static InputActionReference FindActionReference(InputActionAsset asset, string mapName, string actionName)
        {
            if (asset == null)
                return null;

            InputActionMap map = asset.FindActionMap(mapName, true);
            InputAction action = map.FindAction(actionName, true);
            return InputActionReference.Create(action);
        }

        private static void EnsureUVFlashlightPrefab(InputActionReference leftActivateAction, InputActionReference rightActivateAction)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UVToolPrefabPath);
            if (existingPrefab != null)
                return;

            GameObject flashlightRoot = new GameObject("PF_UVFlashlight_Source");

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(flashlightRoot.transform, false);
            body.transform.localScale = new Vector3(0.035f, 0.11f, 0.035f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = CreateOrLoadMaterial("MAT_UVFlashlight_Body", new Color(0.1f, 0.1f, 0.12f));

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Head";
            head.transform.SetParent(flashlightRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            head.transform.localScale = new Vector3(0.05f, 0.04f, 0.05f);
            head.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            head.GetComponent<Renderer>().sharedMaterial = CreateOrLoadMaterial("MAT_UVFlashlight_Head", new Color(0.18f, 0.18f, 0.2f));

            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lens.name = "Lens";
            lens.transform.SetParent(head.transform, false);
            lens.transform.localPosition = new Vector3(0f, 0f, 0.48f);
            lens.transform.localScale = new Vector3(0.55f, 0.55f, 0.18f);
            lens.GetComponent<Renderer>().sharedMaterial = CreateOrLoadEmissiveMaterial(
                "MAT_UVFlashlight_Lens",
                new Color(0.12f, 0.08f, 0.16f),
                new Color(0.45f, 0.1f, 0.95f));

            Object.DestroyImmediate(body.GetComponent<Collider>());
            Object.DestroyImmediate(head.GetComponent<Collider>());
            Object.DestroyImmediate(lens.GetComponent<Collider>());

            CapsuleCollider collider = flashlightRoot.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0f, 0.06f);
            collider.radius = 0.04f;
            collider.height = 0.34f;
            collider.direction = 2;

            Rigidbody rigidbody = flashlightRoot.AddComponent<Rigidbody>();
            rigidbody.mass = 0.45f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            XRGrabInteractable grabInteractable = flashlightRoot.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grabInteractable.throwOnDetach = false;

            GameObject attachPoint = new GameObject("AttachPoint");
            attachPoint.transform.SetParent(flashlightRoot.transform, false);
            attachPoint.transform.localPosition = new Vector3(0f, -0.01f, 0.03f);
            attachPoint.transform.localRotation = Quaternion.identity;
            grabInteractable.attachTransform = attachPoint.transform;

            GameObject beamOrigin = new GameObject("BeamOrigin");
            beamOrigin.transform.SetParent(flashlightRoot.transform, false);
            beamOrigin.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            beamOrigin.transform.localRotation = Quaternion.identity;

            Light uvLight = beamOrigin.AddComponent<Light>();
            uvLight.type = LightType.Spot;
            uvLight.color = new Color(0.45f, 0.15f, 1f);
            uvLight.intensity = 5f;
            uvLight.range = 3.2f;
            uvLight.spotAngle = 42f;
            uvLight.innerSpotAngle = 22f;
            uvLight.shadows = LightShadows.None;
            uvLight.enabled = false;

            UVFlashlightTool uvTool = flashlightRoot.AddComponent<UVFlashlightTool>();
            uvTool.Configure(uvLight, beamOrigin.transform, leftActivateAction, rightActivateAction);

            PrefabUtility.SaveAsPrefabAsset(flashlightRoot, UVToolPrefabPath);
            Object.DestroyImmediate(flashlightRoot);
        }

        private static void EnsureUVToolInScene(Scene scene, VRPlayerRigReferences rigReferences, InputActionReference leftActivateAction, InputActionReference rightActivateAction)
        {
            Transform existingTool = Object.FindFirstObjectByType<UVFlashlightTool>()?.transform;
            if (existingTool != null)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UVToolPrefabPath);
            if (prefab == null)
                throw new FileNotFoundException("No se encontro el prefab de la linterna UV.");

            GameObject uvTool = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            uvTool.name = "Tool_UVFlashlight_01";
            uvTool.transform.position = rigReferences.InventoryHolsterAnchor.position + rigReferences.RigRoot.right * 0.24f + Vector3.up * 0.02f;
            uvTool.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            UVFlashlightTool tool = uvTool.GetComponent<UVFlashlightTool>();
            if (tool != null)
                tool.Configure(tool.GetComponentInChildren<Light>(true), tool.transform.Find("BeamOrigin"), leftActivateAction, rightActivateAction);
        }

        private static Scene CreateCaseSelectionScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CaseSelection_Map";

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.09f);

            GameObject environment = new GameObject("Environment");
            SceneManager.MoveGameObjectToScene(environment, scene);

            GameObject lighting = new GameObject("Lighting");
            SceneManager.MoveGameObjectToScene(lighting, scene);
            CreateMoodPointLight(lighting.transform, "Light_Menu_Key", new Vector3(0f, 5f, -1f), new Color(0.65f, 0.75f, 1f), 3.4f, 12f);

            GameObject cameraObject = new GameObject("MenuCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.025f, 0.04f);
            cameraObject.transform.position = new Vector3(0f, 1.8f, -8.5f);
            cameraObject.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();

            CreateMenuBackdrop(environment.transform);
            CreateMenuMapTable(scene, environment.transform);
            CreateCaseSelectionCanvas(scene);
            return scene;
        }

        private static void CreateMenuBackdrop(Transform environmentRoot)
        {
            Material darkFloor = CreateOrLoadMaterial("MAT_Menu_Floor", new Color(0.08f, 0.085f, 0.1f));
            Material wallMaterial = CreateOrLoadMaterial("MAT_Menu_Wall", new Color(0.11f, 0.12f, 0.15f));
            Material accentMaterial = CreateOrLoadMaterial("MAT_Menu_Accent", new Color(0.42f, 0.08f, 0.08f));

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor_Menu";
            floor.transform.SetParent(environmentRoot, false);
            floor.transform.localScale = new Vector3(2.4f, 1f, 1.8f);
            floor.GetComponent<Renderer>().sharedMaterial = darkFloor;

            CreateWall(environmentRoot, "Wall_Back", new Vector3(0f, 2.4f, 5.6f), new Vector3(14f, 4.8f, 0.2f), wallMaterial);
            CreateWall(environmentRoot, "Wall_Left", new Vector3(-7f, 2.4f, 0f), new Vector3(0.2f, 4.8f, 11.5f), wallMaterial);
            CreateWall(environmentRoot, "Wall_Right", new Vector3(7f, 2.4f, 0f), new Vector3(0.2f, 4.8f, 11.5f), wallMaterial);

            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "Banner_Expediente506";
            banner.transform.SetParent(environmentRoot, false);
            banner.transform.position = new Vector3(0f, 3.2f, 4.95f);
            banner.transform.localScale = new Vector3(5.8f, 0.6f, 0.08f);
            banner.GetComponent<Renderer>().sharedMaterial = accentMaterial;
        }

        private static void CreateMenuMapTable(Scene scene, Transform environmentRoot)
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "MapTable";
            table.transform.SetParent(environmentRoot, false);
            table.transform.position = new Vector3(0f, 0.78f, 0.6f);
            table.transform.localScale = new Vector3(4.4f, 0.16f, 2.4f);
            table.GetComponent<Renderer>().sharedMaterial = CreateOrLoadMaterial("MAT_Menu_Table", new Color(0.18f, 0.16f, 0.14f));

            CreateCaseMarker(scene, table.transform, "Marker_SanJose", new Vector3(-1.2f, 0.18f, 0.15f), "San Jose Centro");
            CreateCaseMarker(scene, table.transform, "Marker_PenasBlancas", new Vector3(0f, 0.18f, -0.2f), "Penas Blancas");
            CreateCaseMarker(scene, table.transform, "Marker_Limon", new Vector3(1.25f, 0.18f, 0.25f), "Puerto de Limon");
        }

        private static void CreateCaseMarker(Scene scene, Transform parent, string name, Vector3 localPosition, string label)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = new Vector3(0.12f, 0.025f, 0.12f);
            marker.GetComponent<Renderer>().sharedMaterial = CreateOrLoadEmissiveMaterial(
                $"MAT_{name}",
                new Color(0.22f, 0.05f, 0.05f),
                new Color(0.9f, 0.15f, 0.15f));

            GameObject labelObject = new GameObject($"{name}_Label");
            labelObject.transform.SetParent(marker.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.text = label;
            text.fontSize = 1.8f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private static void CreateCaseSelectionCanvas(Scene scene)
        {
            GameObject systems = new GameObject("Systems");
            SceneManager.MoveGameObjectToScene(systems, scene);

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(systems.transform, false);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<XRUIInputModule>();

            GameObject canvasObject = new GameObject("CaseSelectionCanvas");
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            CreateImage(canvasObject.transform, "Overlay", new Color(0f, 0f, 0f, 0.2f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateImage(canvasObject.transform, "TopBar", new Color(0.08f, 0.09f, 0.12f, 0.94f), new Vector2(0f, 0.9f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            TMP_Text header = CreateText(canvasObject.transform, "Header", new Vector2(0.04f, 0.925f), new Vector2(0.55f, 0.985f), 36, FontStyles.Bold, TextAlignmentOptions.Left);
            header.text = "CRIME VR INVESTIGATIONS";

            TMP_Text subHeader = CreateText(canvasObject.transform, "SubHeader", new Vector2(0.04f, 0.885f), new Vector2(0.62f, 0.93f), 18, FontStyles.Normal, TextAlignmentOptions.Left);
            subHeader.text = "Seleccione un escenario investigable";

            GameObject cardRow = new GameObject("CardRow");
            cardRow.transform.SetParent(canvasObject.transform, false);
            RectTransform rowRect = cardRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.08f, 0.16f);
            rowRect.anchorMax = new Vector2(0.92f, 0.54f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            CaseSelectionMenu menu = canvasObject.AddComponent<CaseSelectionMenu>();
            var entries = new System.Collections.Generic.List<CaseSelectionMenu.CaseEntry>();

            CreateCaseCard(cardRow.transform, entries, 0, "Escena Forense Interior",
                "Prototipo cerrado para pruebas de pistas, inventario, inspeccion y herramientas forenses.",
                "CrimeScene_Prototype");
            CreateCaseCard(cardRow.transform, entries, 1, "Ciudad Abierta",
                "Escenario urbano amplio con calles, edificios, vehiculos y evidencias distribuidas para exploracion.",
                "OpenCity_Exploration");
            CreateCaseCard(cardRow.transform, entries, 2, "Mansion de Horror",
                "Recorrido interior de tension con habitaciones, pasillos, indicios sangrientos y evidencias clave.",
                "HorrorMansion_Investigation");

            TMP_Text title = CreateText(canvasObject.transform, "CaseTitle", new Vector2(0.08f, 0.62f), new Vector2(0.58f, 0.72f), 32, FontStyles.Bold, TextAlignmentOptions.Left);
            TMP_Text summary = CreateText(canvasObject.transform, "CaseSummary", new Vector2(0.08f, 0.57f), new Vector2(0.76f, 0.79f), 20, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            TMP_Text hint = CreateText(canvasObject.transform, "CaseHint", new Vector2(0.08f, 0.08f), new Vector2(0.76f, 0.14f), 18, FontStyles.Bold, TextAlignmentOptions.Left);

            menu.Configure(entries, title, summary, hint);
        }

        private static void CreateCaseCard(Transform parent, System.Collections.Generic.List<CaseSelectionMenu.CaseEntry> entries, int index, string title, string summary, string sceneName)
        {
            GameObject card = CreateImage(parent, $"CaseCard_{index}", new Color(0.16f, 0.18f, 0.22f, 0.94f),
                new Vector2(index * 0.33f, 0f), new Vector2((index * 0.33f) + 0.3f, 1f), new Vector2(6f, 0f), new Vector2(-6f, 0f));

            Button button = card.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.selectedColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;

            CreateImage(card.transform, "Accent", new Color(0.62f, 0.12f, 0.12f, 1f), new Vector2(0f, 0.88f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            TMP_Text titleText = CreateText(card.transform, "Title", new Vector2(0.06f, 0.62f), new Vector2(0.92f, 0.84f), 24, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            titleText.text = title;

            TMP_Text summaryText = CreateText(card.transform, "Summary", new Vector2(0.06f, 0.14f), new Vector2(0.92f, 0.56f), 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            summaryText.text = summary;

            entries.Add(new CaseSelectionMenu.CaseEntry
            {
                caseId = $"case.{index}",
                displayName = title,
                summary = summary,
                sceneName = sceneName,
                button = button
            });
        }

        private static void AdaptCrimeSceneToExpediente506(Scene scene)
        {
            CrimeSceneSystemsRoot systemsRoot = Object.FindFirstObjectByType<CrimeSceneSystemsRoot>();
            if (systemsRoot == null)
                throw new MissingReferenceException("No se encontro CrimeSceneSystemsRoot en la escena principal.");

            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            Transform decorRoot = FindOrCreateChild(environmentRoot, "SetDressing");
            Transform interactionRoot = FindOrCreateRoot(scene, "Interaction");
            Transform lightingRoot = FindOrCreateRoot(scene, "Lighting");

            ExpandUrbanPerimeter(environmentRoot, decorRoot);
            CreateForensicOperationsZone(scene, decorRoot);
            CreateUrbanEvidenceCluster(scene, interactionRoot);
            CreateScenarioHudCanvas(scene);
            CreateInvestigatorViewRig(systemsRoot.PlayerRig);
            UpgradeLightingMood(lightingRoot);
            ApplyFallbackMaterialsToScene(scene);
            RepairSceneMaterialsForUrp(scene);
        }

        private static void CreateOpenCityEnvironment(Scene scene, CrimeSceneSystemsRoot systemsRoot)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.22f, 0.25f);

            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            Transform layoutRoot = FindOrCreateChild(environmentRoot, "CityLayout");
            Transform buildingRoot = FindOrCreateChild(environmentRoot, "Buildings");
            Transform propRoot = FindOrCreateChild(environmentRoot, "StreetProps");
            Transform vegetationRoot = FindOrCreateChild(environmentRoot, "Vegetation");
            Transform lightingRoot = FindOrCreateRoot(scene, "Lighting");
            Transform interactionRoot = FindOrCreateRoot(scene, "Interaction");

            CreateOpenCityLighting(lightingRoot);
            CreateOpenCityGround(scene, environmentRoot);
            CreateOpenCityRoadGrid(scene, layoutRoot);
            CreateOpenCityBuildings(scene, buildingRoot);
            CreateOpenCityStreetProps(scene, propRoot);
            CreateOpenCityVegetation(scene, vegetationRoot);
            CreateOpenCityBoundaries(scene, environmentRoot);
            CreateOpenCityLandmarks(scene, buildingRoot);
            CreateOpenCityInteractables(scene, interactionRoot, systemsRoot);
            GroundOpenCityObjects(scene);

            ApplyFallbackMaterialsToScene(scene);
            RepairSceneMaterialsForUrp(scene);
        }

        private static void CreateHorrorEnvironment(Scene scene, CrimeSceneSystemsRoot systemsRoot)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.03f, 0.04f);

            Transform environmentRoot = FindOrCreateRoot(scene, "Environment");
            Transform architectureRoot = FindOrCreateChild(environmentRoot, "HorrorArchitecture");
            Transform decorRoot = FindOrCreateChild(environmentRoot, "HorrorDecor");
            Transform lightingRoot = FindOrCreateRoot(scene, "Lighting");
            Transform interactionRoot = FindOrCreateRoot(scene, "Interaction");

            CreateHorrorLighting(lightingRoot);
            BuildHorrorLayout(scene, architectureRoot);
            DressHorrorRooms(scene, decorRoot);
            CreateHorrorEvidence(scene, interactionRoot);
            EnsureHorrorSceneCollisions(scene);

            if (systemsRoot != null && systemsRoot.PlayerRig != null)
                CreateScenarioSpawnMarker(systemsRoot.PlayerRig.transform, new Vector3(0f, 0f, -4.5f));

            ApplyFallbackMaterialsToScene(scene);
            RepairSceneMaterialsForUrp(scene);
        }

        private static void CreateHorrorLighting(Transform lightingRoot)
        {
            GameObject moonObject = new GameObject("Directional Light");
            moonObject.transform.SetParent(lightingRoot, false);
            moonObject.transform.rotation = Quaternion.Euler(22f, -40f, 0f);
            Light moon = moonObject.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.intensity = 0.14f;
            moon.color = new Color(0.56f, 0.62f, 0.78f);
            moon.shadows = LightShadows.Soft;

            CreateMoodPointLight(lightingRoot, "Light_Foyer", new Vector3(0f, 2.4f, -1.5f), new Color(1f, 0.7f, 0.42f), 2.1f, 8f);
            CreateMoodPointLight(lightingRoot, "Light_Corridor", new Vector3(0f, 2.2f, 8f), new Color(0.7f, 0.78f, 1f), 1.1f, 6f);
            CreateMoodPointLight(lightingRoot, "Light_Bedroom", new Vector3(-6f, 2.1f, 12f), new Color(1f, 0.62f, 0.52f), 1.2f, 5f);
            CreateMoodPointLight(lightingRoot, "Light_Study", new Vector3(6f, 2.1f, 12f), new Color(0.95f, 0.78f, 0.55f), 1.3f, 5f);
            CreateMoodPointLight(lightingRoot, "Light_Cellar", new Vector3(0f, 1.8f, 20f), new Color(0.62f, 0.72f, 1f), 0.9f, 6f);
        }

        private static void BuildHorrorLayout(Scene scene, Transform architectureRoot)
        {
            string floors = "Assets/FpsHorrorKit/Prefabs/Floors/";
            string walls = "Assets/FpsHorrorKit/Prefabs/ModulerWalls/";
            string doors = "Assets/FpsHorrorKit/Prefabs/Doors/";
            string cellar = "Assets/FpsHorrorKit/Prefabs/Cellar/";
            string stairs = "Assets/FpsHorrorKit/Prefabs/ModularParts/";

            for (int z = -1; z <= 7; z++)
            {
                for (int x = -2; x <= 2; x++)
                    InstantiateDecorPrefab(scene, architectureRoot, floors + "Floor_3x3.prefab", $"Floor_{x}_{z}", new Vector3(x * 3f, 0f, z * 3f), Quaternion.identity, Vector3.one);
            }

            for (int z = -1; z <= 7; z++)
            {
                InstantiateDecorPrefab(scene, architectureRoot, walls + "Wall_3x3.prefab", $"Wall_W_{z}", new Vector3(-7.5f, 0f, z * 3f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
                InstantiateDecorPrefab(scene, architectureRoot, walls + "Wall_3x3.prefab", $"Wall_E_{z}", new Vector3(7.5f, 0f, z * 3f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            }

            for (int x = -2; x <= 2; x++)
            {
                InstantiateDecorPrefab(scene, architectureRoot, walls + "Wall_3x3.prefab", $"Wall_S_{x}", new Vector3(x * 3f, 0f, -4.5f), Quaternion.identity, Vector3.one);
                InstantiateDecorPrefab(scene, architectureRoot, walls + "Wall_3x3.prefab", $"Wall_N_{x}", new Vector3(x * 3f, 0f, 22.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            }

            InstantiateDecorPrefab(scene, architectureRoot, doors + "Door_Frame.prefab", "Entry_Frame", new Vector3(0f, 0f, -4.5f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, architectureRoot, doors + "OutDoor.prefab", "Entry_Door", new Vector3(0f, 0f, -4.35f), Quaternion.identity, Vector3.one);

            InstantiateDecorPrefab(scene, architectureRoot, walls + "Wall_3x3.prefab", "Split_Foyer_Left", new Vector3(-1.5f, 0f, 4.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, architectureRoot, walls + "Wall_3x3.prefab", "Split_Foyer_Right", new Vector3(1.5f, 0f, 4.5f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, architectureRoot, doors + "Single_Door.prefab", "Door_LeftRoom", new Vector3(-3f, 0f, 7.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, architectureRoot, doors + "Single_Door.prefab", "Door_RightRoom", new Vector3(3f, 0f, 7.5f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);

            InstantiateDecorPrefab(scene, architectureRoot, walls + "Wall_3x3.prefab", "MidSplit_Left", new Vector3(-1.5f, 0f, 13.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, architectureRoot, walls + "Wall_3x3.prefab", "MidSplit_Right", new Vector3(1.5f, 0f, 13.5f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, architectureRoot, doors + "BreakDoor.prefab", "Door_Cellar", new Vector3(0f, 0f, 16.5f), Quaternion.identity, Vector3.one);

            InstantiateDecorPrefab(scene, architectureRoot, stairs + "Stairs.prefab", "Main_Stairs", new Vector3(4.5f, 0f, 16.8f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, architectureRoot, stairs + "StairsRailing.prefab", "Main_Stairs_Railing", new Vector3(4.5f, 0f, 16.8f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);

            InstantiateDecorPrefab(scene, architectureRoot, cellar + "cellar_wall_3x3.prefab", "Cellar_Back", new Vector3(0f, 0f, 19.5f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, architectureRoot, cellar + "cellar_wall_2x3.prefab", "Cellar_Left", new Vector3(-4.5f, 0f, 19.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, architectureRoot, cellar + "cellar_wall_2x3.prefab", "Cellar_Right", new Vector3(4.5f, 0f, 19.5f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
        }

        private static void DressHorrorRooms(Scene scene, Transform decorRoot)
        {
            string furn = "Assets/FpsHorrorKit/Prefabs/Furnitures/";
            string lights = "Assets/FpsHorrorKit/Prefabs/Furnitures/Lights/";
            string props = "Assets/FpsHorrorKit/Prefabs/Props/";
            string decals = "Assets/FpsHorrorKit/Prefabs/Decals/";
            string interactables = "Assets/FpsHorrorKit/Prefabs/InteractablePrefabs/";
            string pipes = "Assets/FpsHorrorKit/Models/NewModels/modular_industrial_pipes_01_2k.fbx/Prefabs/";

            InstantiateDecorPrefab(scene, decorRoot, lights + "Chandelier.prefab", "Foyer_Chandelier", new Vector3(0f, 2.4f, 0f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, furn + "Vintage_Grantfather_Clock.prefab", "Foyer_Clock", new Vector3(-5.8f, 0f, 1.2f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, furn + "ClassicConsole.prefab", "Foyer_Console", new Vector3(5.6f, 0f, 1.1f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, props + "fancy_picture_frame_a.prefab", "Foyer_Frame_A", new Vector3(-5.7f, 1.6f, -1.8f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, props + "fancy_picture_frame_b.prefab", "Foyer_Frame_B", new Vector3(5.7f, 1.5f, -1.4f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);

            InstantiateDecorPrefab(scene, decorRoot, furn + "GothicBed.prefab", "Bedroom_Bed", new Vector3(-4.8f, 0f, 12f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, furn + "GothicCabinet.prefab", "Bedroom_Cabinet", new Vector3(-6f, 0f, 15.5f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, furn + "Rockingchair.prefab", "Bedroom_RockingChair", new Vector3(-2.8f, 0f, 10.8f), Quaternion.Euler(0f, -25f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, props + "vintage_pocket_watch.prefab", "Bedroom_Watch", new Vector3(-4.2f, 0.82f, 13.2f), Quaternion.Euler(0f, 15f, 0f), Vector3.one);

            InstantiateDecorPrefab(scene, decorRoot, furn + "Desk.prefab", "Study_Desk", new Vector3(4.8f, 0f, 12.4f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, furn + "GreenChair.prefab", "Study_Chair", new Vector3(3.9f, 0f, 12.2f), Quaternion.Euler(0f, 85f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, props + "cardboard_box.prefab", "Study_Box", new Vector3(6f, 0f, 14.8f), Quaternion.Euler(0f, 20f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, interactables + "ITOPhotoCamera.prefab", "Study_PhotoCamera", new Vector3(4.8f, 0.86f, 12.3f), Quaternion.Euler(0f, 12f, 0f), Vector3.one);

            InstantiateDecorPrefab(scene, decorRoot, pipes + "modular_industrial_pipes_01_pipe_a.prefab", "Cellar_Pipe_A", new Vector3(-2.8f, 1.6f, 19.5f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, pipes + "modular_industrial_pipes_01_pipe_c.prefab", "Cellar_Pipe_B", new Vector3(2.4f, 1.4f, 18.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, interactables + "Lantern.prefab", "Cellar_Lantern", new Vector3(0.8f, 0.15f, 19.6f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, interactables + "ITO_Paper_a.prefab", "Cellar_Paper_Decor", new Vector3(0f, 0.05f, 18.4f), Quaternion.identity, Vector3.one);

            InstantiateDecorPrefab(scene, decorRoot, decals + "FootPrint_01_L.prefab", "Decal_Footprint_L", new Vector3(0.35f, 0.02f, 5.8f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, decals + "FootPrint_01_R.prefab", "Decal_Footprint_R", new Vector3(-0.15f, 0.02f, 6.6f), Quaternion.Euler(0f, 12f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, decals + "HandPrint_L.prefab", "Decal_Handprint_L", new Vector3(-5.8f, 1.2f, 13.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, decorRoot, decals + "Fingerprint_DarkGrey.prefab", "Decal_Fingerprint", new Vector3(5.7f, 1.1f, 13.6f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
        }

        private static void CreateHorrorEvidence(Scene scene, Transform interactionRoot)
        {
            Transform clueRoot = FindOrCreateChild(interactionRoot, "HorrorEvidence");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Nokobot/Modern Guns - Handgun/_Prefabs/Handgun Black/M1911 Handgun_Black.prefab",
                "Evidence_HorrorGun_01",
                new Vector3(4.85f, 0.86f, 12.1f),
                Quaternion.Euler(0f, -12f, 93f),
                Vector3.one,
                "evidence.horror.gun.001",
                "Pistola en escritorio",
                "Arma de fuego",
                "Pistola recuperada en el estudio principal. Podria vincularse al ocupante desaparecido.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Low Poly Stylized Knife Pack/Prefabs/1mat/Knife05_1mat.prefab",
                "Evidence_HorrorKnife_01",
                new Vector3(-3.8f, 0.62f, 12.8f),
                Quaternion.Euler(90f, 24f, 0f),
                Vector3.one * 1.05f,
                "evidence.horror.knife.001",
                "Cuchillo de dormitorio",
                "Arma blanca",
                "Cuchillo hallado en el dormitorio. Su ubicacion sugiere forcejeo o defensa.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/FpsHorrorKit/Prefabs/InteractablePrefabs/ITO_Paper_a.prefab",
                "Evidence_HorrorLetter_01",
                new Vector3(0.4f, 0.06f, 18.3f),
                Quaternion.Euler(0f, 0f, 0f),
                Vector3.one,
                "evidence.horror.letter.001",
                "Carta deteriorada",
                "Documento",
                "Carta encontrada en el sotano. Puede contener motivaciones o nombres clave del caso.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/FpsHorrorKit/Prefabs/Props/vintage_pocket_watch.prefab",
                "Evidence_HorrorWatch_01",
                new Vector3(-4.2f, 0.82f, 13.2f),
                Quaternion.Euler(0f, 20f, 0f),
                Vector3.one,
                "evidence.horror.watch.001",
                "Reloj de bolsillo",
                "Objeto personal",
                "Reloj antiguo detenido. Puede ayudar a fijar una linea temporal del incidente.");

            CreateBloodMarker(clueRoot, "Clue_BloodSmear_Foyer", new Vector3(0f, 0.02f, 2.8f), new Vector3(0.55f, 0.01f, 0.2f));
            CreateBloodMarker(clueRoot, "Clue_BloodPool_Cellar", new Vector3(0.2f, 0.02f, 19.7f), new Vector3(0.7f, 0.01f, 0.42f));
            CreateBulletCluster(scene, clueRoot, "HorrorBulletCluster_A", new Vector3(4.5f, 0.04f, 12.7f),
                "Assets/Nokobot/Modern Guns - Handgun/_Prefabs/45ACP Bullet_Casing.prefab", 4, 0.12f, "evidence.horror.bullets.a");
        }

        private static void EnsureHorrorSceneCollisions(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                EnsureHorrorCollisionRecursive(roots[i].transform);
        }

        private static void EnsureHorrorCollisionRecursive(Transform target)
        {
            if (target == null)
                return;

            string name = target.name.ToLowerInvariant();
            bool isEvidence = name.StartsWith("evidence_") || name.Contains("_bullet_");
            bool isPlayer = name.Contains("xr_playerrig") || name.Contains("eventsystem");

            if (!isEvidence && !isPlayer)
            {
                Renderer renderer = target.GetComponent<Renderer>();
                if (renderer != null && target.GetComponent<Collider>() == null)
                {
                    BoxCollider collider = target.gameObject.AddComponent<BoxCollider>();
                    collider.center = renderer.localBounds.center;
                    collider.size = renderer.localBounds.size;
                }
            }

            for (int i = 0; i < target.childCount; i++)
                EnsureHorrorCollisionRecursive(target.GetChild(i));
        }

        private static void EnableDesktopRigComponents(Scene scene, CrimeSceneSystemsRoot systemsRoot)
        {
            if (systemsRoot == null || systemsRoot.PlayerRig == null)
                return;

            VRPlayerRigReferences rigReferences = systemsRoot.PlayerRig;
            InventoryPanelView inventoryPanelView = EnsureInventoryPanel(scene, systemsRoot, rigReferences, systemsRoot.InventorySystem);
            ObjectInspectionController inspectionController = EnsureInspectionController(scene, systemsRoot, rigReferences, inventoryPanelView);
            systemsRoot.SetInventoryPanelView(inventoryPanelView);
            systemsRoot.SetObjectInspectionController(inspectionController);

            GameObject simulatorRoot = GameObject.Find("XR Device Simulator");
            if (simulatorRoot != null)
                simulatorRoot.SetActive(false);

            DesktopDebugController desktopDebugController = rigReferences.GetComponent<DesktopDebugController>();
            if (desktopDebugController == null)
                desktopDebugController = rigReferences.gameObject.AddComponent<DesktopDebugController>();

            desktopDebugController.Configure(rigReferences, rigReferences.GetComponent<CharacterController>(), simulatorRoot);

            DesktopInteractionController desktopInteractionController = rigReferences.GetComponent<DesktopInteractionController>();
            if (desktopInteractionController == null)
                desktopInteractionController = rigReferences.gameObject.AddComponent<DesktopInteractionController>();
        }

        private static void CreateOpenCityLighting(Transform lightingRoot)
        {
            GameObject sunObject = new GameObject("Directional Light");
            sunObject.transform.SetParent(lightingRoot, false);
            sunObject.transform.rotation = Quaternion.Euler(38f, -25f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.shadows = LightShadows.Soft;

            CreateMoodPointLight(lightingRoot, "Intersection_Fill_A", new Vector3(0f, 4.5f, 0f), new Color(0.82f, 0.88f, 1f), 0.8f, 16f);
            CreateMoodPointLight(lightingRoot, "Intersection_Fill_B", new Vector3(24f, 4.5f, 24f), new Color(0.82f, 0.88f, 1f), 0.65f, 16f);
        }

        private static void CreateOpenCityGround(Scene scene, Transform environmentRoot)
        {
            GameObject baseGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            baseGround.name = "Ground_Base";
            baseGround.transform.SetParent(environmentRoot, false);
            baseGround.transform.localScale = new Vector3(14f, 1f, 14f);
            baseGround.GetComponent<Renderer>().sharedMaterial = CreateOrLoadMaterial("MAT_City_Ground", new Color(0.23f, 0.24f, 0.21f));
            baseGround.AddComponent<TeleportationArea>();

            GameObject blockInset = GameObject.CreatePrimitive(PrimitiveType.Plane);
            blockInset.name = "Ground_BlockInset";
            blockInset.transform.SetParent(environmentRoot, false);
            blockInset.transform.position = new Vector3(0f, 0.01f, 0f);
            blockInset.transform.localScale = new Vector3(11f, 1f, 11f);
            blockInset.GetComponent<Renderer>().sharedMaterial = CreateOrLoadMaterial("MAT_City_BlockInset", new Color(0.18f, 0.19f, 0.18f));
        }

        private static void CreateOpenCityRoadGrid(Scene scene, Transform layoutRoot)
        {
            string floorRoot = "Assets/POLYGON city pack/Prefabs/Floor/";
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 4 16M prefab.prefab", "Road_Main_NS_01", new Vector3(0f, 0.02f, 0f), Quaternion.identity);
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 4 16M prefab.prefab", "Road_Main_NS_02", new Vector3(0f, 0.02f, 16f), Quaternion.identity);
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 4 16M prefab.prefab", "Road_Main_NS_03", new Vector3(0f, 0.02f, -16f), Quaternion.identity);

            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 4 16M prefab.prefab", "Road_Main_EW_01", new Vector3(0f, 0.02f, 0f), Quaternion.Euler(0f, 90f, 0f));
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 4 16M prefab.prefab", "Road_Main_EW_02", new Vector3(16f, 0.02f, 0f), Quaternion.Euler(0f, 90f, 0f));
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 4 16M prefab.prefab", "Road_Main_EW_03", new Vector3(-16f, 0.02f, 0f), Quaternion.Euler(0f, 90f, 0f));

            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 10 Prefab.prefab", "Road_Cross_Center", new Vector3(0f, 0.025f, 0f), Quaternion.identity);
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 10 Prefab.prefab", "Road_Cross_NE", new Vector3(16f, 0.025f, 16f), Quaternion.identity);
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 10 Prefab.prefab", "Road_Cross_NW", new Vector3(-16f, 0.025f, 16f), Quaternion.identity);
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 10 Prefab.prefab", "Road_Cross_SE", new Vector3(16f, 0.025f, -16f), Quaternion.identity);
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Street 10 Prefab.prefab", "Road_Cross_SW", new Vector3(-16f, 0.025f, -16f), Quaternion.identity);

            PlaceRoadTile(scene, layoutRoot, floorRoot + "Sideway 10 prefab.prefab", "Sidewalk_Center_A", new Vector3(8f, 0.03f, 8f), Quaternion.identity);
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Sideway 10 prefab.prefab", "Sidewalk_Center_B", new Vector3(-8f, 0.03f, 8f), Quaternion.Euler(0f, 90f, 0f));
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Sideway 10 prefab.prefab", "Sidewalk_Center_C", new Vector3(8f, 0.03f, -8f), Quaternion.Euler(0f, 180f, 0f));
            PlaceRoadTile(scene, layoutRoot, floorRoot + "Sideway 10 prefab.prefab", "Sidewalk_Center_D", new Vector3(-8f, 0.03f, -8f), Quaternion.Euler(0f, 270f, 0f));
        }

        private static void CreateOpenCityBuildings(Scene scene, Transform buildingRoot)
        {
            string polyBuildings = "Assets/POLYGON city pack/Prefabs/Buildings/";
            string versatile = "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/";

            InstantiateDecorPrefab(scene, buildingRoot, versatile + "office_building_1_with_base.prefab", "Block_Office_A", new Vector3(-26f, 0f, -22f), Quaternion.identity, Vector3.one * 1.35f);
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "office_building_2_with_base.prefab", "Block_Office_B", new Vector3(-6f, 0f, -24f), Quaternion.identity, Vector3.one * 1.25f);
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "office_building_3_with_base.prefab", "Block_Office_C", new Vector3(17f, 0f, -24f), Quaternion.identity, Vector3.one * 1.25f);
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "office_building_4_with_base.prefab", "Block_Office_D", new Vector3(34f, 0f, -20f), Quaternion.identity, Vector3.one * 1.2f);

            InstantiateDecorPrefab(scene, buildingRoot, versatile + "small_house_1.prefab", "House_Row_A1", new Vector3(-29f, 0f, 23f), Quaternion.identity, Vector3.one * 1.3f);
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "small_house_2.prefab", "House_Row_A2", new Vector3(-18f, 0f, 24f), Quaternion.identity, Vector3.one * 1.3f);
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "small_house_3.prefab", "House_Row_A3", new Vector3(-6f, 0f, 23f), Quaternion.identity, Vector3.one * 1.3f);
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "mid_house_1.prefab", "House_Row_B1", new Vector3(11f, 0f, 24f), Quaternion.identity, Vector3.one * 1.35f);
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "mid_house_4.prefab", "House_Row_B2", new Vector3(24f, 0f, 23f), Quaternion.identity, Vector3.one * 1.35f);

            InstantiateDecorPrefab(scene, buildingRoot, polyBuildings + "Police_station_prefab.prefab", "Landmark_PoliceStation", new Vector3(-28f, 0f, -2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.25f);
            InstantiateDecorPrefab(scene, buildingRoot, polyBuildings + "Hospital_prefab.prefab", "Landmark_Hospital", new Vector3(28f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 1.15f);
            InstantiateDecorPrefab(scene, buildingRoot, polyBuildings + "Gas_station_A_PREFAB.prefab", "Landmark_GasStation", new Vector3(26f, 0f, 15f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.2f);
            InstantiateDecorPrefab(scene, buildingRoot, polyBuildings + "Supermaket_prefab.prefab", "Landmark_Supermarket", new Vector3(-24f, 0f, 14f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.15f);
            InstantiateDecorPrefab(scene, buildingRoot, polyBuildings + "Car_repair_prefab.prefab", "Landmark_Garage", new Vector3(2f, 0f, 27f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.2f);
        }

        private static void CreateOpenCityStreetProps(Scene scene, Transform propRoot)
        {
            string polyProps = "Assets/POLYGON city pack/Prefabs/Props/";
            string trafficSigns = "Assets/POLYGON city pack/Prefabs/traffic signs/";
            string versatile = "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/";

            Vector3[] trafficPositions =
            {
                new Vector3(3.8f, 0f, 3.8f), new Vector3(-3.8f, 0f, 3.8f),
                new Vector3(3.8f, 0f, -3.8f), new Vector3(-3.8f, 0f, -3.8f)
            };
            for (int i = 0; i < trafficPositions.Length; i++)
                InstantiateDecorPrefab(scene, propRoot, polyProps + "Traffic light 1 Prefab.prefab", $"TrafficLight_{i + 1}", trafficPositions[i], Quaternion.Euler(0f, i * 90f, 0f), Vector3.one);

            InstantiateDecorPrefab(scene, propRoot, trafficSigns + "stop sign.prefab", "Sign_Stop_NW", new Vector3(-5.5f, 0f, 5.5f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, trafficSigns + "traffic sign speed 25.prefab", "Sign_Speed_01", new Vector3(5.8f, 0f, 9.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, trafficSigns + "parking sign.prefab", "Sign_Parking_01", new Vector3(-11.2f, 0f, -6.4f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, trafficSigns + "slow sign.prefab", "Sign_Slow_01", new Vector3(14.2f, 0f, -6.6f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);

            for (int i = -2; i <= 2; i++)
            {
                InstantiateDecorPrefab(scene, propRoot, versatile + "lamp_pole_dual_white.prefab", $"Lamp_North_{i + 3}", new Vector3(i * 8f, 0f, 10.5f), Quaternion.identity, Vector3.one);
                InstantiateDecorPrefab(scene, propRoot, versatile + "lamp_pole_dual_white.prefab", $"Lamp_South_{i + 3}", new Vector3(i * 8f, 0f, -10.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            }

            InstantiateDecorPrefab(scene, propRoot, polyProps + "Bus stop prefab.prefab", "BusStop_Main", new Vector3(-13f, 0f, 1.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.05f);
            InstantiateDecorPrefab(scene, propRoot, versatile + "bench.prefab", "Bench_BusStop", new Vector3(-12.1f, 0f, -0.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, polyProps + "phone booth prefab.prefab", "PhoneBooth_Corner", new Vector3(12.4f, 0f, -11.2f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, polyProps + "Mail_box prefab.prefab", "Mailbox_Center", new Vector3(9.5f, 0f, 11.8f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, polyProps + "Hydrant prefab.prefab", "Hydrant_01", new Vector3(-8.5f, 0f, -8.7f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, polyProps + "trashcan prefab.prefab", "TrashCan_01", new Vector3(7.5f, 0f, 7.2f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, polyProps + "trashBag prefab.prefab", "TrashBag_01", new Vector3(8.2f, 0f, 7.8f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, polyProps + "parking meter prefab.prefab", "ParkingMeter_01", new Vector3(-15.3f, 0f, -3f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, polyProps + "Parking_barrier prefab.prefab", "ParkingBarrier_01", new Vector3(-17.5f, 0f, -1f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);

            InstantiateDecorPrefab(scene, propRoot, polyProps + "shopping cart prefab.prefab", "Street_ShoppingCart", new Vector3(18f, 0f, 18f), Quaternion.Euler(0f, 18f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, propRoot, polyProps + "StreetSellerStand prefab.prefab", "StreetSellerStand", new Vector3(-18f, 0f, 18f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);

            CreateRustyCars(scene, propRoot);
        }

        private static void CreateRustyCars(Scene scene, Transform propRoot)
        {
            string rustyCars = "Assets/OlegWER/RustyCarsFree/Prefabs/";
            InstantiateDecorPrefab(scene, propRoot, rustyCars + "SM_asset_00.prefab", "RustyCar_01", new Vector3(-10f, 0f, -13f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.1f);
            InstantiateDecorPrefab(scene, propRoot, rustyCars + "SM_asset_01.prefab", "RustyCar_02", new Vector3(14f, 0f, 12.2f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 1.05f);
            InstantiateDecorPrefab(scene, propRoot, rustyCars + "SM_asset_02.prefab", "RustyCar_03", new Vector3(21f, 0f, -14.2f), Quaternion.identity, Vector3.one * 1.08f);
            InstantiateDecorPrefab(scene, propRoot, rustyCars + "SM_asset_03.prefab", "RustyCar_04", new Vector3(-24f, 0f, 5f), Quaternion.Euler(0f, 30f, 0f), Vector3.one * 1.15f);
            InstantiateDecorPrefab(scene, propRoot, rustyCars + "SM_asset_04.prefab", "RustyCar_05", new Vector3(25f, 0f, 20f), Quaternion.Euler(0f, 210f, 0f), Vector3.one * 1.1f);
        }

        private static void CreateOpenCityVegetation(Scene scene, Transform vegetationRoot)
        {
            string oakTree = "Assets/ALP_Assets/Big Oak Tree FREE/Prefabs/OakBigTree01_pr.prefab";
            string polyProps = "Assets/POLYGON city pack/Prefabs/Props/";

            Vector3[] treePositions =
            {
                new Vector3(-24f, 0f, -12f), new Vector3(-22f, 0f, 11f), new Vector3(21f, 0f, -10f),
                new Vector3(24f, 0f, 12f), new Vector3(-8f, 0f, 20f), new Vector3(9f, 0f, -20f),
                new Vector3(31f, 0f, 27f), new Vector3(-31f, 0f, 29f)
            };

            float[] treeScales = { 0.85f, 1.05f, 0.95f, 1.2f, 0.78f, 1.15f, 1.35f, 0.92f };

            for (int i = 0; i < treePositions.Length; i++)
                InstantiateDecorPrefab(scene, vegetationRoot, oakTree, $"OakTree_{i + 1}", treePositions[i], Quaternion.Euler(0f, i * 37f, 0f), Vector3.one * treeScales[i]);

            InstantiateDecorPrefab(scene, vegetationRoot, polyProps + "hedge prefab.prefab", "Hedge_01", new Vector3(-20f, 0f, 16f), Quaternion.identity, Vector3.one * 1.4f);
            InstantiateDecorPrefab(scene, vegetationRoot, polyProps + "hedge_curve prefab.prefab", "Hedge_02", new Vector3(20f, 0f, 16f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.4f);
            InstantiateDecorPrefab(scene, vegetationRoot, polyProps + "Flower mass prefab.prefab", "Flowers_01", new Vector3(-12f, 0f, 12f), Quaternion.identity, Vector3.one * 1.2f);
            InstantiateDecorPrefab(scene, vegetationRoot, polyProps + "Bush 2 mass prefab.prefab", "BushMass_01", new Vector3(12f, 0f, -12f), Quaternion.identity, Vector3.one * 1.1f);
        }

        private static void CreateOpenCityBoundaries(Scene scene, Transform environmentRoot)
        {
            Material boundaryMaterial = CreateOrLoadMaterial("MAT_City_Boundary", new Color(0.17f, 0.18f, 0.2f));
            CreateWall(environmentRoot, "Boundary_North", new Vector3(0f, 3f, 42f), new Vector3(90f, 6f, 0.8f), boundaryMaterial);
            CreateWall(environmentRoot, "Boundary_South", new Vector3(0f, 3f, -42f), new Vector3(90f, 6f, 0.8f), boundaryMaterial);
            CreateWall(environmentRoot, "Boundary_East", new Vector3(42f, 3f, 0f), new Vector3(0.8f, 6f, 90f), boundaryMaterial);
            CreateWall(environmentRoot, "Boundary_West", new Vector3(-42f, 3f, 0f), new Vector3(0.8f, 6f, 90f), boundaryMaterial);
        }

        private static void CreateOpenCityLandmarks(Scene scene, Transform buildingRoot)
        {
            string versatile = "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/";
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "factory_building_big.prefab", "Industrial_Backdrop_A", new Vector3(34f, 0f, 30f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.2f);
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "factory_building_small.prefab", "Industrial_Backdrop_B", new Vector3(-34f, 0f, 30f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.2f);
            InstantiateDecorPrefab(scene, buildingRoot, versatile + "factory_chimney-stalk.prefab", "Industrial_Chimney", new Vector3(30f, 0f, 34f), Quaternion.identity, Vector3.one * 1.3f);
        }

        private static void CreateOpenCityInteractables(Scene scene, Transform interactionRoot, CrimeSceneSystemsRoot systemsRoot)
        {
            Transform clueRoot = FindOrCreateChild(interactionRoot, "UrbanExplorationClues");

            CreateBloodMarker(clueRoot, "Clue_BloodTrail_Crosswalk", new Vector3(1.6f, 0.03f, -1.4f), new Vector3(0.45f, 0.01f, 0.18f));

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/POLYGON city pack/Prefabs/Props/shopping cart prefab.prefab",
                "Evidence_ShoppingCart",
                new Vector3(18f, 0f, 18f),
                Quaternion.identity,
                Vector3.one,
                "evidence.shoppingcart.urban",
                "Carrito abandonado",
                "Pista urbana",
                "Carrito abandonado en el borde del bloque. Puede contener rastros de desplazamiento o abandono apresurado.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/POLYGON city pack/Prefabs/Props/trashBag prefab.prefab",
                "Evidence_TrashBag",
                new Vector3(8.2f, 0f, 7.8f),
                Quaternion.identity,
                Vector3.one,
                "evidence.trashbag.urban",
                "Bolsa sospechosa",
                "Pista urbana",
                "Bolsa encontrada cerca del cruce principal. Podria ocultar objetos descartados del incidente.");

            if (systemsRoot != null && systemsRoot.PlayerRig != null)
                CreateScenarioSpawnMarker(systemsRoot.PlayerRig.transform, new Vector3(0f, 0f, -6f));
        }

        private static void CreateOpenCityWeaponEvidence(Scene scene, Transform interactionRoot, CrimeSceneSystemsRoot systemsRoot)
        {
            Transform clueRoot = FindOrCreateChild(interactionRoot, "UrbanWeaponEvidence");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Nokobot/Modern Guns - Handgun/_Prefabs/Handgun Black/M1911 Handgun_Black.prefab",
                "Evidence_Gun_01",
                new Vector3(6.8f, 0.12f, -4.1f),
                Quaternion.Euler(0f, 24f, 82f),
                Vector3.one,
                "evidence.gun.001",
                "Pistola M1911 Negra",
                "Arma de fuego",
                "Pistola hallada cerca de la interseccion central. Posible arma vinculada al incidente.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Nokobot/Modern Guns - Handgun/_Prefabs/Handgun Silver/M1911 Handgun_Silver.prefab",
                "Evidence_Gun_02",
                new Vector3(-13.7f, 0.12f, 6.5f),
                Quaternion.Euler(0f, -38f, 95f),
                Vector3.one,
                "evidence.gun.002",
                "Pistola M1911 Plateada",
                "Arma de fuego",
                "Pistola secundaria encontrada junto a una zona residencial. Debe levantarse para analisis balistico.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Low Poly Stylized Knife Pack/Prefabs/1mat/Knife03_1mat.prefab",
                "Evidence_Knife_01",
                new Vector3(18.4f, 0.08f, 17.5f),
                Quaternion.Euler(82f, 12f, 18f),
                Vector3.one * 1.15f,
                "evidence.knife.001",
                "Cuchillo Urbano",
                "Arma blanca",
                "Cuchillo abandonado cerca del puesto callejero. Potencial evidencia de agresion.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Low Poly Stylized Knife Pack/Prefabs/1mat/Knife07_1mat.prefab",
                "Evidence_Knife_02",
                new Vector3(-20.2f, 0.08f, 16.4f),
                Quaternion.Euler(86f, -34f, -12f),
                Vector3.one * 1.12f,
                "evidence.knife.002",
                "Cuchillo de hoja larga",
                "Arma blanca",
                "Arma blanca ubicada junto al borde comercial. Debe preservarse por huellas y residuos.");

            CreateBulletCluster(scene, clueRoot, "BulletCluster_A", new Vector3(7.2f, 0.04f, -4.5f),
                "Assets/Nokobot/Modern Guns - Handgun/_Prefabs/45ACP Bullet_Casing.prefab", 4, 0.14f, "evidence.bullets.a");
            CreateBulletCluster(scene, clueRoot, "BulletCluster_B", new Vector3(-14.4f, 0.04f, 6.8f),
                "Assets/DuNguyn/Bullets Pack/Prefabs/SM_Bullet_03.prefab", 5, 0.12f, "evidence.bullets.b");
            CreateBulletCluster(scene, clueRoot, "BulletCluster_C", new Vector3(20.5f, 0.04f, -13.2f),
                "Assets/DuNguyn/Bullets Pack/Prefabs/SM_Bullet_08.prefab", 3, 0.16f, "evidence.bullets.c");

            GroundWeaponEvidence(clueRoot);
        }

        private static void CreateBulletCluster(Scene scene, Transform parent, string clusterName, Vector3 center, string prefabPath, int count, float spacing, string baseId)
        {
            Transform clusterRoot = FindOrCreateChild(parent, clusterName);
            if (clusterRoot.childCount > 0)
                return;

            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / Mathf.Max(1, count));
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * spacing;
                EnsureEvidenceObject(scene, clusterRoot,
                    prefabPath,
                    $"{clusterName}_Bullet_{i + 1}",
                    center + offset,
                    Quaternion.Euler(90f, angle * 1.7f, 0f),
                    Vector3.one * 0.9f,
                    $"{baseId}.{i + 1}",
                    $"Casquillo {i + 1}",
                    "Municion",
                    "Municion o casquillo recuperado del pavimento. Importante para reconstruccion balistica.");
            }
        }

        private static void GroundWeaponEvidence(Transform clueRoot)
        {
            if (clueRoot == null)
                return;

            foreach (Transform child in clueRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == clueRoot)
                    continue;

                if (child.name.StartsWith("Evidence_") || child.name.Contains("_Bullet_"))
                    DropObjectToGround(child, 8f);
            }
        }

        private static void EnsureOpenCitySceneCollisions(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                EnsureCollisionsRecursive(roots[i].transform);
        }

        private static void OptimizeOpenCitySceneCollisions(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                OptimizeCollisionRecursive(roots[i].transform);
        }

        private static void EnsureCollisionsRecursive(Transform target)
        {
            if (target == null)
                return;

            string name = target.name.ToLowerInvariant();
            bool isEvidence = name.StartsWith("evidence_") || name.Contains("_bullet_") || name.StartsWith("urbanweapon");
            bool isPlayer = name.Contains("xr_playerrig") || name.Contains("xr interaction manager") || name.Contains("eventsystem");

            if (!isEvidence && !isPlayer)
            {
                Renderer renderer = target.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Collider collider = target.GetComponent<Collider>();
                    if (collider == null)
                    {
                        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            MeshCollider meshCollider = target.gameObject.AddComponent<MeshCollider>();
                            meshCollider.sharedMesh = meshFilter.sharedMesh;
                            meshCollider.convex = false;
                        }
                        else
                        {
                            BoxCollider boxCollider = target.gameObject.AddComponent<BoxCollider>();
                            boxCollider.center = renderer.localBounds.center;
                            boxCollider.size = renderer.localBounds.size;
                        }
                    }
                }

                bool shouldHaveRigidbody = name.Contains("building") || name.Contains("house") || name.Contains("station") ||
                                           name.Contains("hospital") || name.Contains("garage") || name.Contains("supermarket") ||
                                           name.Contains("busstop") || name.Contains("phonebooth") || name.Contains("mailbox") ||
                                           name.Contains("rustycar") || name.Contains("tree") || name.Contains("oak");

                if (shouldHaveRigidbody && target.GetComponent<Rigidbody>() == null)
                {
                    Rigidbody rigidbody = target.gameObject.AddComponent<Rigidbody>();
                    rigidbody.isKinematic = true;
                    rigidbody.useGravity = false;
                }
            }

            for (int i = 0; i < target.childCount; i++)
                EnsureCollisionsRecursive(target.GetChild(i));
        }

        private static void OptimizeCollisionRecursive(Transform target)
        {
            if (target == null)
                return;

            string name = target.name.ToLowerInvariant();
            bool isPlayer = name.Contains("xr_playerrig") || name.Contains("xr interaction manager") || name.Contains("eventsystem");
            bool isEvidence = name.StartsWith("evidence_") || name.Contains("_bullet_") || name.Contains("urbanweapon");

            if (isPlayer || isEvidence)
            {
                for (int i = 0; i < target.childCount; i++)
                    OptimizeCollisionRecursive(target.GetChild(i));
                return;
            }

            bool isMajorStatic =
                name.StartsWith("block_office_") ||
                name.StartsWith("house_row_") ||
                name.StartsWith("landmark_") ||
                name.StartsWith("industrial_") ||
                name.StartsWith("oaktree_") ||
                name.StartsWith("rustycar_") ||
                name.StartsWith("boundary_") ||
                name.StartsWith("road_") ||
                name.StartsWith("sidewalk_") ||
                name.Contains("busstop") ||
                name.Contains("phonebooth") ||
                name.Contains("mailbox") ||
                name.Contains("streetsellerstand");

            if (isMajorStatic)
            {
                RemoveChildColliders(target);
                EnsureOptimizedRootCollider(target);
                EnsureKinematicOnlyIfNeeded(target, name);
                return;
            }

            bool isMinorStatic =
                name.Contains("lamp_") ||
                name.Contains("trafficlight") ||
                name.Contains("sign_") ||
                name.Contains("hydrant") ||
                name.Contains("trash") ||
                name.Contains("parkingmeter") ||
                name.Contains("parkingbarrier") ||
                name.Contains("bench_") ||
                name.Contains("hedge") ||
                name.Contains("bushmass") ||
                name.Contains("flowers_");

            if (isMinorStatic)
            {
                ReplaceWithSimpleCollider(target);
                RemoveRigidbody(target.gameObject);
            }

            for (int i = 0; i < target.childCount; i++)
                OptimizeCollisionRecursive(target.GetChild(i));
        }

        private static void RemoveChildColliders(Transform root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                    continue;

                if (colliders[i].transform != root)
                    Object.DestroyImmediate(colliders[i]);
            }
        }

        private static void EnsureOptimizedRootCollider(Transform root)
        {
            Collider existing = root.GetComponent<Collider>();
            if (existing is MeshCollider)
                Object.DestroyImmediate(existing);

            ReplaceWithSimpleCollider(root);
        }

        private static void ReplaceWithSimpleCollider(Transform target)
        {
            if (target == null)
                return;

            Bounds bounds = CalculateHierarchyBounds(target);
            if (bounds.size.sqrMagnitude <= 0.0001f)
                return;

            Collider existing = target.GetComponent<Collider>();
            if (existing != null)
                Object.DestroyImmediate(existing);

            BoxCollider collider = target.gameObject.AddComponent<BoxCollider>();
            collider.center = target.InverseTransformPoint(bounds.center);
            Vector3 localSize = target.InverseTransformVector(bounds.size);
            collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        private static void EnsureKinematicOnlyIfNeeded(Transform target, string lowerName)
        {
            bool shouldKeepBody =
                lowerName.StartsWith("rustycar_") ||
                lowerName.Contains("busstop") ||
                lowerName.Contains("phonebooth") ||
                lowerName.Contains("mailbox");

            if (shouldKeepBody)
            {
                Rigidbody rigidbody = target.GetComponent<Rigidbody>();
                if (rigidbody == null)
                    rigidbody = target.gameObject.AddComponent<Rigidbody>();

                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }
            else
            {
                RemoveRigidbody(target.gameObject);
            }
        }

        private static void RemoveRigidbody(GameObject target)
        {
            if (target == null)
                return;

            Rigidbody rigidbody = target.GetComponent<Rigidbody>();
            if (rigidbody != null)
                Object.DestroyImmediate(rigidbody);
        }

        private static void CreateScenarioSpawnMarker(Transform rigRoot, Vector3 position)
        {
            rigRoot.position = position;
            rigRoot.rotation = Quaternion.identity;
        }

        private static void PlaceRoadTile(Scene scene, Transform parent, string prefabPath, string objectName, Vector3 position, Quaternion rotation)
        {
            InstantiateDecorPrefab(scene, parent, prefabPath, objectName, position, rotation, Vector3.one);
        }

        private static void GroundOpenCityObjects(Scene scene)
        {
            string[] namesToGround =
            {
                "RustyCar_01", "RustyCar_02", "RustyCar_03", "RustyCar_04", "RustyCar_05",
                "BusStop_Main", "Bench_BusStop", "PhoneBooth_Corner", "Mailbox_Center", "Hydrant_01",
                "TrashCan_01", "TrashBag_01", "ParkingMeter_01", "ParkingBarrier_01", "Street_ShoppingCart",
                "StreetSellerStand", "Flowers_01", "BushMass_01", "Hedge_01", "Hedge_02"
            };

            for (int i = 0; i < namesToGround.Length; i++)
            {
                GameObject target = GameObject.Find(namesToGround[i]);
                if (target != null)
                    DropObjectToGround(target.transform, 20f);
            }

            GameObject[] oakTrees = GameObject.FindGameObjectsWithTag("Untagged");
            for (int i = 0; i < oakTrees.Length; i++)
            {
                if (oakTrees[i].name.StartsWith("OakTree_"))
                    DropObjectToGround(oakTrees[i].transform, 30f);
            }
        }

        private static void DropObjectToGround(Transform target, float rayHeight)
        {
            if (target == null)
                return;

            Bounds bounds = CalculateHierarchyBounds(target);
            Vector3 origin = new Vector3(bounds.center.x, bounds.max.y + rayHeight, bounds.center.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayHeight * 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                float yOffset = target.position.y - bounds.min.y;
                Vector3 position = target.position;
                position.y = hit.point.y + yOffset;
                target.position = position;
            }
        }

        private static Bounds CalculateHierarchyBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(target.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static void ExpandUrbanPerimeter(Transform environmentRoot, Transform decorRoot)
        {
            Material asphalt = CreateOrLoadMaterial("MAT_Asphalt_Dark", new Color(0.11f, 0.12f, 0.13f));
            Material perimeter = CreateOrLoadMaterial("MAT_Perimeter_Red", new Color(0.36f, 0.03f, 0.03f));
            Material wall = CreateOrLoadMaterial("MAT_Urban_Wall", new Color(0.24f, 0.26f, 0.29f));

            Transform floor = environmentRoot.Find("Floor_Main");
            if (floor != null)
            {
                floor.localScale = new Vector3(10f, 1f, 10f);
                Renderer floorRenderer = floor.GetComponent<Renderer>();
                if (floorRenderer != null)
                    floorRenderer.sharedMaterial = asphalt;
            }

            CreateCeiling(environmentRoot, CreateOrLoadMaterial("MAT_Ceiling_Thin", new Color(0.1f, 0.1f, 0.11f)));

            CreateWall(environmentRoot, "Perimeter_North", new Vector3(0f, 0.2f, 8.7f), new Vector3(12f, 0.4f, 0.2f), perimeter);
            CreateWall(environmentRoot, "Perimeter_South", new Vector3(0f, 0.2f, -8.7f), new Vector3(12f, 0.4f, 0.2f), perimeter);
            CreateWall(environmentRoot, "Perimeter_East", new Vector3(8.7f, 0.2f, 0f), new Vector3(0.2f, 0.4f, 12f), perimeter);
            CreateWall(environmentRoot, "Perimeter_West", new Vector3(-8.7f, 0.2f, 0f), new Vector3(0.2f, 0.4f, 12f), perimeter);

            CreateWall(environmentRoot, "Street_Backdrop_North", new Vector3(0f, 2.8f, 9.4f), new Vector3(18f, 5.6f, 0.4f), wall);
            CreateWall(environmentRoot, "Street_Backdrop_South", new Vector3(0f, 2.8f, -9.4f), new Vector3(18f, 5.6f, 0.4f), wall);
            CreateWall(environmentRoot, "Street_Backdrop_East", new Vector3(9.4f, 2.8f, 0f), new Vector3(0.4f, 5.6f, 18f), wall);
            CreateWall(environmentRoot, "Street_Backdrop_West", new Vector3(-9.4f, 2.8f, 0f), new Vector3(0.4f, 5.6f, 18f), wall);

            CreatePartition(decorRoot, "Urban_Alley_Blocker", new Vector3(-3.4f, 1.5f, 4.8f), new Vector3(0.2f, 3f, 5.2f), wall);
            CreatePartition(decorRoot, "Urban_Storage_Blocker", new Vector3(4.9f, 1.5f, 0.1f), new Vector3(5.6f, 3f, 0.18f), wall);
        }

        private static void CreateForensicOperationsZone(Scene scene, Transform decorRoot)
        {
            Transform operationsRoot = FindOrCreateChild(decorRoot, "OperationsZone");

            InstantiateDecorPrefab(scene, operationsRoot, "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Furniture/TableRectangleMedium.prefab",
                "Ops_Table_Main", new Vector3(-1.8f, 0f, -3.1f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.2f);
            InstantiateDecorPrefab(scene, operationsRoot, "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Furniture/ShelvesTallA.prefab",
                "Ops_Shelf_A", new Vector3(-4.1f, 0f, -4.2f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, operationsRoot, "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Furniture/ShelvesMediumA.prefab",
                "Ops_Shelf_B", new Vector3(-4.2f, 0f, -1.8f), Quaternion.identity, Vector3.one);
            InstantiateDecorPrefab(scene, operationsRoot, "Assets/Simple Garage/Prefabs/Big shelf.prefab",
                "Ops_ArchiveShelf", new Vector3(5.6f, 0f, 4.2f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, operationsRoot, "Assets/Simple Garage/Prefabs/Opened locker.prefab",
                "Ops_OpenLocker", new Vector3(6.3f, 0f, 2.4f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            InstantiateDecorPrefab(scene, operationsRoot, "Assets/WC/Prefabs/props/SM_first_aid_kit.prefab",
                "Ops_FirstAid", new Vector3(-5.2f, 1.4f, -2.9f), Quaternion.identity, Vector3.one);
        }

        private static void CreateUrbanEvidenceCluster(Scene scene, Transform interactionRoot)
        {
            Transform clueRoot = FindOrCreateChild(interactionRoot, "DocumentClues");

            CreateBloodMarker(clueRoot, "Clue_BloodPool_Main", new Vector3(0.65f, 0.02f, 0.95f), new Vector3(1.1f, 0.01f, 0.7f));
            CreateBloodMarker(clueRoot, "Clue_BloodTrail_Alley", new Vector3(-2.1f, 0.02f, 3.65f), new Vector3(0.9f, 0.01f, 0.18f));

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/Simple Garage/Prefabs/Black suitcase.prefab",
                "Evidence_CaseFile_Suitcase",
                new Vector3(4.7f, 0.02f, 3.25f),
                Quaternion.Euler(0f, 160f, 0f),
                Vector3.one,
                "evidence.casefile.suitcase",
                "Maletin de Evidencia",
                "Contenedor",
                "Maletin hallado cerca del punto de arrastre. Puede contener documentos o herramienta de coaccion.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Props/Mug.prefab",
                "Evidence_Mug_WitnessDesk",
                new Vector3(-1.8f, 0.93f, -3.05f),
                Quaternion.Euler(0f, 22f, 0f),
                Vector3.one,
                "evidence.mug.witness",
                "Taza en mesa de trabajo",
                "Objeto",
                "Taza abandonada en la mesa operativa. Posible fuente secundaria de huellas o ADN.");

            EnsureEvidenceObject(scene, clueRoot,
                "Assets/UnityTechnologies/Basic Asset Pack Interior/Prefabs/Props/Books.prefab",
                "Evidence_CaseNotebook",
                new Vector3(-1.35f, 0.93f, -3.1f),
                Quaternion.Euler(0f, -28f, 0f),
                Vector3.one,
                "evidence.notebook.case",
                "Bitacora de escena",
                "Documento",
                "Bitacora con anotaciones preliminares del perimetro. Debe inspeccionarse antes de descartar lineas falsas.");
        }

        private static void CreateScenarioHudCanvas(Scene scene)
        {
            GameObject existing = GameObject.Find("ScenarioHudCanvas");
            if (existing != null)
                return;

            GameObject canvasObject = new GameObject("ScenarioHudCanvas");
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            CreateImage(canvasObject.transform, "TopStrip", new Color(0.05f, 0.05f, 0.07f, 0.82f), new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            CreateImage(canvasObject.transform, "LeftPanel", new Color(0.08f, 0.08f, 0.1f, 0.72f), new Vector2(0.02f, 0.58f), new Vector2(0.27f, 0.9f), Vector2.zero, Vector2.zero);

            TMP_Text caseText = CreateText(canvasObject.transform, "HudCase", new Vector2(0.03f, 0.945f), new Vector2(0.45f, 0.99f), 22, FontStyles.Bold, TextAlignmentOptions.Left);
            caseText.text = "San Jose Centro  |  Expediente 506";

            TMP_Text statusText = CreateText(canvasObject.transform, "HudStatus", new Vector2(0.035f, 0.8f), new Vector2(0.25f, 0.88f), 18, FontStyles.Bold, TextAlignmentOptions.Left);
            statusText.text = "Modo de analisis activo";

            TMP_Text objectiveText = CreateText(canvasObject.transform, "HudObjective", new Vector2(0.035f, 0.62f), new Vector2(0.25f, 0.79f), 16, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            objectiveText.text = "Objetivo:\n- Asegurar perimetro\n- Inspeccionar manchas\n- Registrar evidencias";
        }

        private static void CreateInvestigatorViewRig(VRPlayerRigReferences rigReferences)
        {
            if (rigReferences == null || rigReferences.PlayerCamera == null)
                return;

            Transform existing = rigReferences.PlayerCamera.transform.Find("InvestigatorViewRig");
            if (existing != null)
                return;

            GameObject root = new GameObject("InvestigatorViewRig");
            root.transform.SetParent(rigReferences.PlayerCamera.transform, false);
            root.transform.localPosition = new Vector3(0f, -0.34f, 0.34f);
            root.transform.localRotation = Quaternion.identity;

            GameObject cameraBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cameraBody.name = "Body";
            cameraBody.transform.SetParent(root.transform, false);
            cameraBody.transform.localScale = new Vector3(0.18f, 0.1f, 0.08f);
            cameraBody.GetComponent<Renderer>().sharedMaterial = CreateOrLoadMaterial("MAT_Investigator_Camera", new Color(0.08f, 0.08f, 0.09f));

            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.name = "Lens";
            lens.transform.SetParent(root.transform, false);
            lens.transform.localPosition = new Vector3(0f, 0f, 0.065f);
            lens.transform.localScale = new Vector3(0.045f, 0.035f, 0.045f);
            lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lens.GetComponent<Renderer>().sharedMaterial = CreateOrLoadEmissiveMaterial("MAT_Investigator_Lens", new Color(0.06f, 0.06f, 0.08f), new Color(0.16f, 0.34f, 0.5f));

            GameObject leftHand = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftHand.name = "LeftHand";
            leftHand.transform.SetParent(root.transform, false);
            leftHand.transform.localPosition = new Vector3(-0.14f, -0.02f, -0.02f);
            leftHand.transform.localRotation = Quaternion.Euler(78f, -18f, 88f);
            leftHand.transform.localScale = new Vector3(0.08f, 0.14f, 0.08f);

            GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightHand.name = "RightHand";
            rightHand.transform.SetParent(root.transform, false);
            rightHand.transform.localPosition = new Vector3(0.14f, -0.02f, -0.02f);
            rightHand.transform.localRotation = Quaternion.Euler(78f, 18f, -88f);
            rightHand.transform.localScale = new Vector3(0.08f, 0.14f, 0.08f);

            Material gloveMaterial = CreateOrLoadMaterial("MAT_Investigator_Glove", new Color(0.14f, 0.15f, 0.17f));
            leftHand.GetComponent<Renderer>().sharedMaterial = gloveMaterial;
            rightHand.GetComponent<Renderer>().sharedMaterial = gloveMaterial;

            Object.DestroyImmediate(cameraBody.GetComponent<Collider>());
            Object.DestroyImmediate(lens.GetComponent<Collider>());
            Object.DestroyImmediate(leftHand.GetComponent<Collider>());
            Object.DestroyImmediate(rightHand.GetComponent<Collider>());

            InvestigatorViewRig viewRig = root.AddComponent<InvestigatorViewRig>();
            SerializedObject serializedRig = new SerializedObject(viewRig);
            serializedRig.FindProperty("visualsRoot").objectReferenceValue = root.transform;
            serializedRig.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureUVReactiveClue(Scene scene)
        {
            Transform existing = GameObject.Find("Clue_BloodPrint_UV")?.transform;
            if (existing != null)
                return;

            GameObject interactionRoot = GameObject.Find("Interaction");
            if (interactionRoot == null)
            {
                interactionRoot = new GameObject("Interaction");
                SceneManager.MoveGameObjectToScene(interactionRoot, scene);
            }

            GameObject clue = GameObject.CreatePrimitive(PrimitiveType.Quad);
            clue.name = "Clue_BloodPrint_UV";
            clue.transform.SetParent(interactionRoot.transform, false);
            clue.transform.position = new Vector3(2.8f, 1.1f, 1.8f);
            clue.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            clue.transform.localScale = new Vector3(0.42f, 0.28f, 1f);
            clue.GetComponent<Renderer>().sharedMaterial = CreateOrLoadEmissiveMaterial(
                "MAT_UVClue_Print",
                new Color(0f, 0f, 0f, 0f),
                new Color(0.08f, 0.5f, 1f));

            Object.DestroyImmediate(clue.GetComponent<Collider>());
            clue.AddComponent<UVReactiveSurface>();
        }
    }
}
