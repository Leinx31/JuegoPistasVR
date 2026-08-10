using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrimeVR.Editor
{
    public static class QuestCleanTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Quest_Clean_Test.unity";
        private const string StarterRigPath = "Assets/XR/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        [MenuItem("Tools/Crime VR/Create Clean Quest Test Scene")]
        public static void CreateCleanQuestTestSceneMenu()
        {
            CreateCleanQuestTestScene();
            EditorUtility.DisplayDialog("Crime VR", "Escena limpia de prueba Quest creada y puesta primera en Build Settings.", "OK");
        }

        public static void CreateCleanQuestTestScene()
        {
            EnsureScenesFolder();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Quest_Clean_Test";

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientSkyColor = new Color(0.22f, 0.24f, 0.27f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.14f, 0.15f, 0.17f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.06f, 0.07f, 1f);

            CreateDirectionalLight();
            CreateFloor();
            CreateReferenceCube();
            CreateStarterRig();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        private static void CreateDirectionalLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);

            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = GetBuiltinMaterial();
        }

        private static void CreateReferenceCube()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Reference_Cube";
            cube.transform.position = new Vector3(0f, 0.5f, 3f);
            cube.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        }

        private static void CreateStarterRig()
        {
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterRigPath);
            if (rigPrefab == null)
                throw new FileNotFoundException($"No se encontro el rig base en: {StarterRigPath}");

            GameObject rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rigInstance.name = "XR_PlayerRig_Clean";
            rigInstance.transform.position = new Vector3(0f, 0.05f, 0f);
            rigInstance.transform.rotation = Quaternion.identity;
        }

        private static Material GetBuiltinMaterial()
        {
            return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        }
    }
}
