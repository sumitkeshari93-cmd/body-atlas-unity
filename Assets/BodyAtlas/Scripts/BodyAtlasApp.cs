using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace BodyAtlas
{
    /// <summary>
    /// Runtime entry: studio lighting, load Z-Anatomy layer roots, wire systems.
    /// Prefers scene-placed model roots named Layer_*; otherwise loads from Resources/Models.
    /// </summary>
    public class BodyAtlasApp : MonoBehaviour
    {
        public OrbitCamera orbit;
        public LayerController layers;
        public AnatomyPicker picker;
        public HeartFocus heart;
        public BodyAtlasUI ui;

        [Header("Model roots (optional pre-placed)")]
        public GameObject skinRoot;
        public GameObject muscleRoot;
        public GameObject organRoot;
        public GameObject cardioRoot;
        public GameObject skeletonRoot;

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.antiAliasing = 4;
            SetupStudio();
            EnsureSystems();
            ResolveOrLoadModels();
            WireLayers();
            FrameBody();
        }

        void SetupStudio()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            cam.fieldOfView = 40f;

            // Soft key + fill + rim
            EnsureLight("KeyLight", new Vector3(35f, 140f, 0f), new Color(1f, 0.96f, 0.9f), 1.15f, LightShadows.Soft);
            EnsureLight("FillLight", new Vector3(20f, -40f, 0f), new Color(0.55f, 0.65f, 0.85f), 0.45f, LightShadows.None);
            EnsureLight("RimLight", new Vector3(10f, -160f, 0f), new Color(0.7f, 0.85f, 1f), 0.55f, LightShadows.None);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.2f, 0.24f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.13f, 0.15f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.06f);
            RenderSettings.reflectionIntensity = 0.4f;
        }

        static void EnsureLight(string name, Vector3 euler, Color col, float intensity, LightShadows shadows)
        {
            var existing = GameObject.Find(name);
            Light lit;
            if (existing == null)
            {
                var go = new GameObject(name);
                lit = go.AddComponent<Light>();
            }
            else lit = existing.GetComponent<Light>() ?? existing.AddComponent<Light>();
            lit.type = LightType.Directional;
            lit.color = col;
            lit.intensity = intensity;
            lit.shadows = shadows;
            lit.transform.rotation = Quaternion.Euler(euler);
        }

        void EnsureSystems()
        {
            if (orbit == null) orbit = FindFirstObjectByType<OrbitCamera>() ?? Camera.main.gameObject.AddComponent<OrbitCamera>();
            if (layers == null) layers = FindFirstObjectByType<LayerController>() ?? gameObject.AddComponent<LayerController>();
            if (picker == null) picker = FindFirstObjectByType<AnatomyPicker>() ?? gameObject.AddComponent<AnatomyPicker>();
            if (heart == null) heart = FindFirstObjectByType<HeartFocus>() ?? gameObject.AddComponent<HeartFocus>();
            if (ui == null) ui = FindFirstObjectByType<BodyAtlasUI>() ?? gameObject.AddComponent<BodyAtlasUI>();

            picker.cam = Camera.main;
            picker.orbit = orbit;
            heart.orbit = orbit;
            heart.layers = layers;
            ui.layers = layers;
            ui.orbit = orbit;
            ui.heart = heart;
            ui.picker = picker;
        }

        void ResolveOrLoadModels()
        {
            skinRoot = skinRoot ?? GameObject.Find("Layer_Skin");
            muscleRoot = muscleRoot ?? GameObject.Find("Layer_Muscles");
            organRoot = organRoot ?? GameObject.Find("Layer_Organs");
            cardioRoot = cardioRoot ?? GameObject.Find("Layer_Cardiovascular");
            skeletonRoot = skeletonRoot ?? GameObject.Find("Layer_Skeleton");

            // If editor bootstrap placed instances under ModelsRoot
            var modelsRoot = GameObject.Find("ModelsRoot");
            if (modelsRoot != null)
            {
                foreach (Transform child in modelsRoot.transform)
                {
                    string n = child.name.ToLowerInvariant();
                    if (n.Contains("region") || n.Contains("skin")) skinRoot = skinRoot ?? child.gameObject;
                    else if (n.Contains("muscular") || n.Contains("muscle")) muscleRoot = muscleRoot ?? child.gameObject;
                    else if (n.Contains("visceral") || n.Contains("organ")) organRoot = organRoot ?? child.gameObject;
                    else if (n.Contains("cardio") || n.Contains("vascular")) cardioRoot = cardioRoot ?? child.gameObject;
                    else if (n.Contains("skeletal") || n.Contains("skeleton") || n.Contains("joint"))
                        skeletonRoot = skeletonRoot ?? child.gameObject;
                }
            }

            // Resources fallback (prefab instances)
            skinRoot = skinRoot ?? TryResource("Models/Regions");
            muscleRoot = muscleRoot ?? TryResource("Models/Muscular");
            organRoot = organRoot ?? TryResource("Models/Visceral");
            cardioRoot = cardioRoot ?? TryResource("Models/Cardio");
            skeletonRoot = skeletonRoot ?? TryResource("Models/Skeletal");
        }

        static GameObject TryResource(string path)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            return Instantiate(prefab);
        }

        void WireLayers()
        {
            if (skinRoot != null)
            {
                skinRoot.name = "Layer_Skin";
                MaterialFactory.RestyleHierarchy(skinRoot, AnatomyLayer.Skin);
                AddColliders(skinRoot);
                layers.Register(AnatomyLayer.Skin, skinRoot, true, 0.45f);
            }
            if (muscleRoot != null)
            {
                muscleRoot.name = "Layer_Muscles";
                MaterialFactory.RestyleHierarchy(muscleRoot, AnatomyLayer.Muscles);
                AddColliders(muscleRoot);
                layers.Register(AnatomyLayer.Muscles, muscleRoot, true, 1f);
            }
            if (organRoot != null)
            {
                organRoot.name = "Layer_Organs";
                MaterialFactory.RestyleHierarchy(organRoot, AnatomyLayer.Organs);
                AddColliders(organRoot);
                layers.Register(AnatomyLayer.Organs, organRoot, true, 1f);
            }
            if (cardioRoot != null)
            {
                cardioRoot.name = "Layer_Cardiovascular";
                MaterialFactory.RestyleHierarchy(cardioRoot, AnatomyLayer.Cardiovascular);
                AddColliders(cardioRoot);
                layers.Register(AnatomyLayer.Cardiovascular, cardioRoot, true, 1f);
            }
            if (skeletonRoot != null)
            {
                skeletonRoot.name = "Layer_Skeleton";
                MaterialFactory.RestyleHierarchy(skeletonRoot, AnatomyLayer.Skeleton);
                AddColliders(skeletonRoot);
                layers.Register(AnatomyLayer.Skeleton, skeletonRoot, true, 1f);
            }

            // Default peel: skin semi, muscles on, organs on, cardio on, skeleton on
            if (skinRoot == null && muscleRoot == null && skeletonRoot == null)
                CreatePlaceholderBody();
        }

        void AddColliders(GameObject root)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.GetComponent<Collider>() != null) continue;
                if (mf.sharedMesh == null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
            }
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.GetComponent<Collider>() != null) continue;
                // Bake approximate box for skinned
                var box = smr.gameObject.AddComponent<BoxCollider>();
                box.center = smr.localBounds.center;
                box.size = smr.localBounds.size;
            }
        }

        void FrameBody()
        {
            Bounds b = new Bounds(new Vector3(0, 1f, 0), new Vector3(0.6f, 1.8f, 0.4f));
            bool any = false;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            if (orbit != null)
            {
                if (orbit.target == null)
                {
                    var t = new GameObject("OrbitTarget").transform;
                    orbit.target = t;
                }
                orbit.target.position = b.center;
                float dist = Mathf.Clamp(b.extents.magnitude * 2.1f, 1.2f, 5.5f);
                orbit.resetDistance = dist;
                orbit.resetOffset = Vector3.zero;
                orbit.distance = dist;
                orbit.targetOffset = Vector3.zero;
                orbit.ResetView();
            }
        }

        void CreatePlaceholderBody()
        {
            Debug.LogWarning("[BodyAtlas] No Z-Anatomy FBX instances found — using teaching placeholders. Open in Editor and run Body Atlas → Setup Scene.");
            var root = new GameObject("PlaceholderBody");
            // Simple mannequin proxies colored by system
            skeletonRoot = MakeProxy(root.transform, "SkeletonProxy", new Vector3(0, 1f, 0), new Vector3(0.35f, 1.7f, 0.25f), AnatomyLayer.Skeleton);
            muscleRoot = MakeProxy(root.transform, "MuscleProxy", new Vector3(0, 1f, 0), new Vector3(0.4f, 1.65f, 0.3f), AnatomyLayer.Muscles);
            organRoot = MakeProxy(root.transform, "OrganProxy", new Vector3(0, 1.15f, 0.02f), new Vector3(0.28f, 0.45f, 0.2f), AnatomyLayer.Organs);
            cardioRoot = MakeProxy(root.transform, "HeartProxy", new Vector3(-0.05f, 1.3f, 0.05f), new Vector3(0.12f, 0.12f, 0.1f), AnatomyLayer.Cardiovascular);
            skinRoot = MakeProxy(root.transform, "SkinProxy", new Vector3(0, 1f, 0), new Vector3(0.42f, 1.72f, 0.32f), AnatomyLayer.Skin);

            MaterialFactory.RestyleHierarchy(skeletonRoot, AnatomyLayer.Skeleton);
            MaterialFactory.RestyleHierarchy(muscleRoot, AnatomyLayer.Muscles);
            MaterialFactory.RestyleHierarchy(organRoot, AnatomyLayer.Organs);
            MaterialFactory.RestyleHierarchy(cardioRoot, AnatomyLayer.Cardiovascular);
            MaterialFactory.RestyleHierarchy(skinRoot, AnatomyLayer.Skin);

            AddColliders(skeletonRoot);
            AddColliders(muscleRoot);
            AddColliders(organRoot);
            AddColliders(cardioRoot);
            AddColliders(skinRoot);

            layers.Register(AnatomyLayer.Skeleton, skeletonRoot, true, 1f);
            layers.Register(AnatomyLayer.Muscles, muscleRoot, true, 0.85f);
            layers.Register(AnatomyLayer.Organs, organRoot, true, 1f);
            layers.Register(AnatomyLayer.Cardiovascular, cardioRoot, true, 1f);
            layers.Register(AnatomyLayer.Skin, skinRoot, true, 0.35f);
        }

        static GameObject MakeProxy(Transform parent, string name, Vector3 pos, Vector3 scale, AnatomyLayer layer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            return go;
        }
    }
}
