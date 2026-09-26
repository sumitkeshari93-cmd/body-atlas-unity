using System;
using System.Collections.Generic;
using UnityEngine;

namespace BodyAtlas
{
    public enum AnatomyLayer
    {
        Skin = 0,
        Muscles = 1,
        Organs = 2,
        Cardiovascular = 3,
        Skeleton = 4
    }

    /// <summary>
    /// Skin → Muscles → Organs → Cardiovascular → Skeleton peel with animated fade/isolate.
    /// </summary>
    public class LayerController : MonoBehaviour
    {
        [Serializable]
        public class LayerRoot
        {
            public AnatomyLayer layer;
            public GameObject root;
            [Range(0f, 1f)] public float opacity = 1f;
            public bool visible = true;
        }

        public List<LayerRoot> layers = new List<LayerRoot>();
        public float fadeSpeed = 4.5f;

        readonly Dictionary<AnatomyLayer, float> _target = new Dictionary<AnatomyLayer, float>();
        readonly Dictionary<Renderer, Material[]> _mats = new Dictionary<Renderer, Material[]>();
        readonly Dictionary<Renderer, Color[]> _baseColors = new Dictionary<Renderer, Color[]>();
        bool _isolated;
        AnatomyLayer _isolateLayer;

        public event Action LayersChanged;

        public void Register(AnatomyLayer layer, GameObject root, bool startVisible = true, float opacity = 1f)
        {
            if (root == null) return;
            var existing = layers.Find(l => l.layer == layer);
            if (existing != null)
            {
                existing.root = root;
                existing.visible = startVisible;
                existing.opacity = opacity;
            }
            else
            {
                layers.Add(new LayerRoot { layer = layer, root = root, visible = startVisible, opacity = opacity });
            }
            CacheRenderers(root);
            _target[layer] = startVisible ? opacity : 0f;
            ApplyImmediate(layer);
            LayersChanged?.Invoke();
        }

        void CacheRenderers(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (_mats.ContainsKey(r)) continue;
                var shared = r.materials; // instance
                _mats[r] = shared;
                var cols = new Color[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] != null && shared[i].HasProperty("_BaseColor"))
                        cols[i] = shared[i].GetColor("_BaseColor");
                    else if (shared[i] != null && shared[i].HasProperty("_Color"))
                        cols[i] = shared[i].GetColor("_Color");
                    else
                        cols[i] = Color.white;
                }
                _baseColors[r] = cols;
            }
        }

        void Update()
        {
            bool any = false;
            foreach (var lr in layers)
            {
                if (lr.root == null) continue;
                float want = 0f;
                if (_isolated)
                    want = lr.layer == _isolateLayer ? 1f : 0f;
                else if (lr.visible)
                    want = lr.opacity;

                float cur = lr.root.activeSelf ? GetApproxAlpha(lr) : 0f;
                float next = Mathf.MoveTowards(cur, want, fadeSpeed * Time.unscaledDeltaTime);
                if (Mathf.Abs(next - cur) > 0.0001f) any = true;
                SetLayerAlpha(lr, next);
            }
            if (any) { /* fading */ }
        }

        float GetApproxAlpha(LayerRoot lr)
        {
            foreach (var r in lr.root.GetComponentsInChildren<Renderer>(true))
            {
                if (_mats.TryGetValue(r, out var mats) && mats.Length > 0 && mats[0] != null)
                {
                    if (mats[0].HasProperty("_BaseColor")) return mats[0].GetColor("_BaseColor").a;
                    if (mats[0].HasProperty("_Color")) return mats[0].GetColor("_Color").a;
                }
            }
            return lr.root.activeSelf ? 1f : 0f;
        }

        void SetLayerAlpha(LayerRoot lr, float a)
        {
            bool on = a > 0.01f;
            if (lr.root.activeSelf != on) lr.root.SetActive(on);
            if (!on) return;

            foreach (var r in lr.root.GetComponentsInChildren<Renderer>(true))
            {
                if (!_mats.TryGetValue(r, out var mats)) continue;
                var bases = _baseColors[r];
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    Color c = bases[i];
                    c.a = a * (lr.visible || _isolated ? 1f : 0f);
                    // Keep nearly opaque for solid systems; skin/muscles can fade
                    bool translucent = a < 0.98f;
                    SetMaterialTransparent(m, translucent);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                }
            }
        }

        static void SetMaterialTransparent(Material m, bool translucent)
        {
            if (m == null) return;
            // URP Lit surface type
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", translucent ? 1f : 0f);
                if (translucent)
                {
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.renderQueue = 3000;
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }
                else
                {
                    m.SetOverrideTag("RenderType", "Opaque");
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    m.SetInt("_ZWrite", 1);
                    m.renderQueue = 2000;
                    m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }
            }
            else
            {
                var c = m.color;
                // Built-in fallback
                if (translucent)
                {
                    m.SetFloat("_Mode", 3f);
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.EnableKeyword("_ALPHABLEND_ON");
                    m.renderQueue = 3000;
                }
            }
        }

        void ApplyImmediate(AnatomyLayer layer)
        {
            var lr = layers.Find(l => l.layer == layer);
            if (lr == null) return;
            float a = lr.visible ? lr.opacity : 0f;
            SetLayerAlpha(lr, a);
        }

        public void SetVisible(AnatomyLayer layer, bool visible)
        {
            var lr = layers.Find(l => l.layer == layer);
            if (lr == null) return;
            lr.visible = visible;
            _isolated = false;
            LayersChanged?.Invoke();
        }

        public void SetOpacity(AnatomyLayer layer, float opacity)
        {
            var lr = layers.Find(l => l.layer == layer);
            if (lr == null) return;
            lr.opacity = Mathf.Clamp01(opacity);
            lr.visible = lr.opacity > 0.01f;
            _isolated = false;
            LayersChanged?.Invoke();
        }

        public void Isolate(AnatomyLayer layer)
        {
            _isolated = true;
            _isolateLayer = layer;
            LayersChanged?.Invoke();
        }

        public void ClearIsolate()
        {
            _isolated = false;
            LayersChanged?.Invoke();
        }

        public bool IsVisible(AnatomyLayer layer)
        {
            var lr = layers.Find(l => l.layer == layer);
            return lr != null && lr.visible && !_isolated || (_isolated && _isolateLayer == layer);
        }

        public GameObject GetRoot(AnatomyLayer layer)
        {
            var lr = layers.Find(l => l.layer == layer);
            return lr?.root;
        }
    }
}
