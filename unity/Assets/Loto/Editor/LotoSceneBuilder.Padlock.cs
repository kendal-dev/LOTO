using UnityEditor;
using UnityEngine;

namespace Loto.EditorTools
{
    /// <summary>
    /// A realistic lockout padlock built in code (no free model was good enough, 21/11): red anodized body with rounded
    /// vertical edges, a round chrome shackle (a real half torus, generated and saved as a mesh asset), a keyhole and a
    /// "PELIGRO" label. Same size and origin as the cube padlock, so the grab volume, the snapping and the lockout tag
    /// stay the same. Looks only.
    /// </summary>
    public static partial class LotoSceneBuilder
    {
        const string MeshFolder = "Assets/Loto/Meshes";

        /// <summary>True if the padlock was dressed (the team's model is then not needed).</summary>
        static bool BuildRealisticPadlock(Transform padlock)
        {
            Material body = MakeMaterial("PadlockAnodized", "Universal Render Pipeline/Lit", new Color(0.72f, 0.06f, 0.05f), 0.72f);
            if (body.HasProperty("_Metallic")) body.SetFloat("_Metallic", 0.35f);
            Material chrome = MakeMaterial("PadlockChrome", "Universal Render Pipeline/Lit", new Color(0.86f, 0.87f, 0.89f), 0.88f);
            if (chrome.HasProperty("_Metallic")) chrome.SetFloat("_Metallic", 1f);
            Material dark = MakeMaterial("PadlockKeyhole", "Universal Render Pipeline/Lit", new Color(0.05f, 0.05f, 0.06f), 0.5f);
            Material plate = MakeMaterial("PadlockLabel", "Universal Render Pipeline/Unlit", new Color(0.97f, 0.97f, 0.95f), 0f);

            Transform root = Group("RealPadlock", padlock, Vector3.zero);
            // Body: 7 x 6.5 x 3.2 cm, center at y -0.025 like the cube body. Rounded vertical edges (r = 8 mm).
            const float w = 0.070f, h = 0.065f, d = 0.032f, r = 0.008f, cy = -0.025f;
            Part(PrimitiveType.Cube, "BodyCore", root, new Vector3(0f, cy, 0f), new Vector3(w - 2f * r, h, d), body);
            Part(PrimitiveType.Cube, "BodySide", root, new Vector3(0f, cy, 0f), new Vector3(w, h, d - 2f * r), body);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (w / 2f - r), z = (i < 2 ? -1f : 1f) * (d / 2f - r);
                Part(PrimitiveType.Cylinder, "BodyEdge", root, new Vector3(x, cy, z), new Vector3(2f * r, h / 2f, 2f * r), body);
            }
            // A thin chrome band on top, where the shackle goes in.
            Part(PrimitiveType.Cube, "TopPlate", root, new Vector3(0f, cy + h / 2f + 0.0015f, 0f), new Vector3(w - 0.006f, 0.003f, d - 0.006f), chrome);

            // Shackle: two legs and a half torus. Legs at x = +-2.2 cm, the arc centered 3 cm up.
            const float R = 0.022f, tube = 0.0055f, legBottom = cy + h / 2f, arcY = 0.030f;
            float legLength = arcY - legBottom;
            Part(PrimitiveType.Cylinder, "ShackleLegL", root, new Vector3(-R, legBottom + legLength / 2f, 0f), new Vector3(2f * tube, legLength / 2f, 2f * tube), chrome);
            Part(PrimitiveType.Cylinder, "ShackleLegR", root, new Vector3(R, legBottom + legLength / 2f, 0f), new Vector3(2f * tube, legLength / 2f, 2f * tube), chrome);
            GameObject arc = new GameObject("ShackleArc");
            arc.transform.SetParent(root, false);
            arc.transform.localPosition = new Vector3(0f, arcY, 0f);
            arc.AddComponent<MeshFilter>().sharedMesh = HalfTorus(R, tube);
            var arcRenderer = arc.AddComponent<MeshRenderer>();
            arcRenderer.sharedMaterial = chrome;
            arcRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Front: keyhole at the bottom, label in the middle.
            float front = -d / 2f - 0.0006f;
            GameObject hole = Part(PrimitiveType.Cylinder, "Keyhole", root, new Vector3(0f, cy - 0.020f, front), new Vector3(0.010f, 0.0008f, 0.010f), chrome);
            hole.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(PrimitiveType.Cube, "KeySlot", root, new Vector3(0f, cy - 0.020f, front - 0.0006f), new Vector3(0.0018f, 0.006f, 0.0008f), dark);
            Part(PrimitiveType.Quad, "LabelPlate", root, new Vector3(0f, cy + 0.006f, front - 0.0004f), new Vector3(0.052f, 0.026f, 1f), plate);
            Label(root, "LabelText", "<b>PELIGRO</b>\n<size=26>BLOQUEADO</size>", new Vector3(0f, cy + 0.006f, front - 0.0008f),
                Quaternion.identity, 300, 150, 0.050f, 44, new Color(0.75f, 0.05f, 0.04f));

            foreach (string part in new[] { "Body", "ShackleLeft", "ShackleRight", "ShackleTop" })
            {
                Transform t = padlock.Find(part);   // the cube padlock's own parts
                if (t != null && t.GetComponent<Renderer>() != null) t.GetComponent<Renderer>().enabled = false;
            }
            Debug.Log("[Loto] Padlock: realistic model built in code (chrome shackle, anodized body).");
            return true;
        }

        /// <summary>Half a torus in the XY plane, arching over +Y, ends pointing down at x = +-ringRadius. Saved as an asset.</summary>
        static Mesh HalfTorus(float ringRadius, float tubeRadius)
        {
            const int ringSegments = 32, tubeSegments = 16;
            var vertices = new Vector3[(ringSegments + 1) * (tubeSegments + 1)];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[ringSegments * tubeSegments * 6];
            for (int i = 0; i <= ringSegments; i++)
            {
                float a = Mathf.PI * i / ringSegments;                       // 0 .. 180 degrees
                var center = new Vector3(Mathf.Cos(a) * ringRadius, Mathf.Sin(a) * ringRadius, 0f);
                var outward = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                for (int j = 0; j <= tubeSegments; j++)
                {
                    float b = 2f * Mathf.PI * j / tubeSegments;
                    Vector3 n = outward * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    int k = i * (tubeSegments + 1) + j;
                    vertices[k] = center + n * tubeRadius;
                    normals[k] = n;
                }
            }
            int t = 0;
            for (int i = 0; i < ringSegments; i++)
                for (int j = 0; j < tubeSegments; j++)
                {
                    int a0 = i * (tubeSegments + 1) + j, a1 = a0 + 1, b0 = a0 + tubeSegments + 1, b1 = b0 + 1;
                    triangles[t++] = a0; triangles[t++] = b0; triangles[t++] = a1;   // clockwise seen from outside
                    triangles[t++] = a1; triangles[t++] = b0; triangles[t++] = b1;
                }
            var mesh = new Mesh { name = "ShackleArc", vertices = vertices, normals = normals, triangles = triangles };
            mesh.RecalculateBounds();
            if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/Loto", "Meshes");
            string path = MeshFolder + "/ShackleArc.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
