#define LOTO_RIG   // comment this line out to build the scene without the Meta camera rig
#if LOTO_RIG
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Loto.EditorTools
{
    /// <summary>
    /// The Meta camera rig with passthrough (Meta XR Core SDK only). Same steps as the team's Walkback
    /// project, which ran on the Quest 3 with this SDK version.
    /// </summary>
    public static partial class LotoSceneBuilder
    {
        public const string RigPrefabPath = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";

        static partial void BuildRig(Parts parts)
        {
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (rigPrefab == null) throw new FileNotFoundException("Meta camera rig prefab not found", RigPrefabPath);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rig.name = "OVRCameraRig";
            // Plain objects from here on: every change below is saved with the scene as it is.
            PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var manager = rig.GetComponent<OVRManager>();
            manager.isInsightPassthroughEnabled = true;
            var managerData = new SerializedObject(manager);
            // Floor level: y = 0 is the real floor, so the table top is at 0.90 m for everybody.
            managerData.FindProperty("_trackingOriginType").intValue = (int)OVRManager.TrackingOrigin.FloorLevel;
            managerData.ApplyModifiedPropertiesWithoutUndo();

            // In this SDK version the passthrough layer is always the background (underlay).
            rig.AddComponent<OVRPassthroughLayer>();
            foreach (var eye in rig.GetComponentsInChildren<Camera>(true))
            {
                // Transparent black: where nothing is drawn, the room shows through.
                eye.clearFlags = CameraClearFlags.SolidColor;
                eye.backgroundColor = new Color(0f, 0f, 0f, 0f);
                eye.nearClipPlane = 0.05f;
            }

            // Passthrough and hand tracking declared for the build (Assets/Oculus/OculusProjectConfig.asset).
            var projectConfig = OVRProjectConfig.CachedProjectConfig;
            bool changed = false;
            if (projectConfig.insightPassthroughSupport == OVRProjectConfig.FeatureSupport.None)
            {
                projectConfig.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Supported;
                changed = true;
            }
            if (projectConfig.handTrackingSupport != OVRProjectConfig.HandTrackingSupport.ControllersAndHands)
            {
                projectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
                changed = true;
            }
            if (changed) OVRProjectConfig.CommitProjectConfig(projectConfig);

            // With controllers (no tracked hand) the fingertip spheres follow the controller anchors.
            Wire(parts.leftTip, "fallbackAnchor", FindChild(rig.transform, "LeftControllerAnchor"));
            Wire(parts.rightTip, "fallbackAnchor", FindChild(rig.transform, "RightControllerAnchor"));

            parts.rig = rig;
        }
    }
}
#endif
