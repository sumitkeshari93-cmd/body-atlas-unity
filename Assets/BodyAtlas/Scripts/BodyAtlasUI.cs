using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BodyAtlas
{
    /// <summary>
    /// Clean mobile uGUI: layer chips, search, detail panel, heart + reset + credits.
    /// Built entirely in code so the scene stays simple.
    /// </summary>
    public class BodyAtlasUI : MonoBehaviour
    {
        public LayerController layers;
        public OrbitCamera orbit;
        public HeartFocus heart;
        public AnatomyPicker picker;

        Text _title;
        Text _detailName;
        Text _detailNote;
        Text _credits;
        GameObject _creditsPanel;
        GameObject _detailPanel;
        InputField _search;
        readonly System.Collections.Generic.Dictionary<AnatomyLayer, Image> _chipBg =
            new System.Collections.Generic.Dictionary<AnatomyLayer, Image>();

        static readonly AnatomyLayer[] ChipOrder =
        {
            AnatomyLayer.Skin, AnatomyLayer.Muscles, AnatomyLayer.Organs,
            AnatomyLayer.Cardiovascular, AnatomyLayer.Skeleton
        };

        static readonly string[] ChipLabels = { "Skin", "Muscles", "Organs", "Cardio", "Skeleton" };

        void Start()
        {
            EnsureEventSystem();
            BuildUI();
            if (picker != null)
                picker.onSelected.AddListener(OnSelected);
            RefreshChips();
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("BodyAtlasCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Top bar
            var top = Panel(canvasGo.transform, "TopBar", new Color(0.06f, 0.08f, 0.12f, 0.82f));
            Stretch(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -96), new Vector2(0, 0));

            _title = Label(top.transform, "Body Atlas", 36, FontStyle.Bold, TextAnchor.MiddleLeft);
            Stretch(_title.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(28, 8), new Vector2(-280, -8));

            Button(top.transform, "Reset", new Vector2(1, 0.5f), new Vector2(-200, 0), () => orbit?.ResetView());
            Button(top.transform, "Heart", new Vector2(1, 0.5f), new Vector2(-110, 0), () => heart?.FocusHeart());
            Button(top.transform, "Inside", new Vector2(1, 0.5f), new Vector2(-20, 0), () => heart?.ToggleGoInside());

            // Layer chips
            var chipBar = Panel(canvasGo.transform, "LayerBar", new Color(0.05f, 0.07f, 0.1f, 0.75f));
            Stretch(chipBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -168), new Vector2(-12, -104));
            var hlg = chipBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(12, 12, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            for (int i = 0; i < ChipOrder.Length; i++)
            {
                var layer = ChipOrder[i];
                var label = ChipLabels[i];
                var chip = Chip(chipBar.transform, label, () => ToggleLayer(layer));
                _chipBg[layer] = chip.GetComponent<Image>();
            }

            // Search
            var searchPanel = Panel(canvasGo.transform, "Search", new Color(0.08f, 0.1f, 0.14f, 0.85f));
            Stretch(searchPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -230), new Vector2(-12, -176));
            _search = SearchField(searchPanel.transform);
            _search.onEndEdit.AddListener(OnSearch);

            // Detail panel bottom
            _detailPanel = Panel(canvasGo.transform, "Detail", new Color(0.07f, 0.09f, 0.13f, 0.9f)).gameObject;
            Stretch(_detailPanel.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(12, 24), new Vector2(-12, 210));
            _detailName = Label(_detailPanel.transform, "Tap a structure", 30, FontStyle.Bold, TextAnchor.UpperLeft);
            Stretch(_detailName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -48), new Vector2(-20, -12));
            _detailNote = Label(_detailPanel.transform, "Orbit: drag · Pinch: zoom · Two-finger: pan · Reset / Heart / Inside above.",
                22, FontStyle.Normal, TextAnchor.UpperLeft);
            Stretch(_detailNote.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 16), new Vector2(-20, -56));
            _detailNote.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailNote.verticalOverflow = VerticalWrapMode.Overflow;

            // Credits button + panel
            Button(canvasGo.transform, "Credits", new Vector2(0, 0), new Vector2(90, 40), ToggleCredits)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(90, 40);

            _creditsPanel = Panel(canvasGo.transform, "CreditsPanel", new Color(0.05f, 0.06f, 0.09f, 0.95f)).gameObject;
            Stretch(_creditsPanel.GetComponent<RectTransform>(), new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.8f),
                Vector2.zero, Vector2.zero);
            _credits = Label(_creditsPanel.transform,
                "Body Atlas\n\nAnatomical meshes: Z-Anatomy by Lluís Vinent Juanico\nLicense: CC BY-SA 4.0\nhttps://github.com/LluisV/Z-Anatomy\n\nOpen atlas quality (not commercial Visible Body photogrammetry).\nBuilt for medical-student layer learning.\n\nTap outside / Credits to close.",
                24, FontStyle.Normal, TextAnchor.MiddleCenter);
            Stretch(_credits.rectTransform, Vector2.zero, Vector2.one, new Vector2(24, 24), new Vector2(-24, -24));
            _credits.horizontalOverflow = HorizontalWrapMode.Wrap;
            _credits.verticalOverflow = VerticalWrapMode.Overflow;
            _creditsPanel.SetActive(false);
        }

        void ToggleLayer(AnatomyLayer layer)
        {
            if (layers == null) return;
            bool on = layers.IsVisible(layer);
            layers.ClearIsolate();
            layers.SetVisible(layer, !on);
            if (!on) layers.SetOpacity(layer, 1f);
            RefreshChips();
        }

        void RefreshChips()
        {
            foreach (var kv in _chipBg)
            {
                bool on = layers != null && layers.IsVisible(kv.Key);
                kv.Value.color = on ? new Color(0.2f, 0.55f, 0.75f, 0.95f) : new Color(0.18f, 0.2f, 0.25f, 0.9f);
            }
        }

        void OnSelected(string name, string note)
        {
            if (string.IsNullOrEmpty(name))
            {
                _detailName.text = "Tap a structure";
                _detailNote.text = "Orbit: drag · Pinch: zoom · Two-finger: pan.";
                return;
            }
            _detailName.text = name;
            _detailNote.text = note;
        }

        void OnSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return;
            q = q.Trim().ToLowerInvariant();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.name.ToLowerInvariant().Contains(q))
                {
                    picker?.Select(r, r.name);
                    if (orbit != null)
                        orbit.FocusWorldPoint(r.bounds.center, Mathf.Max(0.5f, r.bounds.extents.magnitude * 2.2f));
                    return;
                }
            }
            _detailName.text = "Not found";
            _detailNote.text = $"No mesh matching \"{q}\". Try heart, femur, liver…";
        }

        void ToggleCredits()
        {
            _creditsPanel.SetActive(!_creditsPanel.activeSelf);
        }

        // --- UI helpers ---
        static RectTransform Panel(Transform parent, string name, Color col)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = col;
            return go.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        static Text Label(Transform parent, string text, int size, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            t.raycastTarget = false;
            return t;
        }

        static Button Button(Transform parent, string label, Vector2 anchor, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(88, 52);
            rt.anchoredPosition = anchoredPos;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.42f, 0.62f, 0.95f);
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);
            var t = Label(go.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        static GameObject Chip(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Chip_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 48;
            go.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.25f, 0.9f);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            var t = Label(go.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go;
        }

        static InputField SearchField(Transform parent)
        {
            var go = new GameObject("SearchField", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(8, 6), new Vector2(-8, -6));
            go.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);
            var text = Label(go.transform, "", 24, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0));
            text.color = Color.white;
            text.raycastTarget = true;
            var placeholder = Label(go.transform, "Search anatomy (heart, femur…)", 22, FontStyle.Italic, TextAnchor.MiddleLeft);
            Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0));
            placeholder.color = new Color(0.6f, 0.65f, 0.7f, 0.8f);
            var field = go.GetComponent<InputField>();
            field.textComponent = text;
            field.placeholder = placeholder;
            return field;
        }
    }
}
