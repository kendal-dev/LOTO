using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Loto.EditorTools
{
    /// <summary>
    /// Builds the whole scene from code; nobody clicks anything in Unity.
    ///   Loto > Build Scene                 headset scene (Meta rig, passthrough, hands) -> Assets/Loto/Scenes/Loto.unity
    ///   Loto > Build Scene (Desktop only)  same board with a normal camera, nothing from Meta -> LotoDesktop.unity
    /// This file uses only Unity and the Loto scripts. Everything from Meta lives in two other files:
    ///   LotoSceneBuilder.Rig.cs    camera rig + passthrough (Meta XR Core SDK)
    ///   LotoSceneBuilder.Hands.cs  hands, grab on the lever and the padlock (Meta Interaction SDK)
    /// Each of them can be switched off by commenting out its first line (#define): the hook below
    /// then compiles to nothing and the scene is built without that part.
    /// </summary>
    public static partial class LotoSceneBuilder
    {
        /// <summary>What the common part builds and the Meta parts need.</summary>
        public sealed class Parts
        {
            public Transform board;
            public LotoSession session;
            public Lever lever;
            public Padlock padlock;
            public FingerTip leftTip;
            public FingerTip rightTip;
            public Camera desktopCamera;
            public GameObject rig;          // set by BuildRig (null = no Meta rig in the scene)
            public bool handsBuilt;         // set by BuildHands
        }

        static partial void BuildRig(Parts parts);
        static partial void BuildHands(Parts parts);
        static partial void DressMachine(Transform machine, GameObject body);
        static partial void DressPadlock(Transform padlock);
        static partial void AddMachineButtons(Parts parts, DangerZone dangerZone);

        public const string ScenePath = "Assets/Loto/Scenes/Loto.unity";
        public const string DesktopScenePath = "Assets/Loto/Scenes/LotoDesktop.unity";
        public const string MaterialFolder = "Assets/Loto/Materials";

        /// <summary>Default server address. In the headset it is replaced by server.txt (see AttemptSender).</summary>
        public const string ServerUrl = "http://localhost:3000";

        /// <summary>
        /// PLAN B. true = two more touch buttons on the table, PALANCA and CANDADO, that move the lever and
        /// the padlock without grabbing them (same path as keys 1/6 and 2/5). Set it to true and run
        /// Loto > Build Scene again if grabbing with the hands does not work in the headset.
        /// They are also added by themselves when LotoSceneBuilder.Hands.cs is switched off.
        /// </summary>
        public const bool TouchButtonsForLeverAndPadlock = false;

        // Layout in meters. The user stands at the origin looking along +z; the floor is y = 0.
        public const float TableTop = 0.90f;
        public const float TableDistance = 0.55f;
        public static readonly Vector3 TableSize = new Vector3(1.10f, 0.04f, 0.46f);

        public const float LeverX = -0.36f;
        public const float LeverZ = 0.62f;
        public const float LeverArm = 0.18f;
        public static readonly Vector3 SwitchBoxSize = new Vector3(0.16f, 0.20f, 0.12f);
        public static readonly Vector3 LeverPivot = new Vector3(LeverX, TableTop + 0.23f, LeverZ);
        /// <summary>Where the padlock's center goes: its shackle closes around the lever arm lying in OFF.</summary>
        public static readonly Vector3 LockPoint = new Vector3(LeverX, TableTop + 0.20f, LeverZ - 0.13f);
        public static readonly Vector3 PadlockHome = new Vector3(-0.16f, TableTop + 0.068f, 0.50f);

        public static readonly Vector3 VerifyButtonPosition = new Vector3(0.02f, TableTop, 0.60f);
        public static readonly Vector3 RetryButtonPosition = new Vector3(0.02f, TableTop, 0.42f);
        public static readonly Vector3 LeverButtonPosition = new Vector3(LeverX, TableTop, 0.385f);
        public static readonly Vector3 PadlockButtonPosition = new Vector3(-0.16f, TableTop, 0.385f);

        /// <summary>The machine is 0.72 m to the right of the lever (the brief asks for 0.25 m or more).</summary>
        public static readonly Vector3 MachinePosition = new Vector3(0.36f, TableTop, 0.60f);
        public const float MachineSide = 0.20f;

        /// <summary>
        /// Real-scale machines on the real floor beside the table (FloorStand), instead of 25 cm models on it. The
        /// table keeps the switch, the padlock and the buttons (the control station); MÁQUINA and PURGAR take the
        /// machine's old place on the table. false = the table-scale scene of 21/11 morning.
        /// </summary>
        public const bool RealScaleMachines = true;
        public static readonly Vector3 MachineFloorPosition = new Vector3(1.35f, 0f, 0.85f);
        public static readonly Vector3 MachineButtonPosition = new Vector3(0.36f, TableTop, 0.42f);
        public static readonly Vector3 BleedButtonPosition = new Vector3(0.36f, TableTop, 0.62f);

        public static readonly Vector3 PanelCenter = new Vector3(0f, 1.55f, 1.60f);
        public static readonly Vector3 DesktopEye = new Vector3(0f, 1.50f, -0.90f);

        static Font _font;
        static Material _uiMaterial;

        [MenuItem("Loto/Build Scene")]
        public static void BuildFromMenu()
        {
            if (!ReadyToBuild()) return;
            Parts parts = Build(true);
            Debug.Log("[Loto] Scene built: " + ScenePath +
                " | Meta rig: " + (parts.rig != null ? "yes" : "NO") +
                " | hands: " + (parts.handsBuilt ? "yes" : "NO") +
                ". Press Play and use the keys 1 2 3 4 (R = retry).");
        }

        [MenuItem("Loto/Build Scene (Desktop only)")]
        public static void BuildDesktopFromMenu()
        {
            if (!ReadyToBuild()) return;
            Build(false);
            Debug.Log("[Loto] Desktop scene built: " + DesktopScenePath + " (no Meta rig; it is NOT the scene of the APK). " +
                "Press Play and use the keys 1 2 3 4 (R = retry).");
        }

        static bool ReadyToBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Loto] Stop Play before building the scene.");
                return false;
            }
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        /// <summary>withHeadset = false builds the same board with no Meta object at all.</summary>
        public static Parts Build(bool withHeadset)
        {
            string scenePath = withHeadset ? ScenePath : DesktopScenePath;
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _uiMaterial = MakeMaterial("UI", "Loto/UI", Color.white, 0f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Light
            var lightObject = new GameObject("Sun");
            var sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.50f, 0.50f, 0.50f);

            var parts = new Parts();

            // Session first: everything else points at it.
            var sessionObject = new GameObject("LotoSession");
            parts.session = sessionObject.AddComponent<LotoSession>();

            parts.board = new GameObject("Board").transform;
            BuildTable(parts);
            BuildLever(parts);
            BuildPadlock(parts);
            VerifyButton verifyButton = BuildButton(parts, "VerifyButton", VerifyButtonPosition, "VERIFICAR",
                VerifyButton.ButtonAction.Verify, new Color(0.10f, 0.55f, 0.20f));
            BuildButton(parts, "RetryButton", RetryButtonPosition, "REINTENTAR",
                VerifyButton.ButtonAction.Retry, new Color(0.12f, 0.35f, 0.75f));
            DangerZone dangerZone = BuildMachine(parts);
            AddMachineButtons(parts, dangerZone);   // LotoSceneBuilder.Models.cs: MÁQUINA and PURGAR (real-scale scene)
            StatusPanel panel = BuildPanel(parts);
            BuildStepPointer(parts, verifyButton, dangerZone);
            parts.leftTip = BuildFingerTip("FingerTipLeft", "Left");
            parts.rightTip = BuildFingerTip("FingerTipRight", "Right");

            // Desktop camera (Play without a headset)
            var desktopObject = new GameObject("DesktopCamera");
            var desktopCamera = desktopObject.AddComponent<Camera>();
            desktopCamera.clearFlags = CameraClearFlags.SolidColor;
            desktopCamera.backgroundColor = new Color(0.20f, 0.21f, 0.23f, 1f);
            desktopCamera.fieldOfView = 60f;
            desktopCamera.nearClipPlane = 0.05f;
            desktopCamera.depth = 10f;
            desktopObject.transform.position = DesktopEye;
            desktopObject.transform.LookAt(new Vector3(0f, 1.20f, 1.0f));
            parts.desktopCamera = desktopCamera;

            // Session helpers. LotoLog needs no object: it starts by itself (RuntimeInitializeOnLoadMethod).
            var sender = sessionObject.AddComponent<AttemptSender>();
            sender.baseUrl = ServerUrl;
            Wire(sender, "session", parts.session);
            Wire(sender, "panel", panel);

            var keys = sessionObject.AddComponent<DesktopDebugKeys>();
            Wire(keys, "session", parts.session);
            Wire(keys, "lever", parts.lever);
            Wire(keys, "padlock", parts.padlock);
            Wire(keys, "verifyButton", verifyButton);
            Wire(keys, "dangerZone", dangerZone);
            Wire(keys, "desktopCamera", desktopCamera);

            if (withHeadset)
            {
                BuildRig(parts);
                if (parts.rig == null)
                    Debug.LogWarning("[Loto] No Meta rig in the scene: LotoSceneBuilder.Rig.cs is switched off. The scene only works on the desktop.");
                else
                {
                    BuildHands(parts);
                    if (!parts.handsBuilt)
                        Debug.LogWarning("[Loto] No hand grab in the scene: LotoSceneBuilder.Hands.cs is switched off. The lever and the padlock get touch buttons (PALANCA, CANDADO); use the CONTROLLERS: the fingertip spheres follow them (with bare hands they would sit at the wrist).");
                }
            }

            // Plan B: touch buttons instead of grabbing. Always there when the headset scene has no hand grab.
            bool touchButtons = TouchButtonsForLeverAndPadlock;
            if (withHeadset && !parts.handsBuilt) touchButtons = true;
            if (touchButtons)
            {
                BuildButton(parts, "LeverButton", LeverButtonPosition, "PALANCA",
                    VerifyButton.ButtonAction.ToggleLever, new Color(0.70f, 0.50f, 0.05f));
                BuildButton(parts, "PadlockButton", PadlockButtonPosition, "CANDADO",
                    VerifyButton.ButtonAction.TogglePadlock, new Color(0.75f, 0.10f, 0.10f));
            }
            else
            {
                Label(parts.board, "PadlockLabel", "CANDADO",
                    new Vector3(PadlockHome.x, TableTop + 0.002f, PadlockHome.z - 0.09f),
                    Quaternion.Euler(90f, 0f, 0f), 360, 90, 0.15f, 60, Color.white);
            }

            if (parts.rig == null)
            {
                // The only camera: on from the start, and it carries the audio listener.
                desktopObject.tag = "MainCamera";
                desktopObject.AddComponent<AudioListener>();
            }
            else
            {
                // DesktopDebugKeys turns it on when Play runs without a headset.
                desktopCamera.enabled = false;
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            if (withHeadset) EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            return parts;
        }

        // ---------------------------------------------------------------- pieces

        static void BuildTable(Parts parts)
        {
            Material tableMaterial = MakeMaterial("Table", "Universal Render Pipeline/Lit", new Color(0.30f, 0.33f, 0.38f), 0.15f);
            Material legMaterial = MakeMaterial("TableLeg", "Universal Render Pipeline/Lit", new Color(0.18f, 0.19f, 0.22f), 0.1f);

            Part(PrimitiveType.Cube, "TableTop", parts.board,
                new Vector3(0f, TableTop - TableSize.y / 2f, TableDistance), TableSize, tableMaterial);
            float legHeight = TableTop - TableSize.y;
            Part(PrimitiveType.Cube, "TableLeg", parts.board,
                new Vector3(0f, legHeight / 2f, TableDistance + 0.05f), new Vector3(0.12f, legHeight, 0.12f), legMaterial);
        }

        static void BuildLever(Parts parts)
        {
            Material boxMaterial = MakeMaterial("SwitchBox", "Universal Render Pipeline/Lit", new Color(0.85f, 0.65f, 0.05f), 0.2f);
            Material armMaterial = MakeMaterial("LeverArm", "Universal Render Pipeline/Lit", new Color(0.75f, 0.76f, 0.78f), 0.4f);
            Material knobMaterial = MakeMaterial("LeverKnob", "Universal Render Pipeline/Lit", new Color(0.80f, 0.08f, 0.08f), 0.3f);
            Material darkMaterial = MakeMaterial("Dark", "Universal Render Pipeline/Lit", new Color(0.10f, 0.10f, 0.12f), 0.2f);

            // Fixed part: the switch box on the table and the block that holds the hinge.
            Part(PrimitiveType.Cube, "SwitchBox", parts.board,
                new Vector3(LeverX, TableTop + SwitchBoxSize.y / 2f, LeverZ), SwitchBoxSize, boxMaterial);
            Part(PrimitiveType.Cube, "HingeBlock", parts.board,
                new Vector3(LeverX, TableTop + SwitchBoxSize.y + 0.015f, LeverZ), new Vector3(0.11f, 0.03f, 0.05f), darkMaterial);
            Label(parts.board, "SwitchLabel", "ENERGÍA\n<size=34>arriba: ENCENDIDA\nhacia ti: APAGADA</size>",
                new Vector3(LeverX, TableTop + SwitchBoxSize.y / 2f, LeverZ - SwitchBoxSize.z / 2f - 0.0015f),
                Quaternion.identity, 450, 420, 0.15f, 70, new Color(0.08f, 0.08f, 0.08f));

            // Moving part. Its local X is the hinge: 0 degrees = arm up (ON), 90 = arm toward the user (OFF).
            Transform pivot = Group("Lever", parts.board, LeverPivot);
            GameObject hinge = Part(PrimitiveType.Cylinder, "Hinge", pivot, Vector3.zero, new Vector3(0.04f, 0.05f, 0.04f), darkMaterial);
            hinge.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Part(PrimitiveType.Cylinder, "Arm", pivot, new Vector3(0f, LeverArm / 2f, 0f), new Vector3(0.035f, LeverArm / 2f, 0.035f), armMaterial);
            Part(PrimitiveType.Sphere, "Knob", pivot, new Vector3(0f, LeverArm, 0f), Vector3.one * 0.06f, knobMaterial);

            // The hand grabs the lever by its red knob only (the primitives' own colliders were removed).
            // A box along the whole arm overlapped the padlock's grab volume once the padlock was on it:
            // pulling the lever could grab the padlock instead and take it off (open issue B). This box
            // stays about 1 cm away from the padlock. Check in the headset that the knob is still easy to grab.
            var grabVolume = pivot.gameObject.AddComponent<BoxCollider>();
            grabVolume.center = new Vector3(0f, LeverArm + 0.03f, 0f);
            grabVolume.size = new Vector3(0.07f, 0.085f, 0.07f);
            var body = pivot.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var lever = pivot.gameObject.AddComponent<Lever>();
            Wire(lever, "session", parts.session);
            parts.lever = lever;
        }

        static void BuildPadlock(Parts parts)
        {
            Material bodyMaterial = MakeMaterial("PadlockBody", "Universal Render Pipeline/Lit", new Color(0.85f, 0.10f, 0.10f), 0.3f);
            Material steelMaterial = MakeMaterial("PadlockSteel", "Universal Render Pipeline/Lit", new Color(0.80f, 0.82f, 0.85f), 0.5f);
            Material trayMaterial = MakeMaterial("Tray", "Universal Render Pipeline/Lit", new Color(0.95f, 0.50f, 0.10f), 0.1f);
            Material postMaterial = MakeMaterial("LockPost", "Universal Render Pipeline/Lit", new Color(0.95f, 0.80f, 0.10f), 0.1f);

            // Where it rests (its name on the table is added in Build, unless a plan B button takes that place).
            Part(PrimitiveType.Cube, "PadlockTray", parts.board,
                new Vector3(PadlockHome.x, TableTop + 0.004f, PadlockHome.z), new Vector3(0.14f, 0.008f, 0.10f), trayMaterial);

            // The lock point: fixed to the board, under the place where the arm lies in OFF. A post marks it.
            Transform anchor = Group("LockPoint", parts.board, LockPoint);
            float postTop = LockPoint.y - 0.065f;
            Part(PrimitiveType.Cube, "LockPost", parts.board,
                new Vector3(LockPoint.x, (TableTop + postTop) / 2f, LockPoint.z), new Vector3(0.03f, postTop - TableTop, 0.03f), postMaterial);

            // The padlock: 12 cm tall, 8 cm wide. Its origin is its center.
            Transform padlock = Group("Padlock", parts.board, PadlockHome);
            Part(PrimitiveType.Cube, "Body", padlock, new Vector3(0f, -0.025f, 0f), new Vector3(0.08f, 0.07f, 0.03f), bodyMaterial);
            Part(PrimitiveType.Cube, "ShackleLeft", padlock, new Vector3(-0.028f, 0.03f, 0f), new Vector3(0.012f, 0.04f, 0.012f), steelMaterial);
            Part(PrimitiveType.Cube, "ShackleRight", padlock, new Vector3(0.028f, 0.03f, 0f), new Vector3(0.012f, 0.04f, 0.012f), steelMaterial);
            Part(PrimitiveType.Cube, "ShackleTop", padlock, new Vector3(0f, 0.054f, 0f), new Vector3(0.068f, 0.012f, 0.012f), steelMaterial);

            // The lockout tag (LockoutTag shows it only while the padlock is locked). Measured against the lock
            // point: in front of the yellow post's face, below the padlock body. No colliders (Part removes them),
            // so the padlock's grab volume below is the same as before.
            Material cardMaterial = MakeMaterial("TagCard", "Universal Render Pipeline/Lit", new Color(0.97f, 0.97f, 0.95f), 0.1f);
            Transform tag = Group("Tag", padlock, new Vector3(0f, -0.115f, -0.022f));
            Part(PrimitiveType.Cube, "TagWire", tag, new Vector3(0f, 0.049f, 0f), new Vector3(0.004f, 0.012f, 0.004f), steelMaterial);
            Part(PrimitiveType.Cube, "TagCard", tag, Vector3.zero, new Vector3(0.065f, 0.085f, 0.003f), cardMaterial);
            Text tagText = Label(tag, "TagText", "", new Vector3(0f, 0f, -0.0025f), Quaternion.identity,
                270, 350, 0.063f, 40, new Color(0.08f, 0.08f, 0.08f));
            tag.gameObject.SetActive(false);

            var grabVolume = padlock.gameObject.AddComponent<BoxCollider>();
            grabVolume.center = Vector3.zero;
            grabVolume.size = new Vector3(0.09f, 0.12f, 0.05f);
            var body = padlock.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var script = padlock.gameObject.AddComponent<Padlock>();
            script.snapDistance = 0.10f;
            Wire(script, "session", parts.session);
            Wire(script, "lever", parts.lever);
            Wire(script, "anchor", anchor);
            parts.padlock = script;

            var tagScript = padlock.gameObject.AddComponent<LockoutTag>();
            Wire(tagScript, "session", parts.session);
            Wire(tagScript, "padlock", script);
            Wire(tagScript, "tagRoot", tag.gameObject);
            Wire(tagScript, "tagText", tagText);

            DressPadlock(padlock);   // LotoSceneBuilder.Models.cs: the team's padlock model, if present (looks only)
        }

        /// <summary>A flat button on the table, pressed by touching it with a fingertip. Its text is on the cap.</summary>
        static VerifyButton BuildButton(Parts parts, string name, Vector3 position, string text,
            VerifyButton.ButtonAction action, Color capColor)
        {
            Material baseMaterial = MakeMaterial("ButtonBase", "Universal Render Pipeline/Lit", new Color(0.12f, 0.12f, 0.14f), 0.2f);
            Material capMaterial = MakeMaterial(name + "Cap", "Universal Render Pipeline/Lit", capColor, 0.2f);

            Transform root = Group(name, parts.board, position);
            Transform visual = Group("Visual", root, Vector3.zero);
            Part(PrimitiveType.Cube, "Base", visual, new Vector3(0f, 0.015f, 0f), new Vector3(0.16f, 0.03f, 0.12f), baseMaterial);
            Transform cap = Group("Cap", visual, new Vector3(0f, 0.04f, 0f));
            Part(PrimitiveType.Cube, "CapMesh", cap, Vector3.zero, new Vector3(0.13f, 0.02f, 0.09f), capMaterial);
            Text label = Label(cap, "Label", text, new Vector3(0f, 0.0115f, 0f), Quaternion.Euler(90f, 0f, 0f),
                390, 270, 0.13f, 52, Color.white);

            // The fingertip sphere entering this volume is the press.
            var trigger = root.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.055f, 0f);
            trigger.size = new Vector3(0.13f, 0.035f, 0.09f);

            var button = root.gameObject.AddComponent<VerifyButton>();
            button.action = action;
            button.pressedOffset = new Vector3(0f, -0.012f, 0f);
            Wire(button, "session", parts.session);
            Wire(button, "cap", cap);
            if (action == VerifyButton.ButtonAction.Retry)
            {
                Wire(button, "label", label);
                Wire(button, "visual", visual.gameObject);
            }
            if (action == VerifyButton.ButtonAction.ToggleLever) Wire(button, "lever", parts.lever);
            if (action == VerifyButton.ButtonAction.TogglePadlock) Wire(button, "padlock", parts.padlock);
            return button;
        }

        static DangerZone BuildMachine(Parts parts)
        {
            Material machineMaterial = MakeMaterial("Machine", "Universal Render Pipeline/Lit", new Color(0.15f, 0.30f, 0.50f), 0.3f);
            Material bladeMaterial = MakeMaterial("Blade", "Universal Render Pipeline/Lit", new Color(0.95f, 0.45f, 0.05f), 0.3f);
            Material darkMaterial = MakeMaterial("Dark", "Universal Render Pipeline/Lit", new Color(0.10f, 0.10f, 0.12f), 0.2f);

            Transform machine = Group("Machine", parts.board, RealScaleMachines ? MachineFloorPosition : MachinePosition);
            if (RealScaleMachines) machine.gameObject.AddComponent<FloorStand>();
            GameObject body = Part(PrimitiveType.Cube, "Body", machine, new Vector3(0f, MachineSide / 2f, 0f), Vector3.one * MachineSide, machineMaterial);
            Label(machine, "MachineLabel", "MÁQUINA", new Vector3(0f, MachineSide / 2f, -MachineSide / 2f - 0.0015f),
                Quaternion.identity, 360, 100, 0.18f, 66, Color.white);

            // Energy lamp above the MÁQUINA label (EnergyFeedback: red with energy, grey without, amber blink on VERIFY).
            Material lampMaterial = MakeMaterial("EnergyLamp", "Universal Render Pipeline/Unlit", new Color(0.95f, 0.10f, 0.08f), 0f);
            GameObject lamp = Part(PrimitiveType.Sphere, "EnergyLamp", machine,
                new Vector3(-0.065f, MachineSide - 0.035f, -MachineSide / 2f - 0.006f), Vector3.one * 0.035f, lampMaterial);
            // Shown for 2.5 s after VERIFY. Centered 0.40 m above the table: leaves 0.25-0.31 free for the step pointer.
            // 760 px wide: at 520 "PRUEBA DE ARRANQUE" (~669 px in bold 56) wrapped and Truncate dropped "NO ARRANCA"
            // (adversarial review, 20/11). Same scale (0.0005 m/px), 0.38 m across.
            Text tryOut = Label(machine, "TryOutLabel", "PRUEBA DE ARRANQUE\nNO ARRANCA", new Vector3(0f, MachineSide + 0.20f, 0f),
                Quaternion.identity, 760, 160, 0.38f, 56, new Color(0.49f, 0.88f, 0.54f));
            GameObject tryOutCanvas = tryOut.transform.parent.gameObject;
            // Opaque plate behind the text (canvas pixels, 3 mm back): Loto/UI never writes alpha, so without it the
            // label is added over passthrough and washes out on a bright wall (audit 21/11, C3). Hidden with the canvas.
            Part(PrimitiveType.Quad, "TryOutPlate", tryOutCanvas.transform, new Vector3(0f, 0f, 6f), new Vector3(760f, 160f, 1f), darkMaterial);

            // The part that turns while there is energy (DangerZone spins it around its local Y).
            Transform spinner = Group("Spinner", machine, new Vector3(0f, MachineSide + 0.015f, 0f));
            Part(PrimitiveType.Cylinder, "Hub", spinner, Vector3.zero, new Vector3(0.05f, 0.012f, 0.05f), darkMaterial);
            Part(PrimitiveType.Cube, "BladeA", spinner, Vector3.zero, new Vector3(0.20f, 0.012f, 0.035f), bladeMaterial);
            Part(PrimitiveType.Cube, "BladeB", spinner, Vector3.zero, new Vector3(0.035f, 0.012f, 0.20f), bladeMaterial);

            // The danger zone: a trigger box 3.5 cm bigger than the machine on every side.
            Transform zone = Group("DangerZone", machine, new Vector3(0f, 0.12f, 0f));
            var trigger = zone.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.27f, 0.28f, 0.27f);
            var dangerZone = zone.gameObject.AddComponent<DangerZone>();
            Wire(dangerZone, "session", parts.session);
            Wire(dangerZone, "lever", parts.lever);
            Wire(dangerZone, "spinner", spinner);

            // Sound. spatialize = false everywhere: the project's spatializer (Meta XR Audio) is not needed for this.
            // The hum is 3D and comes from the machine; the event sounds are 2D, at a steady volume for the recording.
            AudioSource humSource = machine.gameObject.AddComponent<AudioSource>();
            humSource.playOnAwake = false;
            humSource.loop = true;
            humSource.spatialBlend = 1f;
            humSource.spatialize = false;
            humSource.rolloffMode = AudioRolloffMode.Linear;
            humSource.minDistance = 0.6f;
            humSource.maxDistance = 6f;
            AudioSource sfxSource = machine.gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.spatialize = false;

            var energy = machine.gameObject.AddComponent<EnergyFeedback>();
            Wire(energy, "session", parts.session);
            Wire(energy, "lever", parts.lever);
            Wire(energy, "lamp", lamp.GetComponent<MeshRenderer>());
            Wire(energy, "hum", humSource);
            Wire(energy, "sfx", sfxSource);
            Wire(energy, "tryOutLabel", tryOutCanvas);
            tryOutCanvas.SetActive(false);
            DressMachine(machine, body);   // LotoSceneBuilder.Models.cs: the Kenney model, if present
            return dangerZone;
        }

        static StatusPanel BuildPanel(Parts parts)
        {
            // Unlit and opaque: the panel changes this material's color at run time.
            Material plateMaterial = MakeMaterial("PanelBack", "Universal Render Pipeline/Unlit", StatusPanel.PlateNormal, 0f);

            Transform root = Group("StatusPanel", null, PanelCenter);
            GameObject plate = Part(PrimitiveType.Quad, "BackPlate", root, new Vector3(0f, 0f, 0.004f),
                new Vector3(StatusPanel.MetersWide, StatusPanel.MetersHigh, 1f), plateMaterial);

            RectTransform canvas = StatusPanel.MakeCanvas(root, "Canvas", StatusPanel.PixelsWide, StatusPanel.PixelsHigh, StatusPanel.MetersWide);
            Text header = StatusPanel.AddText(canvas, "Header", "BLOQUEO Y ETIQUETADO", _font, _uiMaterial, 48, FontStyle.Bold,
                StatusPanel.TextMain, TextAnchor.MiddleLeft, 45f, 25f, 1150f, 75f);
            Text clock = StatusPanel.AddText(canvas, "Clock", "0:00", _font, _uiMaterial, 48, FontStyle.Bold,
                StatusPanel.TextSoft, TextAnchor.MiddleRight, 1205f, 25f, 250f, 75f);
            Text steps = StatusPanel.AddText(canvas, "Steps", "", _font, _uiMaterial, 44, FontStyle.Normal,
                StatusPanel.TextMain, TextAnchor.UpperLeft, 45f, 120f, 1410f, 268f);   // 4 lines of 44 px + the "Tu orden" line of 26 px; ends at y 388, Notice starts at 395
            Text notice = StatusPanel.AddText(canvas, "Notice", "", _font, _uiMaterial, 36, FontStyle.Normal,
                StatusPanel.TextSoft, TextAnchor.UpperLeft, 45f, 395f, 1410f, 285f);

            var panel = root.gameObject.AddComponent<StatusPanel>();
            Wire(panel, "session", parts.session);
            Wire(panel, "backPlate", plate.GetComponent<MeshRenderer>());
            Wire(panel, "header", header);
            Wire(panel, "clock", clock);
            Wire(panel, "steps", steps);
            Wire(panel, "notice", notice);
            return panel;
        }

        /// <summary>The pin over the missing step (StepPointer). No colliders: it cannot press buttons or touch the machine.</summary>
        static void BuildStepPointer(Parts parts, VerifyButton verifyButton, DangerZone dangerZone)
        {
            Material material = MakeMaterial("StepPointer", "Universal Render Pipeline/Unlit", new Color(1.00f, 0.65f, 0.10f), 0f);
            Transform root = Group("StepPointer", null, Vector3.zero);
            Transform visual = Group("Visual", root, Vector3.zero);
            GameObject head = Part(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 0.055f, 0f), Vector3.one * 0.035f, material);
            GameObject stem = Part(PrimitiveType.Cylinder, "Stem", visual, new Vector3(0f, 0.02f, 0f), new Vector3(0.01f, 0.02f, 0.01f), material);

            var pointer = root.gameObject.AddComponent<StepPointer>();
            Wire(pointer, "session", parts.session);
            Wire(pointer, "visual", visual.gameObject);
            Wire(pointer, "head", head.GetComponent<MeshRenderer>());
            Wire(pointer, "stem", stem.GetComponent<MeshRenderer>());
            Wire(pointer, "target1", FindChild(parts.lever.transform, "Knob"));
            Wire(pointer, "target2", parts.padlock.transform);
            Wire(pointer, "target3", verifyButton.transform);
            // The machine's label (on its front plate, wherever MachineSelector puts it): the machine root is on the floor.
            Transform machineLabel = FindChild(dangerZone.transform.parent, "MachineLabel");
            Wire(pointer, "target4", machineLabel != null ? machineLabel : dangerZone.transform.parent);
            visual.gameObject.SetActive(false);
        }

        /// <summary>The trigger sphere that follows one index fingertip (parked far below until a hand is tracked).</summary>
        static FingerTip BuildFingerTip(string name, string handednessName)
        {
            Material tipMaterial = MakeMaterial("FingerTip", "Universal Render Pipeline/Unlit", new Color(1.00f, 0.85f, 0.10f), 0f);
            GameObject sphere = Part(PrimitiveType.Sphere, name, null, new Vector3(0f, -100f, 0f), Vector3.one * 0.018f, tipMaterial);
            var trigger = sphere.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;      // local: 0.6 * 0.018 m = about 1.1 cm in the world
            var body = sphere.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var tip = sphere.AddComponent<FingerTip>();
            SetEnum(tip, "handedness", handednessName);
            return tip;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>A primitive without its collider, with an opaque material and no shadows.</summary>
        public static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }

        public static Transform Group(string name, Transform parent, Vector3 localPosition)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.localPosition = localPosition;
            return group.transform;
        }

        /// <summary>One centered line of text on its own small world canvas, metersWide across.</summary>
        static Text Label(Transform parent, string name, string content, Vector3 localPosition, Quaternion localRotation,
            int pixelsWide, int pixelsHigh, float metersWide, int fontSize, Color color)
        {
            RectTransform canvas = StatusPanel.MakeCanvas(parent, name, pixelsWide, pixelsHigh, metersWide);
            canvas.localPosition = localPosition;
            canvas.localRotation = localRotation;
            return StatusPanel.AddText(canvas, "Text", content, _font, _uiMaterial, fontSize, FontStyle.Bold, color,
                TextAnchor.MiddleCenter, 0f, 0f, pixelsWide, pixelsHigh);
        }

        /// <summary>Sets a serialized object reference by field name, as the inspector would. A wrong name stops the build.</summary>
        public static void Wire(Object target, string field, Object value)
        {
            var data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field);
            if (property == null)
                throw new System.InvalidOperationException(target.GetType().Name + " has no serialized field '" + field + "'");
            property.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
            if (value != null && property.objectReferenceValue == null)
                throw new System.InvalidOperationException(target.GetType().Name + "." + field + " does not accept a " + value.GetType().Name);
        }

        /// <summary>Sets a serialized enum by the name of its value (no need to name the enum's type here).</summary>
        public static void SetEnum(Object target, string field, string valueName)
        {
            var data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field);
            if (property == null)
                throw new System.InvalidOperationException(target.GetType().Name + " has no serialized field '" + field + "'");
            int index = System.Array.IndexOf(property.enumNames, valueName);
            if (index < 0)
                throw new System.InvalidOperationException(target.GetType().Name + "." + field + " has no value '" + valueName + "'");
            property.enumValueIndex = index;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Transform FindChild(Transform parent, string childName)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t;
            return null;
        }

        /// <summary>Creates or updates Assets/Loto/Materials/[name].mat. Always opaque: translucent opens holes in passthrough.</summary>
        public static Material MakeMaterial(string name, string shaderName, Color color, float smoothness)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.InvalidOperationException("Shader not found: " + shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
