#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BodyAtlas;

namespace BodyAtlas.Editor
{
    public static class BodyAtlasSetup
    {
        const string ScenePath = "Assets/Scenes/BodyAtlas.unity";
        const string ModelsFolder = "Assets/Models/ZAnatomy";

        [MenuItem("Body Atlas/1. Setup Scene (import layers + lighting + UI)")]
        public static void SetupScene()
        {
            EnsureFolders();
            ConfigureModelImport();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<OrbitCamera>();

            var appGo = new GameObject("BodyAtlasApp");
            var app = appGo.AddComponent<BodyAtlasApp>();
            appGo.AddComponent<LayerController>();
            appGo.AddComponent<AnatomyPicker>();
            appGo.AddComponent<HeartFocus>();
            appGo.AddComponent<BodyAtlasUI>();

            var modelsRoot = new GameObject("ModelsRoot");
            PlaceModel(modelsRoot.transform, "Regions of human body100", "Layer_Skin", ref app.skinRoot);
            PlaceModel(modelsRoot.transform, "MuscularSystem100", "Layer_Muscles", ref app.muscleRoot);
            PlaceModel(modelsRoot.transform, "VisceralSystem100", "Layer_Organs", ref app.organRoot);
            PlaceModel(modelsRoot.transform, "CardioVascular41", "Layer_Cardiovascular", ref app.cardioRoot);
            PlaceModel(modelsRoot.transform, "SkeletalSystem100", "Layer_Skeleton", ref app.skeletonRoot);
            // Joints optional merge into skeleton
            PlaceModel(modelsRoot.transform, "Joints100", "Layer_Joints", ref app.skeletonRoot, mergeIfExists: true);

            // Scale / normalize if needed
            NormalizeModels(modelsRoot.transform);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            Debug.Log("[BodyAtlas] Scene saved: " + ScenePath);
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(ModelsFolder);
            Directory.CreateDirectory("Assets/BodyAtlas/Resources/Models");
        }

        static void ConfigureModelImport()
        {
            if (!Directory.Exists(ModelsFolder)) return;
            foreach (var path in Directory.GetFiles(ModelsFolder, "*.fbx"))
            {
                string assetPath = path.Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null) continue;
                importer.globalScale = 1f;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.isReadable = true; // mesh colliders
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.animationType = ModelImporterAnimationType.None;
                importer.SaveAndReimport();
            }
        }

        static void PlaceModel(Transform parent, string assetName, string layerName, ref GameObject slot, bool mergeIfExists = false)
        {
            string[] guids = AssetDatabase.FindAssets(assetName + " t:Model");
            GameObject prefab = null;
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (!p.Contains("ZAnatomy")) continue;
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (prefab != null) break;
            }
            if (prefab == null)
            {
                // try direct path
                string direct = $"{ModelsFolder}/{assetName}.fbx";
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(direct);
            }
            if (prefab == null)
            {
                Debug.LogWarning("[BodyAtlas] Missing model: " + assetName);
                return;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = layerName;
            instance.transform.SetParent(parent, false);
            if (mergeIfExists && slot != null)
            {
                instance.transform.SetParent(slot.transform, true);
            }
            else
            {
                slot = instance;
            }
        }

        static void NormalizeModels(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            // If model is in mm or huge, scale to ~1.7m height
            float height = b.size.y;
            if (height > 5f || height < 0.3f)
            {
                float target = 1.7f;
                float s = target / Mathf.Max(height, 0.001f);
                root.localScale = root.localScale * s;
                Debug.Log($"[BodyAtlas] Scaled models by {s:F4} (was height {height:F2})");
            }
            // Recenter XZ
            renderers = root.GetComponentsInChildren<Renderer>();
            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Vector3 shift = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            root.position += shift;
        }

        [MenuItem("Body Atlas/2. Assign Materials")]
        public static void AssignMaterials()
        {
            Assign("Layer_Skin", AnatomyLayer.Skin);
            Assign("Layer_Muscles", AnatomyLayer.Muscles);
            Assign("Layer_Organs", AnatomyLayer.Organs);
            Assign("Layer_Cardiovascular", AnatomyLayer.Cardiovascular);
            Assign("Layer_Skeleton", AnatomyLayer.Skeleton);
            EditorSceneManager.MarkAllScenesDirty();
        }

        static void Assign(string name, AnatomyLayer layer)
        {
            var go = GameObject.Find(name);
            if (go == null) return;
            MaterialFactory.RestyleHierarchy(go, layer);
        }
    }
}
#endif
