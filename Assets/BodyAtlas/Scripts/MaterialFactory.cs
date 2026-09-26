using UnityEngine;
using UnityEngine.Rendering;

namespace BodyAtlas
{
    /// <summary>
    /// Readable PBR-ish materials for anatomy systems (not flat unlit pink).
    /// </summary>
    public static class MaterialFactory
    {
        static Shader _lit;

        static Shader LitShader
        {
            get
            {
                if (_lit == null)
                {
                    _lit = Shader.Find("Universal Render Pipeline/Lit");
                    if (_lit == null) _lit = Shader.Find("URP/Lit");
                    if (_lit == null) _lit = Shader.Find("Standard");
                }
                return _lit;
            }
        }

        public static Material Create(AnatomyLayer layer)
        {
            var m = new Material(LitShader);
            Color baseCol;
            float smooth;
            float metal = 0f;
            switch (layer)
            {
                case AnatomyLayer.Skin:
                    baseCol = new Color(0.82f, 0.62f, 0.52f, 0.55f);
                    smooth = 0.45f;
                    break;
                case AnatomyLayer.Muscles:
                    baseCol = new Color(0.72f, 0.22f, 0.22f, 1f);
                    smooth = 0.35f;
                    break;
                case AnatomyLayer.Organs:
                    baseCol = new Color(0.78f, 0.40f, 0.42f, 1f);
                    smooth = 0.4f;
                    break;
                case AnatomyLayer.Cardiovascular:
                    baseCol = new Color(0.75f, 0.12f, 0.15f, 1f);
                    smooth = 0.55f;
                    break;
                case AnatomyLayer.Skeleton:
                    baseCol = new Color(0.92f, 0.90f, 0.82f, 1f);
                    smooth = 0.25f;
                    break;
                default:
                    baseCol = new Color(0.7f, 0.7f, 0.7f, 1f);
                    smooth = 0.4f;
                    break;
            }
            Apply(m, baseCol, smooth, metal);
            return m;
        }

        public static Material CreateVein() 
        {
            var m = new Material(LitShader);
            Apply(m, new Color(0.25f, 0.35f, 0.75f, 1f), 0.5f, 0f);
            return m;
        }

        public static Material CreateArtery()
        {
            var m = new Material(LitShader);
            Apply(m, new Color(0.8f, 0.15f, 0.18f, 1f), 0.5f, 0f);
            return m;
        }

        static void Apply(Material m, Color c, float smooth, float metal)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 1f);
            // Slight subsurface-ish warmth via emission for muscles/organs
            if (m.HasProperty("_EmissionColor") && c.r > 0.5f && c.g < 0.5f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 0.05f);
            }
        }

        public static void RestyleHierarchy(GameObject root, AnatomyLayer layer)
        {
            if (root == null) return;
            var muscle = Create(layer);
            var artery = CreateArtery();
            var vein = CreateVein();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name.ToLowerInvariant();
                Material use = muscle;
                if (layer == AnatomyLayer.Cardiovascular)
                {
                    if (n.Contains("vein") || n.Contains("vena") || n.Contains("caval"))
                        use = vein;
                    else
                        use = artery;
                }
                // Keep array length
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = use;
                r.sharedMaterials = mats;
            }
        }
    }
}
