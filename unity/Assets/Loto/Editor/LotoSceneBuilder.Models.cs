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
            if (!molino) model = PlaceModel("machine-window", machine, RealScaleMachines ? 1.0f : 0.25f, RealScaleMachines ? 0.9f : 0.22f, 0f);
            if (model == null)
            {
                Debug.LogWarning("[Loto] No machine model found: the machine stays a cube.");
                return;
            }
            body.GetComponent<MeshRenderer>().enabled = false;

            // Hazard stripes on the table under the machine: the danger zone made visible.
            GameObject stripes = PlaceModel("indicator-special-lines", machine, RealScaleMachines ? 1.7f : 0.30f, 0.02f, 0f);
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
            EnergyFlow flow = null;
            if (RealScaleMachines)
            {
                // Readable from the table: the label and the lamp grow with the machine.
                if (label != null) label.localScale = Vector3.one * 2.5f;
                if (lamp != null) lamp.localScale = lamp.localScale * 3f;
                // "PRUEBA DE ARRANQUE / NO ARRANCA" goes over the table, next to VERIFICAR, not 2 m up the press.
                Transform tryOut = FindChild(machine, "TryOutLabel");
                if (tryOut != null && board != null)
                {
                    tryOut.SetParent(board, false);
                    tryOut.localPosition = new Vector3(0.36f, TableTop + 0.30f, 0.70f);
                }
            }
            if (board != null && RealScaleMachines)
            {
                // Cable: from the switch box along the back of the table to its right edge, then down to the machine
                // on the floor (CableLink keeps that last stretch joined when the board moves onto a real table).
                Material rubber = MakeMaterial("Cable", "Universal Render Pipeline/Lit", new Color(0.06f, 0.06f, 0.07f), 0.35f);
                float h = TableTop + 0.012f, backZ = TableDistance + TableSize.z / 2f - 0.04f, boxBack = LeverZ + SwitchBoxSize.z / 2f;
                float edgeX = TableSize.x / 2f - 0.02f;
                Cable(board, new Vector3(LeverX, h, boxBack), new Vector3(LeverX, h, backZ), rubber);
                Cable(board, new Vector3(LeverX, h, backZ), new Vector3(edgeX, h, backZ), rubber);
                Transform flowRoot = Group("EnergyFlow", board, Vector3.zero);
                Transform p0 = Group("Path0", flowRoot, new Vector3(LeverX, h, boxBack));
                Transform p1 = Group("Path1", flowRoot, new Vector3(LeverX, h, backZ));
                Transform p2 = Group("Path2", flowRoot, new Vector3(edgeX, h, backZ));
                Transform p3 = Group("CablePlug", machine, new Vector3(b.min.x + 0.06f, 0.10f, b.center.z));
                GameObject drop = Part(PrimitiveType.Cylinder, "CableDrop", board, Vector3.zero, Vector3.one * 0.016f, rubber);
                var link = drop.AddComponent<CableLink>();
                link.from = p2;
                link.to = p3;
                link.thickness = 0.02f;
                flow = flowRoot.gameObject.AddComponent<EnergyFlow>();
                flow.lever = Object.FindAnyObjectByType<Lever>();
                flow.path = new[] { p0, p1, p2, p3 };
                flow.shaker = model.transform;
                flow.shakeAmplitude = 0.003f;
            }
            else if (board != null)
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
                flow = flowRoot.gameObject.AddComponent<EnergyFlow>();
                flow.lever = Object.FindAnyObjectByType<Lever>();
                flow.path = new[] { p0, p1, p2 };
                flow.shaker = model.transform;
            }

            AddTeamMachines(machine, model, molino, label, lamp, zone, trigger, flow);
        }

        public const string PrensaPath = "Assets/ThirdParty/Prensa/Prensa.fbx";
        public const string NeumaticaPath = "Assets/ThirdParty/Neumatica/Neumatica.fbx";

        const string HintMolino = "<b>ESPERA: TODAVÍA SE MUEVE</b>\nApagar no alcanza: la polea sigue girando por inercia.\nVerifica cuando esté quieta ({0} s).";
        const string HintPrensa = "<b>ESPERA: TODAVÍA HAY PRESIÓN</b>\nApagar no alcanza: el aceite sigue empujando el pistón.\nToca PURGAR y verifica cuando el manómetro marque 0.";
        const string HintNeumatica = "<b>ESPERA: TODAVÍA HAY AIRE</b>\nApagar no alcanza: queda aire comprimido en la línea.\nToca PURGAR y verifica cuando el manómetro marque 0.";

        /// <summary>MÁQUINA (choose the scenario between attempts) and PURGAR (let the stored pressure out), on the
        /// table where the small machine used to be. Only in the real-scale scene.</summary>
        static partial void AddMachineButtons(Parts parts, DangerZone dangerZone)
        {
            if (!RealScaleMachines || dangerZone == null) return;
            MachineSelector selector = dangerZone.transform.parent.GetComponent<MachineSelector>();
            if (selector == null) return;
            VerifyButton change = BuildButton(parts, "MachineButton", MachineButtonPosition, "MÁQUINA",
                VerifyButton.ButtonAction.NextMachine, new Color(0.30f, 0.32f, 0.38f));
            selector.buttonLabel = change.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (selector.buttonLabel != null && selector.names != null && selector.names.Length > 0)
                selector.buttonLabel.text = selector.names[0] + "\n<size=34>cambiar</size>";
            VerifyButton bleed = BuildButton(parts, "BleedButton", BleedButtonPosition, "PURGAR",
                VerifyButton.ButtonAction.Bleed, new Color(0.80f, 0.12f, 0.10f));
            selector.bleedButton = bleed.gameObject;
            BuildControlStation(parts, dangerZone, bleed);
            bleed.gameObject.SetActive(false);   // the first machine (the mill) has no stored pressure
            Debug.Log("[Loto] Real-scale machines on the floor: MÁQUINA and PURGAR buttons on the table.");
        }

        /// <summary>
        /// The team's press and pneumatic station (Modelos/Prototipo1) next to the first machine, one shown at a time
        /// (MachineSelector: the next student gets the next machine). Each gets its own label and lamp place, and its
        /// moving parts (MachineMotion). The danger zone grows to hold the largest. Nothing changes if both files are missing.
        /// </summary>
        static void AddTeamMachines(Transform machine, GameObject first, bool molino, Transform label, Transform lamp,
            Transform zone, BoxCollider trigger, EnergyFlow flow)
        {
            DangerZone dz = zone != null ? zone.GetComponent<DangerZone>() : null;
            if (dz == null || label == null || lamp == null) return;
            // The press is the tallest: 0.29 m keeps clear of the step pointer above the machine (0.25-0.31 is its band,
            // but it points at the front, and the press is narrow at the top).
            // Real sizes from MODELOS-MAQUINAS-2-3.md: press 1.80 m, pneumatic station 1.20 m.
            GameObject prensa = PlaceTeamMachine(PrensaPath, "Model_Prensa", machine, "Bed", "Cylinder",
                RealScaleMachines ? 2.0f : 0.24f, RealScaleMachines ? 1.80f : 0.29f);
            GameObject neumatica = PlaceTeamMachine(NeumaticaPath, "Model_Neumatica", machine, "Base", "CylinderBody",
                RealScaleMachines ? 2.0f : 0.27f, RealScaleMachines ? 1.20f : 0.27f);
            if (prensa == null && neumatica == null) return;

            Transform holder = Group("Models", machine, Vector3.zero);
            Transform firstGroup = Group(molino ? "Molino" : "Kenney", holder, Vector3.zero);
            first.transform.SetParent(firstGroup, true);
            foreach (string extra in new[] { "RotorSpin", "Model_conveyor-long" })
            {
                Transform t = FindChild(machine, extra);
                if (t != null) t.SetParent(firstGroup, true);
            }
            Transform oldSpinner = FindChild(machine, "Spinner");
            if (oldSpinner != null && oldSpinner.gameObject.activeSelf) oldSpinner.SetParent(firstGroup, true);

            var machines = new System.Collections.Generic.List<GameObject> { firstGroup.gameObject };
            var names = new System.Collections.Generic.List<string> { molino ? "MOLINO" : "MÁQUINA" };
            var hints = new System.Collections.Generic.List<string> { HintMolino };
            var pressure = new System.Collections.Generic.List<bool> { false };
            var labels = new System.Collections.Generic.List<Vector3> { label.localPosition };
            var lamps = new System.Collections.Generic.List<Vector3> { lamp.localPosition };
            Bounds all = LocalBounds(machine, firstGroup.gameObject);
            Lever lever = Object.FindAnyObjectByType<Lever>();

            if (prensa != null)
            {
                Bounds b = LocalBounds(machine, prensa);
                all.Encapsulate(b);
                var motion = prensa.AddComponent<MachineMotion>();
                motion.zone = dz;
                motion.lever = lever;
                // Real stroke 0.25 m of 1.80 m: the same share of the scaled height.
                SetupSlider(machine, motion, FindChild(prensa.transform, "RamPivot"), 0.25f / 1.80f * b.size.y);
                SetupNeedle(machine, motion, FindChild(prensa.transform, "GaugeNeedlePivot"), 180f);
                SetupHandle(machine, motion, FindChild(prensa.transform, "BleedValvePivot"), Vector3.back, 90f);
                motion.handleOnBleed = true;   // the red bleed knob opens on PURGAR
                machines.Add(prensa); names.Add("PRENSA"); hints.Add(HintPrensa); pressure.Add(true);
                labels.Add(LabelPlace(machine, prensa, b)); lamps.Add(LampPlace(b));
                Debug.Log("[Loto] Team press added (" + b.size.ToString("F2") + " m).");
            }
            if (neumatica != null)
            {
                Bounds b = LocalBounds(machine, neumatica);
                all.Encapsulate(b);
                var motion = neumatica.AddComponent<MachineMotion>();
                motion.zone = dz;
                motion.lever = lever;
                motion.cyclesPerSecond = 0.8f;
                SetupSlider(machine, motion, FindChild(neumatica.transform, "RodPivot"), 0.15f / 1.20f * b.size.y);
                SetupNeedle(machine, motion, FindChild(neumatica.transform, "GaugeNeedlePivot"), 150f);
                SetupHandle(machine, motion, FindChild(neumatica.transform, "ValveHandlePivot"), Vector3.up, 90f);
                machines.Add(neumatica); names.Add("NEUMÁTICA"); hints.Add(HintNeumatica); pressure.Add(true);
                labels.Add(LabelPlace(machine, neumatica, b)); lamps.Add(LampPlace(b));
                Debug.Log("[Loto] Team pneumatic station added (" + b.size.ToString("F2") + " m).");
            }
            for (int i = 0; i < machines.Count; i++)
            {
                if (machines[i] == firstGroup.gameObject) continue;
                Transform group = Group(names[i] == "PRENSA" ? "Prensa" : "Neumatica", holder, Vector3.zero);
                machines[i].transform.SetParent(group, true);
                machines[i] = group.gameObject;
            }
            if (RealScaleMachines)
            {
                foreach (GameObject g in machines) AddGround(machine, g);
                for (int i = 0; i < machines.Count; i++)
                {
                    bool press = names[i] == "PRENSA", air = names[i] == "NEUMÁTICA";
                    AddSign(machine, machines[i], press || air ? "PELIGRO\n<size=44>ENERGÍA ALMACENADA</size>" : "PELIGRO\n<size=44>PARTES EN MOVIMIENTO</size>");
                    if (!press && !air) continue;
                    // The jet comes out of the valve the student would open: the press's red bleed knob, the station's exhaust.
                    Transform valve = FindChild(machines[i].transform, press ? "BleedValvePivot" : "ExhaustValve");
                    if (valve == null) continue;
                    Transform emitter = Group("PurgeEmitter", machine, machine.InverseTransformPoint(
                        valve.GetComponentInChildren<Renderer>() != null ? valve.GetComponentInChildren<Renderer>().bounds.center : valve.position));
                    emitter.SetParent(machines[i].transform, true);
                    var jet = machines[i].AddComponent<PurgeJet>();
                    jet.zone = dz;
                    jet.emitter = emitter;
                    jet.color = press ? new Color(0.86f, 0.74f, 0.42f) : new Color(0.92f, 0.94f, 0.96f);
                    jet.size = press ? 0.025f : 0.035f;
                }
            }

            if (trigger != null)
            {
                zone.localPosition = all.center;
                trigger.size = Vector3.Max(all.size + Vector3.one * 0.07f, new Vector3(0.27f, 0.28f, 0.27f));
            }
            if (flow != null)
            {
                flow.shaker = holder;
                // The cable plug follows the first machine's side; move it to the side of the widest one.
                Transform plug = FindChild(machine, "CablePlug");
                if (plug != null) plug.localPosition = new Vector3(all.min.x + 0.06f, 0.10f, all.center.z);
            }

            var selector = machine.gameObject.AddComponent<MachineSelector>();
            selector.session = dz.session;
            selector.machines = machines.ToArray();
            selector.names = names.ToArray();
            selector.hints = hints.ToArray();
            selector.labelPositions = labels.ToArray();
            selector.lampPositions = lamps.ToArray();
            selector.label = label;
            selector.labelText = label.GetComponentInChildren<UnityEngine.UI.Text>(true);
            selector.lamp = lamp;
            selector.zone = dz;
            selector.holdsPressure = pressure.ToArray();
            selector.advanceOnNewStudent = !RealScaleMachines;
            for (int i = 1; i < machines.Count; i++) machines[i].SetActive(false);
            Debug.Log("[Loto] " + machines.Count + " machines on the table; the next student gets the next one.");
        }

        /// <summary>
        /// A flat, opaque, near-black ellipse under the machine: in passthrough it reads as the machine's shadow on the
        /// real floor, so the machine stands on it instead of floating. Child of the machine's group (switches with it).
        /// </summary>
        static void AddGround(Transform machine, GameObject group)
        {
            Bounds b = LocalBounds(machine, group);
            Material ground = MakeMaterial("GroundShadow", "Universal Render Pipeline/Unlit", new Color(0.03f, 0.03f, 0.035f), 0f);
            GameObject disc = Part(PrimitiveType.Cylinder, "Ground", machine, new Vector3(b.center.x, 0.002f, b.center.z),
                new Vector3(b.size.x * 1.12f, 0.001f, b.size.z * 1.12f), ground);
            disc.transform.SetParent(group.transform, true);
        }

        /// <summary>A yellow danger sign with black text on the machine's front, upper right (opaque plate behind).</summary>
        static void AddSign(Transform machine, GameObject group, string text)
        {
            Bounds b = LocalBounds(machine, group);
            Material yellow = MakeMaterial("DangerSign", "Universal Render Pipeline/Unlit", new Color(0.98f, 0.78f, 0.05f), 0f);
            Vector3 at = new Vector3(b.center.x + b.size.x * 0.22f, Mathf.Max(0.6f, b.max.y * 0.62f), b.min.z - 0.012f);
            Transform sign = Group("DangerSign", group.transform, Vector3.zero);
            sign.position = machine.TransformPoint(at);
            sign.rotation = machine.rotation;
            Part(PrimitiveType.Quad, "Plate", sign, new Vector3(0f, 0f, 0.002f), new Vector3(0.34f, 0.20f, 1f), yellow);
            Label(sign, "Text", text, new Vector3(0f, 0f, -0.001f), Quaternion.identity, 640, 360, 0.32f, 72, new Color(0.05f, 0.05f, 0.05f));
        }

        public const string SeccionadorPath = "Assets/ThirdParty/Seccionador/Seccionador.fbx";

        /// <summary>
        /// The lockout station beside the real-scale machine, as in a real workshop: the disconnect switch with its
        /// lever and lock point, VERIFICAR and PURGAR, on a pedestal just outside the danger zone. The padlock stays in
        /// its tray on the table: the student carries it to the machine. Nothing changes for the Referee.
        /// </summary>
        static void BuildControlStation(Parts parts, DangerZone dangerZone, VerifyButton bleed)
        {
            Transform board = parts.board, machine = dangerZone.transform.parent;
            BoxCollider zone = dangerZone.GetComponent<BoxCollider>();
            if (zone == null) return;
            Vector3 zoneMin = dangerZone.transform.localPosition + zone.center - zone.size / 2f;
            const float top = 0.95f;   // hand height, like the table
            Transform station = Group("ControlStation", machine, new Vector3(zoneMin.x - 0.33f, 0f, zoneMin.z + 0.30f));
            Material stand = MakeMaterial("StationStand", "Universal Render Pipeline/Lit", new Color(0.20f, 0.22f, 0.25f), 0.3f);
            Material edge = MakeMaterial("StationEdge", "Universal Render Pipeline/Lit", new Color(0.95f, 0.72f, 0.05f), 0.2f);
            Part(PrimitiveType.Cube, "Pedestal", station, new Vector3(0f, (top - 0.01f) / 2f, 0f), new Vector3(0.55f, top - 0.01f, 0.40f), stand);
            Part(PrimitiveType.Cube, "PedestalTop", station, new Vector3(0f, top - 0.005f, 0f), new Vector3(0.58f, 0.01f, 0.43f), edge);
            Label(station, "StationLabel", "PUNTO DE BLOQUEO", new Vector3(0f, top - 0.12f, -0.2015f), Quaternion.identity,
                520, 90, 0.40f, 56, new Color(0.95f, 0.72f, 0.05f));

            // Same arrangement as on the table, moved as a block: switch box base -> left of the pedestal top.
            Vector3 switchBase = new Vector3(LeverX, TableTop, LeverZ);
            Vector3 switchSpot = new Vector3(-0.13f, top, 0.07f);
            foreach (string n in new[] { "SwitchBox", "HingeBlock", "SwitchLabel", "LockPost", "LockPoint" })
            {
                Transform t = board.Find(n);   // direct children only: the press model has its own LockPoint
                if (t == null) continue;
                Vector3 p = t.localPosition;
                t.SetParent(station, false);
                t.localPosition = p - switchBase + switchSpot;
            }
            Transform lever = parts.lever.transform;
            Vector3 lp = lever.localPosition;
            lever.SetParent(station, false);
            lever.localPosition = lp - switchBase + switchSpot;

            Transform verify = board.Find("VerifyButton");
            if (verify != null) { verify.SetParent(station, false); verify.localPosition = new Vector3(0.15f, top, 0.09f); }
            if (bleed != null) { bleed.transform.SetParent(station, false); bleed.transform.localPosition = new Vector3(0.15f, top, -0.09f); }
            Transform tryOut = board.Find("TryOutLabel");
            if (tryOut != null) { tryOut.SetParent(station, false); tryOut.localPosition = new Vector3(0f, top + 0.45f, 0f); }

            // The cable now runs from the switch box to the machine: drop the table run.
            var old = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in board) if (child.name == "Cable") old.Add(child.gameObject);
            foreach (GameObject g in old) Object.DestroyImmediate(g);
            Transform start = Group("CableStart", station, switchSpot + new Vector3(0f, 0.03f, SwitchBoxSize.z / 2f));
            Transform plug = FindChild(machine, "CablePlug");
            Transform flowRoot = board.Find("EnergyFlow");
            EnergyFlow flow = flowRoot != null ? flowRoot.GetComponent<EnergyFlow>() : null;
            if (flow != null && plug != null) flow.path = new[] { start, plug };
            Transform drop = board.Find("CableDrop");
            CableLink link = drop != null ? drop.GetComponent<CableLink>() : null;
            if (link != null) link.from = start;

            DressSwitch(station, switchSpot);
            Debug.Log("[Loto] Lockout station beside the machine: lever, lock point, VERIFICAR and PURGAR.");
        }

        /// <summary>The team's disconnect switch box (Seccionador) in place of the yellow cube; our lever stays the one
        /// that is grabbed, so the model's own handle goes.</summary>
        static void DressSwitch(Transform station, Vector3 spot)
        {
            GameObject m = PlaceFbx(SeccionadorPath, "Model_Seccionador", station, 0.25f, 0.40f, 0f, Quaternion.identity, true);
            if (m == null) return;
            Transform box = FindChild(m.transform, "Box"), handle = FindChild(m.transform, "Handle");
            if (box != null && handle != null)
            {
                Vector3 axis = DominantAxis(LocalBounds(station, handle.gameObject).center - LocalBounds(station, box.gameObject).center);
                if (axis != Vector3.up)
                {
                    Object.DestroyImmediate(m);
                    m = PlaceFbx(SeccionadorPath, "Model_Seccionador", station, 0.25f, 0.40f, 0f, Quaternion.FromToRotation(axis, Vector3.up), true);
                }
            }
            foreach (string n in new[] { "HandlePivot", "Handle", "LockPoint" })
            {
                Transform t = FindChild(m.transform, n);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            Bounds b = LocalBounds(station, m);
            float footprint = Mathf.Max(b.size.x, b.size.z);
            if (footprint < 0.0001f || b.size.y < 0.0001f) return;
            m.transform.localScale *= Mathf.Min(SwitchBoxSize.y / b.size.y, 0.22f / footprint);
            b = LocalBounds(station, m);
            m.transform.localPosition -= new Vector3(b.center.x - spot.x, b.min.y - spot.y, b.center.z - spot.z);
            Transform cube = station.Find("SwitchBox");
            if (cube != null && cube.GetComponent<Renderer>() != null) cube.GetComponent<Renderer>().enabled = false;
        }

        /// <summary>Stood upright (high part above low part) and turned so its LabelPoint faces the user.</summary>
        static GameObject PlaceTeamMachine(string path, string objectName, Transform machine, string low, string high, float footprint, float height)
        {
            GameObject m = PlaceFbx(path, objectName, machine, footprint, height, 0f, Quaternion.identity, true);
            if (m == null) return null;
            Quaternion upright = Quaternion.identity;
            Transform lo = FindChild(m.transform, low), hi = FindChild(m.transform, high);
            if (lo != null && hi != null)
            {
                Vector3 axis = DominantAxis(LocalBounds(machine, hi.gameObject).center - LocalBounds(machine, lo.gameObject).center);
                if (axis != Vector3.up) upright = Quaternion.FromToRotation(axis, Vector3.up);
            }
            if (upright != Quaternion.identity)
            {
                Object.DestroyImmediate(m);
                m = PlaceFbx(path, objectName, machine, footprint, height, 0f, upright, true);
            }
            // Both machines are wider (X) than deep (Z) in real life: the wide side goes across the table. The label
            // plate can sit off to one side (the press's is on the pump), so it only decides front or back (seen 21/11:
            // snapping to the plate's direction turned both machines a quarter turn).
            Bounds b = LocalBounds(machine, m);
            float yaw = b.size.z > b.size.x ? 90f : 0f;
            if (yaw != 0f)
            {
                Object.DestroyImmediate(m);
                m = PlaceFbx(path, objectName, machine, footprint, height, yaw, upright, true);
            }
            Transform labelPoint = FindChild(m.transform, "LabelPoint");
            if (labelPoint == null) return m;
            b = LocalBounds(machine, m);
            if (machine.InverseTransformPoint(labelPoint.position).z <= b.center.z) return m;   // plate already toward the user (-Z)
            Object.DestroyImmediate(m);
            return PlaceFbx(path, objectName, machine, footprint, height, yaw + 180f, upright, true);
        }

        static void SetupSlider(Transform machine, MachineMotion motion, Transform pivot, float stroke)
        {
            if (pivot == null || pivot.parent == null) return;
            motion.slider = pivot;
            motion.slideOffset = pivot.parent.InverseTransformVector(machine.TransformVector(Vector3.down * stroke));
        }

        static void SetupNeedle(Transform machine, MachineMotion motion, Transform pivot, float angle)
        {
            if (pivot == null) return;
            motion.needle = pivot;
            motion.needleAxis = pivot.InverseTransformDirection(machine.TransformDirection(Vector3.back));
            motion.needleAngle = angle;
        }

        static void SetupHandle(Transform machine, MachineMotion motion, Transform pivot, Vector3 machineAxis, float angle)
        {
            if (pivot == null) return;
            motion.handle = pivot;
            motion.handleAxis = pivot.InverseTransformDirection(machine.TransformDirection(machineAxis));
            motion.handleAngle = angle;
        }

        static Vector3 LabelPlace(Transform machine, GameObject model, Bounds b)
        {
            Transform labelPoint = FindChild(model.transform, "LabelPoint");
            if (labelPoint != null) return machine.InverseTransformPoint(labelPoint.position) + new Vector3(0f, 0f, -0.004f);
            return new Vector3(0f, b.min.y + 0.03f, b.min.z - 0.003f);
        }

        static Vector3 LampPlace(Bounds b)
        {
            return new Vector3(b.min.x + 0.03f, b.max.y - 0.03f, b.min.z - 0.008f);
        }

        public const string MolinoPath = "Assets/ThirdParty/Molino/Molino.fbx";
        // Real scale: a workshop hammer mill about 1.1 m tall; on the table, 25 cm.
        static float MolinoFootprint { get { return RealScaleMachines ? 1.30f : 0.27f; } }
        static float MolinoHeight { get { return RealScaleMachines ? 1.10f : 0.25f; } }

        /// <summary>
        /// The team's hammer mill, scaled to the table and turned so its LabelPoint face looks at the user (-Z).
        /// Null if the file is missing.
        /// </summary>
        static GameObject PlaceMolino(Transform machine)
        {
            GameObject m = PlaceFbx(MolinoPath, "Model_Molino", machine, MolinoFootprint, MolinoHeight, 0f, Quaternion.identity, true);
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
                m = PlaceFbx(MolinoPath, "Model_Molino", machine, MolinoFootprint, MolinoHeight, 0f, upright, true);
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
            return PlaceFbx(MolinoPath, "Model_Molino", machine, MolinoFootprint, MolinoHeight, yaw, upright, true);
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
                        float hi = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), lo = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                        bool steel = hi > 0.55f && hi - lo < 0.12f;
                        if (slots[i].HasProperty("_Metallic")) slots[i].SetFloat("_Metallic", steel ? 0.75f : 0f);
                        if (steel && slots[i].HasProperty("_Smoothness")) slots[i].SetFloat("_Smoothness", 0.65f);
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
