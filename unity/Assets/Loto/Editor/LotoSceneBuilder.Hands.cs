#define LOTO_HANDS   // comment this line out to build the scene without the Interaction SDK (no hand grab)
#if LOTO_HANDS
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Loto.EditorTools
{
    /// <summary>
    /// Everything that touches Meta's Interaction SDK, kept in this one file: the interaction rig
    /// (hands, controllers and their interactors) and the grab on the lever and on the padlock.
    /// SDK types are written with their full name so a wrong one is easy to find and fix.
    /// If this file does not compile and there is no time: comment out its first line. The scene is
    /// then built without hand grab; the keys in the Editor, the fingertip buttons and the machine still work.
    /// </summary>
    public static partial class LotoSceneBuilder
    {
        /// <summary>Meta's rig with every interactor, to be placed under the OVRCameraRig (what the menu
        /// "Add OVR Comprehensive Interaction Rig" instantiates: OVRComprehensiveInteractionRigWizard).</summary>
        public const string InteractionRigGuid = "0a7d2469f24041c4284c66706f84c45e";
        public const string InteractionRigPath = "Packages/com.meta.xr.sdk.interaction.ovr/Runtime/Prefabs/OVRComprehensiveInteractionRig.prefab";

        static partial void BuildHands(Parts parts)
        {
            BuildInteractionRig(parts);

            // --- Lever: grabbed with one hand, it only turns around its local X, between ON (0) and OFF. ---
            GameObject leverObject = parts.lever.gameObject;
            var leverBody = leverObject.GetComponent<Rigidbody>();
            var leverGrabbable = leverObject.AddComponent<Oculus.Interaction.Grabbable>();
            leverGrabbable.MaxGrabPoints = 1;
            leverGrabbable.InjectOptionalThrowWhenUnselected(false);   // kinematic: nothing to throw

            var transformer = leverObject.AddComponent<Oculus.Interaction.OneGrabRotateTransformer>();
            transformer.InjectOptionalRotationAxis(Oculus.Interaction.OneGrabRotateTransformer.Axis.Right);
            // Same limits Lever.ApplyLimits sets at run time: the transformer's angle is direction * Lever.Angle.
            float end = parts.lever.direction * parts.lever.offAngle;
            transformer.InjectOptionalConstraints(new Oculus.Interaction.OneGrabRotateTransformer.OneGrabRotateConstraints
            {
                MinAngle = new Oculus.Interaction.FloatConstraint { Constrain = true, Value = Mathf.Min(0f, end) },
                MaxAngle = new Oculus.Interaction.FloatConstraint { Constrain = true, Value = Mathf.Max(0f, end) },
            });
            leverGrabbable.InjectOptionalOneGrabTransformer(transformer);
            Wire(parts.lever, "transformer", transformer);
            Wire(parts.lever, "grabbable", leverGrabbable);   // Lever waits for the release before a reset moves it
            AddGrabInteractables(leverObject, leverBody, leverGrabbable);

            // --- Padlock: grabbed freely (Grabbable creates its own free transformer at Start). ---
            GameObject padlockObject = parts.padlock.gameObject;
            var padlockBody = padlockObject.GetComponent<Rigidbody>();
            var padlockGrabbable = padlockObject.AddComponent<Oculus.Interaction.Grabbable>();
            padlockGrabbable.InjectOptionalThrowWhenUnselected(false);  // Padlock.cs decides where it goes on release
            Wire(parts.padlock, "grabbable", padlockGrabbable);
            AddGrabInteractables(padlockObject, padlockBody, padlockGrabbable);

            parts.handsBuilt = true;
        }

        /// <summary>Same as Meta's menu "Add Grab Interaction" (GrabWizard.Create): a child with the hand
        /// grab and the controller grab, both moving the object through its Grabbable.</summary>
        static void AddGrabInteractables(GameObject target, Rigidbody body, Oculus.Interaction.Grabbable grabbable)
        {
            var child = new GameObject("GrabInteraction");
            child.transform.SetParent(target.transform, false);

            var handGrab = child.AddComponent<Oculus.Interaction.HandGrab.HandGrabInteractable>();
            handGrab.InjectRigidbody(body);
            handGrab.InjectSupportedGrabTypes(Oculus.Interaction.Grab.GrabTypeFlags.All);
            handGrab.InjectOptionalPointableElement(grabbable);

            var controllerGrab = child.AddComponent<Oculus.Interaction.GrabInteractable>();
            controllerGrab.InjectRigidbody(body);
            controllerGrab.InjectOptionalPointableElement(grabbable);
        }

        static void BuildInteractionRig(Parts parts)
        {
            var cameraRig = parts.rig.GetComponent<OVRCameraRig>();

            string path = AssetDatabase.GUIDToAssetPath(InteractionRigGuid);
            if (string.IsNullOrEmpty(path)) path = InteractionRigPath;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException("Meta interaction rig prefab not found", InteractionRigPath);

            var interactionRig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            interactionRig.name = "OVRComprehensiveInteractionRig";
            interactionRig.transform.SetParent(parts.rig.transform, false);
            // Plain objects from here on: what the wiring below sets is saved with the scene as it is.
            PrefabUtility.UnpackPrefabInstance(interactionRig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // The SDK connects the new rig to the camera rig by itself (auto-wiring), as its own menu does.
            try
            {
                Oculus.Interaction.Editor.UnityObjectAddedBroadcaster.HandleObjectWasAdded(interactionRig);
                Undo.FlushUndoRecordObjects();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Loto] Meta's auto-wiring stopped (" + e.Message + "); the camera rig is wired by hand below.");
            }

            // Checked by hand, in case the auto-wiring did nothing.
            var rigRef = interactionRig.GetComponentInChildren<Oculus.Interaction.Input.OVRCameraRigRef>(true);
            if (rigRef == null) throw new System.InvalidOperationException("The interaction rig has no OVRCameraRigRef");
            WireIfEmpty(rigRef, "_ovrCameraRig", cameraRig);

            // The rig brings a locomotor (teleport, turn). It needs the player's root and eyes.
            foreach (MonoBehaviour behaviour in interactionRig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour.GetType().Name != "FirstPersonLocomotor") continue;
                WireIfEmpty(behaviour, "_playerOrigin", parts.rig.transform);
                WireIfEmpty(behaviour, "_playerEyes", FindChild(parts.rig.transform, "CenterEyeAnchor"));
            }

            if (DisableLocomotion) SwitchOffLocomotion(interactionRig);
        }

        /// <summary>
        /// true = the rig's teleport and turn are switched off when the scene is built. The student stands at
        /// a table: a turn or a teleport fired by a gesture by mistake would move the whole board away
        /// (open issue in TRASPASO §8). false = leave Meta's rig as it comes.
        /// </summary>
        public const bool DisableLocomotion = true;

        /// <summary>
        /// Deactivates every object of the interaction rig whose name says locomotion (Locomot, Teleport, Turner),
        /// unless it also holds a grab or poke interactor (those are needed for the lever, the padlock and the
        /// buttons). Names are read at build time because they come from nested prefabs of the SDK.
        /// The console lists what was switched off; if something needed disappears, set DisableLocomotion = false.
        /// </summary>
        static void SwitchOffLocomotion(GameObject interactionRig)
        {
            var switchedOff = new System.Collections.Generic.List<string>();
            foreach (Transform child in interactionRig.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || child.gameObject == interactionRig || !child.gameObject.activeSelf) continue;
                string name = child.name;
                bool locomotion = name.Contains("Locomot") || name.Contains("Teleport") || name.Contains("Turner");
                if (!locomotion || HoldsGrabOrPoke(child) || HasInactiveParent(child)) continue;
                child.gameObject.SetActive(false);
                switchedOff.Add(name);
            }
            Debug.Log(switchedOff.Count == 0
                ? "[Loto] Locomotion: nothing found to switch off (names changed in this SDK?). Turn and teleport may still fire."
                : "[Loto] Locomotion switched off (" + switchedOff.Count + "): " + string.Join(", ", switchedOff));
        }

        static bool HoldsGrabOrPoke(Transform root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                string type = behaviour.GetType().Name;
                if (type.Contains("GrabInteractor") || type.Contains("PokeInteractor")) return true;
            }
            return false;
        }

        static bool HasInactiveParent(Transform child)
        {
            for (Transform p = child.parent; p != null; p = p.parent)
                if (!p.gameObject.activeSelf) return true;
            return false;
        }

        /// <summary>Sets a serialized reference only when it is still empty. A missing field is only a warning here.</summary>
        static void WireIfEmpty(Object target, string field, Object value)
        {
            var data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Loto] " + target.GetType().Name + " has no field '" + field + "' in this SDK version.");
                return;
            }
            if (property.objectReferenceValue != null) return;
            property.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Loto] Wired by hand: " + target.GetType().Name + "." + field);
        }
    }
}
#endif
