#define LOTO_MODELS   // comment this line out to go back to the plain cube machine
#if LOTO_MODELS
using UnityEditor;
using UnityEngine;

namespace Loto.EditorTools
{
    /// <summary>
    /// Dresses the machine with a CC0 model from Kenney's Factory Kit (Assets/ThirdParty/Kenney/FactoryKit, see its
    /// License.txt). Only looks change: the danger zone, the energy lamp, the spinner, the label and the sounds stay
    /// the same objects, moved to fit the model. If the model file is missing, the cube machine stays as it was.
    /// </summary>
    public static partial class LotoSceneBuilder
    {
        public const string KenneyFolder = "Assets/ThirdParty/Kenney/FactoryKit";

        static partial void DressMachine(Transform machine, GameObject body)
        {
            // About 0.20 x 0.22 x 0.25 m once scaled: close to the 0.20 m cube the rest was measured against.
            GameObject model = PlaceModel("machine-window", machine, 0.25f, 0.22f, 0f);
            if (model == null)
            {
                Debug.LogWarning("[Loto] " + KenneyFolder + "/machine-window.fbx not found: the machine stays a cube.");
                return;
            }
            body.GetComponent<MeshRenderer>().enabled = false;

            // Hazard stripes on the table under the machine: the danger zone made visible.
            GameObject stripes = PlaceModel("indicator-special-lines", machine, 0.34f, 0.02f, 0f);
            if (stripes != null) stripes.transform.localPosition += new Vector3(0f, 0.001f, 0f);

            Bounds b = LocalBounds(machine, model);
            float front = b.min.z;   // the user looks along +Z: the face toward the user is the lowest Z
            float top = b.max.y;

            Transform label = FindChild(machine, "MachineLabel");
            if (label != null) label.localPosition = new Vector3(0f, b.min.y + 0.03f, front - 0.003f);
            Transform lamp = FindChild(machine, "EnergyLamp");
            if (lamp != null) lamp.localPosition = new Vector3(b.min.x + 0.03f, top - 0.03f, front - 0.008f);
            Transform spinner = FindChild(machine, "Spinner");
            if (spinner != null) spinner.localPosition = new Vector3(0f, top + 0.015f, 0f);

            // Danger zone: the model's box plus 3.5 cm on every side, never smaller than before.
            Transform zone = FindChild(machine, "DangerZone");
            BoxCollider trigger = zone != null ? zone.GetComponent<BoxCollider>() : null;
            if (trigger != null)
            {
                zone.localPosition = new Vector3(b.center.x, b.center.y, b.center.z);
                trigger.size = Vector3.Max(b.size + Vector3.one * 0.07f, new Vector3(0.27f, 0.28f, 0.27f));
            }
            Debug.Log("[Loto] Machine dressed with Kenney machine-window (" + b.size.x.ToString("F2") + " x " +
                b.size.y.ToString("F2") + " x " + b.size.z.ToString("F2") + " m).");

            // The part that turns becomes a gear (same Spinner object: DangerZone keeps turning it).
            if (spinner != null)
            {
                GameObject cog = PlaceModel("cog-a", spinner, 0.18f, 0.035f, 0f, true);
                if (cog != null)
                {
                    foreach (Transform child in spinner)
                        if (child.gameObject != cog) { var mr = child.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false; }
                    cog.transform.localPosition += new Vector3(0f, -0.012f, 0f);
                }
            }

            // A conveyor through the machine's tunnel (along X): it reads as a production line.
            GameObject belt = PlaceModel("conveyor-long", machine, 0.40f, 0.07f, 0f, false, true);
            if (belt != null) belt.transform.localPosition += new Vector3(0f, 0.002f, 0f);

            // The power cable from the back of the switch box to the back of the machine, along the back of the table
            // (behind the buttons): it shows at a glance that the lever feeds the machine.
            Transform board = machine.parent;
            if (board != null)
            {
                Material rubber = MakeMaterial("Cable", "Universal Render Pipeline/Lit", new Color(0.06f, 0.06f, 0.07f), 0.35f);
                float h = TableTop + 0.012f, backZ = MachinePosition.z + b.max.z, boxBack = LeverZ + SwitchBoxSize.z / 2f;
                Cable(board, new Vector3(LeverX, h, boxBack), new Vector3(LeverX, h, backZ), rubber);
                Cable(board, new Vector3(LeverX, h, backZ), new Vector3(MachinePosition.x, h, backZ), rubber);

                // Energy you can see: lights run along the cable while the lever is ON; the machine trembles.
                Transform flowRoot = Group("EnergyFlow", board, Vector3.zero);
                Transform p0 = Group("Path0", flowRoot, new Vector3(LeverX, h, boxBack));
                Transform p1 = Group("Path1", flowRoot, new Vector3(LeverX, h, backZ));
                Transform p2 = Group("Path2", flowRoot, new Vector3(MachinePosition.x, h, backZ));
                var flow = flowRoot.gameObject.AddComponent<EnergyFlow>();
                flow.lever = Object.FindAnyObjectByType<Lever>();
                flow.path = new[] { p0, p1, p2 };
                flow.shaker = model.transform;
            }
        }

        /// <summary>A straight cable (opaque cylinder, no collider) between two board-local points.</summary>
        static void Cable(Transform board, Vector3 from, Vector3 to, Material material)
        {
            Vector3 mid = (from + to) / 2f, dir = to - from;
            if (dir.sqrMagnitude < 0.0001f) return;
            GameObject c = Part(PrimitiveType.Cylinder, "Cable", board, mid, new Vector3(0.016f, dir.magnitude / 2f, 0.016f), material);
            c.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        }

        /// <summary>
        /// Instantiates a Kenney model under parent, without colliders or shadows, scaled so it fits maxFootprint
        /// (largest of X and Z) and maxHeight, centered on the parent's origin and standing on it. Null if missing.
        /// </summary>
        static GameObject PlaceModel(string name, Transform parent, float maxFootprint, float maxHeight, float yaw)
        {
            return PlaceModel(name, parent, maxFootprint, maxHeight, yaw, false, false);
        }

        /// <summary>
        /// lieFlat: if the model stands taller than wide (a gear modeled upright), it is laid flat first.
        /// alongX: the model's long side is turned to point along X.
        /// </summary>
        static GameObject PlaceModel(string name, Transform parent, float maxFootprint, float maxHeight, float yaw, bool lieFlat, bool alongX = false)
        {
            GameObject model = PlaceModelRaw(name, parent, maxFootprint, maxHeight, yaw);
            if (model == null) return null;
            Bounds b = LocalBounds(parent, model);
            Quaternion extra = Quaternion.identity;
            if (lieFlat && b.size.y > Mathf.Max(b.size.x, b.size.z)) extra = Quaternion.Euler(90f, 0f, 0f);
            else if (alongX && b.size.z > b.size.x) extra = Quaternion.Euler(0f, 90f, 0f);
            if (extra == Quaternion.identity) return model;
            Object.DestroyImmediate(model);
            model = PlaceModelRaw(name, parent, maxFootprint, maxHeight, yaw, extra);
            return model;
        }

        static GameObject PlaceModelRaw(string name, Transform parent, float maxFootprint, float maxHeight, float yaw)
        {
            return PlaceModelRaw(name, parent, maxFootprint, maxHeight, yaw, Quaternion.identity);
        }

        static GameObject PlaceModelRaw(string name, Transform parent, float maxFootprint, float maxHeight, float yaw, Quaternion extra)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyFolder + "/" + name + ".fbx");
            if (prefab == null) return null;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model_" + name;
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * extra;
            model.transform.localScale = Vector3.one;
            foreach (Collider c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            // Our own opaque URP materials: the kit's imported ones may not use the project's shader (pink) and its
            // "glass" is translucent, which would open holes in the passthrough (CLAUDE.md, trampas ya pisadas).
            Material colormap = KenneyColormap();
            Material glass = MakeMaterial("KenneyGlass", "Universal Render Pipeline/Lit", new Color(0.13f, 0.20f, 0.29f), 0.6f);
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                Material[] slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = slots[i] != null && slots[i].name.ToLowerInvariant().Contains("glass") ? glass : colormap;
                r.sharedMaterials = slots;
            }

            Bounds b = LocalBounds(parent, model);
            float footprint = Mathf.Max(b.size.x, b.size.z);
            if (footprint < 0.0001f || b.size.y < 0.0001f) return model;
            float k = Mathf.Min(maxFootprint / footprint, maxHeight / b.size.y);
            model.transform.localScale = Vector3.one * k;
            b = LocalBounds(parent, model);
            model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            return model;
        }

        /// <summary>Opaque URP Lit material with the kit's palette texture (Textures/colormap.png).</summary>
        static Material KenneyColormap()
        {
            Material m = MakeMaterial("KenneyColormap", "Universal Render Pipeline/Lit", Color.white, 0.15f);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(KenneyFolder + "/Textures/colormap.png");
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                m.mainTexture = tex;
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        /// <summary>The renderers' bounds of root, in parent's local space.</summary>
        static Bounds LocalBounds(Transform parent, GameObject root)
        {
            bool any = false;
            var result = new Bounds();
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                Bounds w = r.bounds;
                Vector3 min = w.min, max = w.max;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                    Vector3 local = parent.InverseTransformPoint(corner);
                    if (!any) { result = new Bounds(local, Vector3.zero); any = true; }
                    else result.Encapsulate(local);
                }
            }
            return result;
        }
    }
}
#endif
