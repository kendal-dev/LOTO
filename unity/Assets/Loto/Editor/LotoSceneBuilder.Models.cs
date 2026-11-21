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
            // The team's hammer mill (Modelos/Prototipo1) first; the Kenney machine if it is missing.
            GameObject model = PlaceMolino(machine);
            bool molino = model != null;
            // About 0.20 x 0.22 x 0.25 m once scaled: close to the 0.20 m cube the rest was measured against.
            if (!molino) model = PlaceModel("machine-window", machine, 0.25f, 0.22f, 0f);
            if (model == null)
            {
                Debug.LogWarning("[Loto] No machine model found: the machine stays a cube.");
                return;
            }
            body.GetComponent<MeshRenderer>().enabled = false;

            // Hazard stripes on the table under the machine: the danger zone made visible.
            GameObject stripes = PlaceModel("indicator-special-lines", machine, 0.30f, 0.02f, 0f);
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
            Debug.Log("[Loto] Machine dressed with " + (molino ? "the team's Molino" : "Kenney machine-window") + " (" +
                b.size.x.ToString("F2") + " x " + b.size.y.ToString("F2") + " x " + b.size.z.ToString("F2") + " m).");

            if (molino)
            {
                // Label and lamp on the points the modeler marked; hide the marker cubes.
                Transform labelPoint = FindChild(model.transform, "LabelPoint");
                Transform lampPoint = FindChild(model.transform, "LampPoint");
                if (label != null && labelPoint != null)
                    label.localPosition = machine.InverseTransformPoint(labelPoint.position) + new Vector3(0f, 0f, -0.004f);
                if (lamp != null && lampPoint != null)
                    lamp.localPosition = machine.InverseTransformPoint(lampPoint.position) + new Vector3(0f, 0f, -0.006f);
                foreach (Transform marker in new[] { labelPoint, lampPoint })
                    if (marker != null) { var mr = marker.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false; }

                // The pulley is what turns: it spins about its thinnest axis (the disk's axis), wrapped in a holder whose
                // local Y is that axis, because DangerZone turns its spinner about local Y.
                Transform rotorPivot = FindChild(model.transform, "RotorPivot");
                Transform rotor = FindChild(model.transform, "Rotor");
                DangerZone dz = zone != null ? zone.GetComponent<DangerZone>() : null;
                if (rotorPivot != null && rotor != null && dz != null)
                {
                    Bounds rb = LocalBounds(machine, rotor.gameObject);
                    Vector3 axis = rb.size.x <= rb.size.y && rb.size.x <= rb.size.z ? Vector3.right
                        : rb.size.y <= rb.size.z ? Vector3.up : Vector3.forward;
                    Transform holder = Group("RotorSpin", machine, machine.InverseTransformPoint(rotorPivot.position));
                    holder.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
                    rotorPivot.SetParent(holder, true);
                    Wire(dz, "spinner", holder);
                    dz.spinDegreesPerSecond = 300f;
                    if (spinner != null) spinner.gameObject.SetActive(false);   // the old blades are not needed
                    Debug.Log("[Loto] Molino pulley spins about machine-local " + axis + ".");
                }
            }
            // The part that turns becomes a gear (same Spinner object: DangerZone keeps turning it).
            else if (spinner != null)
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
            GameObject belt = molino ? null : PlaceModel("conveyor-long", machine, 0.40f, 0.07f, 0f, false, true);
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

        public const string MolinoPath = "Assets/ThirdParty/Molino/Molino.fbx";

        /// <summary>
        /// The team's hammer mill, scaled to the table and turned so its LabelPoint face looks at the user (-Z).
        /// Null if the file is missing.
        /// </summary>
        static GameObject PlaceMolino(Transform machine)
        {
            GameObject m = PlaceFbx(MolinoPath, "Model_Molino", machine, 0.27f, 0.25f, 0f, Quaternion.identity, true);
            if (m == null) return null;

            // 1. Stand it up: the hopper must be on top. FBX files exported Z-up arrive lying on their side (seen 21/11:
            //    the prototype came in tipped over), so the part names decide which way is up, not the file's axes.
            Quaternion upright = Quaternion.identity;
            Transform hopper = FindChild(m.transform, "Hopper");
            if (hopper != null)
            {
                Vector3 toHopper = LocalBounds(machine, hopper.gameObject).center - LocalBounds(machine, m).center;
                Vector3 axis = DominantAxis(toHopper);
                if (axis != Vector3.up) upright = Quaternion.FromToRotation(axis, Vector3.up);
            }
            if (upright != Quaternion.identity)
            {
                Object.DestroyImmediate(m);
                m = PlaceFbx(MolinoPath, "Model_Molino", machine, 0.27f, 0.25f, 0f, upright, true);
            }

            // 2. Face the user: the LabelPoint side looks at -Z.
            Transform labelPoint = FindChild(m.transform, "LabelPoint");
            if (labelPoint == null) return m;
            Bounds b = LocalBounds(machine, m);
            Vector3 toLabel = machine.InverseTransformPoint(labelPoint.position) - b.center;
            toLabel.y = 0f;
            if (toLabel.sqrMagnitude < 0.000001f) return m;
            float yaw = Vector3.SignedAngle(toLabel, Vector3.back, Vector3.up);
            if (Mathf.Abs(yaw) < 1f) return m;
            Object.DestroyImmediate(m);
            return PlaceFbx(MolinoPath, "Model_Molino", machine, 0.27f, 0.25f, yaw, upright, true);
        }

        public const string CandadoPath = "Assets/ThirdParty/Candado/Candado.fbx";

        /// <summary>
        /// The team's padlock (Modelos/Prototipo1) in place of the cube padlock: only looks. The grab volume, the
        /// snapping and our own lockout tag (shown only while locked, with the student's name) stay the same, so the
        /// model's own Tag is removed. Stood upright with the shackle on top and the thin side to the front, whatever
        /// axes the FBX was exported with, and fitted to the cube padlock's 8 x 12 cm.
        /// </summary>
        static partial void DressPadlock(Transform padlock)
        {
            GameObject m = PlaceCandado(padlock, 0f, Quaternion.identity);
            if (m == null) return;
            Quaternion upright = Quaternion.identity;
            Transform body = FindChild(m.transform, "Body"), shackle = FindChild(m.transform, "Shackle");
            if (body != null && shackle != null)
            {
                Vector3 axis = DominantAxis(LocalBounds(padlock, shackle.gameObject).center - LocalBounds(padlock, body.gameObject).center);
                if (axis != Vector3.up) upright = Quaternion.FromToRotation(axis, Vector3.up);
            }
            if (upright != Quaternion.identity)
            {
                Object.DestroyImmediate(m);
                m = PlaceCandado(padlock, 0f, upright);
                body = FindChild(m.transform, "Body");
            }
            if (body != null)
            {
                Bounds bb = LocalBounds(padlock, body.gameObject);
                if (bb.size.x < bb.size.z)
                {
                    Object.DestroyImmediate(m);
                    m = PlaceCandado(padlock, 90f, upright);
                }
            }
            foreach (string part in new[] { "Body", "ShackleLeft", "ShackleRight", "ShackleTop" })
            {
                Transform t = padlock.Find(part);   // our cube parts (direct children), not the model's
                if (t != null && t.GetComponent<Renderer>() != null) t.GetComponent<Renderer>().enabled = false;
            }
            Debug.Log("[Loto] Padlock dressed with the team's model (" + LocalBounds(padlock, m).size.ToString("F3") + " m).");
        }

        static GameObject PlaceCandado(Transform padlock, float yaw, Quaternion extra)
        {
            GameObject m = PlaceFbx(CandadoPath, "Model_Candado", padlock, 0.08f, 0.12f, yaw, extra, true);
            if (m == null) return null;
            // Our tag is the one that shows the student's name: the model's goes. Destroyed, not hidden, so the
            // bounds below do not count it.
            for (int i = 0; i < 2; i++)
            {
                Transform tag = FindChild(m.transform, i == 0 ? "Tag" : "TagFace");
                if (tag != null) Object.DestroyImmediate(tag.gameObject);
            }
            Bounds b = LocalBounds(padlock, m);
            float footprint = Mathf.Max(b.size.x, b.size.z);
            if (footprint < 0.0001f || b.size.y < 0.0001f) return m;
            float k = Mathf.Min(0.08f / footprint, 0.12f / b.size.y);
            m.transform.localScale *= k;
            b = LocalBounds(padlock, m);
            m.transform.localPosition -= b.center;   // centered on the padlock's origin, like the cube padlock
            return m;
        }

        /// <summary>The signed unit axis (±X, ±Y, ±Z) closest to v.</summary>
        static Vector3 DominantAxis(Vector3 v)
        {
            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
            if (ay >= ax && ay >= az) return v.y >= 0f ? Vector3.up : Vector3.down;
            if (ax >= az) return v.x >= 0f ? Vector3.right : Vector3.left;
            return v.z >= 0f ? Vector3.forward : Vector3.back;
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
            return PlaceFbx(KenneyFolder + "/" + name + ".fbx", "Model_" + name, parent, maxFootprint, maxHeight, yaw, extra, false);
        }

        /// <summary>
        /// Any FBX, placed like the Kenney ones. keepColors: each imported material becomes an opaque URP Lit material
        /// of the same color (the team's own models, solid colors per part); otherwise the Kenney palette texture.
        /// </summary>
        static GameObject PlaceFbx(string path, string objectName, Transform parent, float maxFootprint, float maxHeight, float yaw, Quaternion extra, bool keepColors)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = objectName;
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
                {
                    if (keepColors)
                    {
                        Color c = slots[i] != null && slots[i].HasProperty("_Color") ? slots[i].color : new Color(0.5f, 0.5f, 0.5f);
                        if (slots[i] != null && slots[i].HasProperty("_BaseColor")) c = slots[i].GetColor("_BaseColor");
                        c.a = 1f;   // opaque, always
                        string matName = "Team_" + (slots[i] != null ? slots[i].name : "Part").Replace(" ", "_").Replace("/", "_");
                        slots[i] = MakeMaterial(matName, "Universal Render Pipeline/Lit", c, 0.25f);
                    }
                    else slots[i] = slots[i] != null && slots[i].name.ToLowerInvariant().Contains("glass") ? glass : colormap;
                }
                r.sharedMaterials = slots;
            }

            Bounds b = LocalBounds(parent, model);
            float footprint = Mathf.Max(b.size.x, b.size.z);
            // Flat models (the hazard stripes are a plane: height 0) still scale by their footprint (seen 21/11 in the Quest:
            // unscaled stripes crossed the whole table).
            if (footprint < 0.0001f) return model;
            float k = Mathf.Min(maxFootprint / footprint, maxHeight / Mathf.Max(b.size.y, 0.0001f));
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
