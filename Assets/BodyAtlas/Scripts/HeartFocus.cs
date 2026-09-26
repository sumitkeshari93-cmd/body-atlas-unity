using System.Collections.Generic;
using UnityEngine;

namespace BodyAtlas
{
    /// <summary>
    /// Fly camera to heart/cardio region; Go Inside isolates cardio and
    /// soft-hides outer walls where mesh naming supports it; RA/LA/RV/LV labels.
    /// </summary>
    public class HeartFocus : MonoBehaviour
    {
        public OrbitCamera orbit;
        public LayerController layers;
        public Transform heartAnchor;
        public float focusDistance = 0.55f;
        public bool insideMode;

        readonly List<GameObject> _hiddenOuter = new List<GameObject>();
        readonly List<GameObject> _labels = new List<GameObject>();
        bool _savedIso;
        AnatomyLayer _savedIsoLayer;

        public void FocusHeart()
        {
            EnsureAnchor();
            if (orbit != null && heartAnchor != null)
                orbit.FocusWorldPoint(heartAnchor.position, focusDistance, 15f, 22f);

            if (layers != null)
            {
                layers.SetVisible(AnatomyLayer.Skin, false);
                layers.SetVisible(AnatomyLayer.Muscles, false);
                layers.SetVisible(AnatomyLayer.Organs, true);
                layers.SetOpacity(AnatomyLayer.Organs, 0.35f);
                layers.SetVisible(AnatomyLayer.Cardiovascular, true);
                layers.SetOpacity(AnatomyLayer.Cardiovascular, 1f);
                layers.SetVisible(AnatomyLayer.Skeleton, true);
                layers.SetOpacity(AnatomyLayer.Skeleton, 0.25f);
            }
            SpawnChamberLabels();
        }

        public void ToggleGoInside()
        {
            if (!insideMode) EnterInside();
            else ExitInside();
        }

        public void EnterInside()
        {
            insideMode = true;
            FocusHeart();
            HideOuterWalls(true);
            if (orbit != null)
            {
                EnsureAnchor();
                orbit.AnimateTo(orbit.yaw, 30f, 0.28f,
                    heartAnchor.position - (orbit.target ? orbit.target.position : Vector3.zero), 0.8f);
                orbit.minDistance = 0.12f;
            }
            if (layers != null)
                layers.Isolate(AnatomyLayer.Cardiovascular);
        }

        public void ExitInside()
        {
            insideMode = false;
            HideOuterWalls(false);
            if (orbit != null)
            {
                orbit.minDistance = 0.35f;
                FocusHeart();
            }
            if (layers != null)
                layers.ClearIsolate();
        }

        void EnsureAnchor()
        {
            if (heartAnchor != null) return;
            // Search cardio / visceral for heart-like mesh
            Transform found = FindHeartTransform();
            var go = new GameObject("HeartAnchor");
            go.transform.SetParent(transform, false);
            if (found != null)
            {
                var b = BoundsOf(found);
                go.transform.position = b.center;
            }
            else
            {
                // Anatomical guess: mid-thorax slightly left
                go.transform.position = new Vector3(-0.05f, 1.25f, 0.05f);
            }
            heartAnchor = go.transform;
        }

        Transform FindHeartTransform()
        {
            GameObject cardio = layers != null ? layers.GetRoot(AnatomyLayer.Cardiovascular) : null;
            if (cardio != null)
            {
                foreach (var t in cardio.GetComponentsInChildren<Transform>(true))
                {
                    if (AnatomyCatalog.LooksLikeHeart(t.name))
                        return t;
                }
            }
            GameObject organs = layers != null ? layers.GetRoot(AnatomyLayer.Organs) : null;
            if (organs != null)
            {
                foreach (var t in organs.GetComponentsInChildren<Transform>(true))
                {
                    if (AnatomyCatalog.LooksLikeHeart(t.name))
                        return t;
                }
            }
            // Global search
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (AnatomyCatalog.LooksLikeHeart(r.name))
                    return r.transform;
            }
            return null;
        }

        static Bounds BoundsOf(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one * 0.2f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        void HideOuterWalls(bool hide)
        {
            if (!hide)
            {
                foreach (var g in _hiddenOuter)
                    if (g != null) g.SetActive(true);
                _hiddenOuter.Clear();
                return;
            }
            _hiddenOuter.Clear();
            GameObject cardio = layers != null ? layers.GetRoot(AnatomyLayer.Cardiovascular) : null;
            if (cardio == null) return;
            foreach (var t in cardio.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                // Soft "cross-section": hide pericardium / outer wall naming if present
                if (n.Contains("pericard") || n.Contains("epicard") || n.Contains("outer")
                    || n.Contains("wall_outer") || n.Contains("surface"))
                {
                    if (t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        _hiddenOuter.Add(t.gameObject);
                    }
                }
            }
        }

        void SpawnChamberLabels()
        {
            ClearLabels();
            GameObject cardio = layers != null ? layers.GetRoot(AnatomyLayer.Cardiovascular) : null;
            if (cardio == null) return;
            var placed = new HashSet<string>();
            foreach (var r in cardio.GetComponentsInChildren<Renderer>(true))
            {
                string lab = AnatomyCatalog.ChamberLabel(r.name);
                if (lab == null || placed.Contains(lab)) continue;
                placed.Add(lab);
                var label = CreateWorldLabel(lab, r.bounds.center + Vector3.up * 0.02f);
                _labels.Add(label);
            }
            // Fallback fixed RA/LA/RV/LV around anchor
            if (_labels.Count == 0 && heartAnchor != null)
            {
                Vector3 c = heartAnchor.position;
                _labels.Add(CreateWorldLabel("RA", c + new Vector3(0.06f, 0.04f, 0.02f)));
                _labels.Add(CreateWorldLabel("LA", c + new Vector3(-0.06f, 0.04f, 0.02f)));
                _labels.Add(CreateWorldLabel("RV", c + new Vector3(0.05f, -0.03f, 0.05f)));
                _labels.Add(CreateWorldLabel("LV", c + new Vector3(-0.05f, -0.03f, 0.05f)));
            }
        }

        void ClearLabels()
        {
            foreach (var g in _labels)
                if (g != null) Destroy(g);
            _labels.Clear();
        }

        GameObject CreateWorldLabel(string text, Vector3 pos)
        {
            var go = new GameObject("Label_" + text);
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = 0.01f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.85f, 0.4f, 1f);
            var bill = go.AddComponent<Billboard>();
            return go;
        }

        void OnDisable() => ClearLabels();
    }

    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
