using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BodyAtlas
{
    /// <summary>
    /// Curated medical-student notes keyed by cleaned mesh/part names.
    /// Fallback: humanized mesh name.
    /// </summary>
    public static class AnatomyCatalog
    {
        static readonly Dictionary<string, string> Notes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Heart / cardio
            { "heart", "Muscular pump with four chambers. Deoxygenated blood enters RA → RV → lungs; oxygenated blood LA → LV → aorta." },
            { "right atrium", "Receives deoxygenated blood from SVC/IVC; empties to RV through tricuspid valve." },
            { "left atrium", "Receives oxygenated blood from pulmonary veins; empties to LV through mitral valve." },
            { "right ventricle", "Pumps deoxygenated blood to lungs via pulmonary trunk; thinner wall than LV." },
            { "left ventricle", "Pumps oxygenated blood to systemic circulation; thickest myocardium." },
            { "aorta", "Largest artery; ascending, arch, descending. Coronary arteries arise from aortic sinuses." },
            { "pulmonary artery", "Carries deoxygenated blood from RV to lungs (unique among arteries)." },
            { "pulmonary vein", "Returns oxygenated blood from lungs to LA (unique among veins)." },
            { "vena cava", "SVC/IVC return systemic venous blood to RA." },
            { "superior vena cava", "Drains upper body venous blood into RA." },
            { "inferior vena cava", "Drains lower body venous blood into RA." },
            { "coronary", "Coronary arteries supply myocardium; occlusion → myocardial infarction." },
            { "tricuspid", "AV valve between RA and RV; three cusps." },
            { "mitral", "Bicuspid AV valve between LA and LV." },
            { "aortic valve", "Semilunar valve between LV and aorta." },
            { "pulmonary valve", "Semilunar valve between RV and pulmonary trunk." },

            // Skeleton highlights
            { "femur", "Longest bone; hip to knee. Head articulates with acetabulum." },
            { "tibia", "Medial leg bone; weight-bearing; forms medial malleolus." },
            { "fibula", "Lateral leg bone; not primary weight-bearing; lateral malleolus." },
            { "humerus", "Upper arm bone; articulates with glenoid and elbow." },
            { "radius", "Lateral forearm bone; rotates in pronation/supination." },
            { "ulna", "Medial forearm bone; olecranon forms elbow tip." },
            { "scapula", "Shoulder blade; glenoid fossa for humeral head." },
            { "clavicle", "Collar bone; strut between sternum and acromion." },
            { "sternum", "Manubrium, body, xiphoid; articulates with clavicles and ribs." },
            { "rib", "12 pairs; protect thorax; true (1–7), false (8–10), floating (11–12)." },
            { "vertebra", "Cervical 7 / Thoracic 12 / Lumbar 5 + sacrum + coccyx." },
            { "skull", "Cranial vault + facial bones; protects brain." },
            { "pelvis", "Ilium, ischium, pubis; transmits weight to lower limbs." },
            { "patella", "Sesamoid bone in quadriceps tendon; improves leverage." },
            { "calcaneus", "Heel bone; insertion of Achilles tendon." },

            // Muscles
            { "biceps brachii", "Flexes elbow, supinates forearm; musculocutaneous nerve." },
            { "triceps brachii", "Extends elbow; radial nerve." },
            { "deltoid", "Abducts arm (middle fibers); axillary nerve." },
            { "pectoralis major", "Adducts and medially rotates arm; clavicular + sternocostal heads." },
            { "latissimus dorsi", "Adducts, extends, medially rotates arm; thoracodorsal nerve." },
            { "trapezius", "Elevates/retracts/depresses scapula; CN XI." },
            { "rectus abdominis", "Flexes trunk; 'six-pack'; tendinous intersections." },
            { "gluteus maximus", "Powerful hip extensor; inferior gluteal nerve." },
            { "quadriceps", "Knee extensors; femoral nerve. Vastus + rectus femoris." },
            { "hamstring", "Knee flexors / hip extensors; tibial portion of sciatic." },
            { "gastrocnemius", "Plantarflexes ankle; tibial nerve; forms Achilles with soleus." },
            { "soleus", "Plantarflexes ankle (postural); tibial nerve." },
            { "diaphragm", "Primary muscle of inspiration; phrenic nerve (C3–C5)." },
            { "sternocleidomastoid", "Rotates/flexes head; CN XI." },
            { "masseter", "Powerful jaw closer; CN V3." },

            // Viscera
            { "liver", "Largest solid organ; RUQ; metabolism, bile, detox. Porta hepatis triad." },
            { "lung", "Gas exchange; right 3 lobes, left 2. Pleura and hilum landmarks." },
            { "stomach", "LUQ; cardia, fundus, body, pylorus. Digestion and intrinsic factor." },
            { "kidney", "Retroperitoneal; filters blood, urine, renin. Hilum vessels + ureter." },
            { "spleen", "LUQ lymphoid organ; filters blood; vulnerable to trauma." },
            { "pancreas", "Exocrine enzymes + endocrine islets; head in C-loop of duodenum." },
            { "intestine", "Small: duodenum/jejunum/ileum. Large: colon, absorption & flora." },
            { "colon", "Ascending/transverse/descending/sigmoid → rectum." },
            { "bladder", "Stores urine; detrusor muscle; trigone landmarks." },
            { "esophagus", "Muscular tube pharynx → stomach; upper/lower sphincters." },
            { "trachea", "C-shaped cartilage rings; bifurcates at carina (T4/5)." },
            { "thyroid", "Endocrine gland; T3/T4/calcitonin; wraps trachea." },
            { "brain", "Cerebrum, cerebellum, brainstem; higher function and vitals." },
        };

        static readonly Regex Cleanup = new Regex(@"[_\.\d]+", RegexOptions.Compiled);
        static readonly Regex MultiSpace = new Regex(@"\s+", RegexOptions.Compiled);

        public static string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Unknown structure";
            string s = raw;
            // Strip common FBX / Blender suffixes
            int idx = s.LastIndexOf('/');
            if (idx >= 0) s = s.Substring(idx + 1);
            s = s.Replace("(Clone)", "").Replace("100", "");
            s = Cleanup.Replace(s, " ");
            s = s.Replace('-', ' ');
            s = MultiSpace.Replace(s, " ").Trim();
            if (s.Length == 0) return raw;
            // Title-ish
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        public static string GetNote(string rawName)
        {
            string cleaned = CleanName(rawName);
            string key = cleaned.ToLowerInvariant();
            if (Notes.TryGetValue(key, out var n)) return n;

            // Fuzzy contains match (prefer longer keys)
            string best = null;
            int bestLen = 0;
            foreach (var kv in Notes)
            {
                if (key.Contains(kv.Key) && kv.Key.Length > bestLen)
                {
                    best = kv.Value;
                    bestLen = kv.Key.Length;
                }
            }
            if (best != null) return best;
            return $"{cleaned} — anatomical structure from the Z-Anatomy atlas. Explore related layers with the peel controls.";
        }

        public static bool LooksLikeHeart(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string k = name.ToLowerInvariant();
            return k.Contains("heart") || k.Contains("atrium") || k.Contains("ventricle")
                   || k.Contains("cardio") || k == "ra" || k == "la" || k == "rv" || k == "lv"
                   || k.Contains("myocard");
        }

        public static string ChamberLabel(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string k = name.ToLowerInvariant();
            if (k.Contains("right atrium") || Regex.IsMatch(k, @"\bra\b")) return "RA";
            if (k.Contains("left atrium") || Regex.IsMatch(k, @"\bla\b")) return "LA";
            if (k.Contains("right ventricle") || Regex.IsMatch(k, @"\brv\b")) return "RV";
            if (k.Contains("left ventricle") || Regex.IsMatch(k, @"\blv\b")) return "LV";
            return null;
        }
    }
}
