using UnityEngine;
using UnityEngine.Events;

namespace BodyAtlas
{
    /// <summary>
    /// Raycast mesh on tap → emission highlight + detail event.
    /// Coordinates with OrbitCamera tap-vs-drag threshold.
    /// </summary>
    public class AnatomyPicker : MonoBehaviour
    {
        public Camera cam;
        public OrbitCamera orbit;
        public LayerMask pickMask = ~0;
        public Color highlightColor = new Color(0.35f, 0.85f, 1f, 1f);
        public float emissionIntensity = 1.8f;

        public UnityEvent<string, string> onSelected; // name, note

        Renderer _selected;
        Material[] _selectedMats;
        Color[] _savedEmission;
        bool[] _hadEmission;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (orbit == null) orbit = FindFirstObjectByType<OrbitCamera>();
            if (onSelected == null) onSelected = new UnityEvent<string, string>();
        }

        void Update()
        {
            if (orbit != null && orbit.WasTap)
            {
                TryPick(orbit.LastTapScreenPos);
                orbit.ConsumeTap();
            }
        }

        public void TryPick(Vector2 screenPos)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, pickMask, QueryTriggerInteraction.Ignore))
            {
                var r = hit.collider.GetComponentInChildren<Renderer>();
                if (r == null) r = hit.collider.GetComponentInParent<Renderer>();
                Select(r, hit.collider.gameObject.name);
            }
            else
            {
                ClearSelection();
                onSelected?.Invoke("", "");
            }
        }

        public void Select(Renderer r, string rawName)
        {
            if (r == _selected) 
            {
                string n = AnatomyCatalog.CleanName(rawName);
                onSelected?.Invoke(n, AnatomyCatalog.GetNote(rawName));
                return;
            }
            ClearSelection();
            if (r == null) return;
            _selected = r;
            _selectedMats = r.materials;
            _savedEmission = new Color[_selectedMats.Length];
            _hadEmission = new bool[_selectedMats.Length];
            for (int i = 0; i < _selectedMats.Length; i++)
            {
                var m = _selectedMats[i];
                if (m == null) continue;
                if (m.HasProperty("_EmissionColor"))
                {
                    _hadEmission[i] = m.IsKeywordEnabled("_EMISSION");
                    _savedEmission[i] = m.GetColor("_EmissionColor");
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", highlightColor * emissionIntensity);
                }
                else if (m.HasProperty("_BaseColor"))
                {
                    _savedEmission[i] = m.GetColor("_BaseColor");
                    Color c = Color.Lerp(_savedEmission[i], highlightColor, 0.45f);
                    c.a = _savedEmission[i].a;
                    m.SetColor("_BaseColor", c);
                }
            }
            string name = AnatomyCatalog.CleanName(rawName);
            onSelected?.Invoke(name, AnatomyCatalog.GetNote(rawName));
        }

        public void ClearSelection()
        {
            if (_selected == null) return;
            for (int i = 0; i < _selectedMats.Length; i++)
            {
                var m = _selectedMats[i];
                if (m == null) continue;
                if (m.HasProperty("_EmissionColor"))
                {
                    m.SetColor("_EmissionColor", _savedEmission[i]);
                    if (!_hadEmission[i]) m.DisableKeyword("_EMISSION");
                }
                else if (m.HasProperty("_BaseColor"))
                {
                    m.SetColor("_BaseColor", _savedEmission[i]);
                }
            }
            _selected = null;
            _selectedMats = null;
        }
    }
}
