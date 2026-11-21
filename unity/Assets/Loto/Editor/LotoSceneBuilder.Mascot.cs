using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Loto.EditorTools
{
    /// <summary>"Chispa", the safety assistant (MascotGuide): a small glossy robot with a dark visor and glowing eyes,
    /// a speech bubble with an opaque plate (passthrough-safe) and its own voice source. No colliders.</summary>
    public static partial class LotoSceneBuilder
    {
        static partial void BuildMascot(Parts parts)
        {
            Material shell = MakeMaterial("MascotShell", "Universal Render Pipeline/Lit", new Color(0.95f, 0.96f, 0.97f), 0.85f);
            Material visor = MakeMaterial("MascotVisor", "Universal Render Pipeline/Lit", new Color(0.04f, 0.05f, 0.08f), 0.95f);
            Material accent = MakeMaterial("MascotAccent", "Universal Render Pipeline/Lit", new Color(0.98f, 0.55f, 0.08f), 0.6f);
            Material dark = MakeMaterial("MascotBubble", "Universal Render Pipeline/Unlit", new Color(0.08f, 0.10f, 0.14f), 0f);

            Transform root = Group("Chispa", null, new Vector3(-0.45f, 1.30f, 0.55f));
            Transform body = Group("Body", root, Vector3.zero);
            Part(PrimitiveType.Sphere, "Shell", body, Vector3.zero, new Vector3(0.17f, 0.15f, 0.16f), shell);
            Part(PrimitiveType.Sphere, "Visor", body, new Vector3(0f, 0.008f, -0.052f), new Vector3(0.13f, 0.085f, 0.07f), visor);
            var eyes = new Renderer[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject e = Part(PrimitiveType.Sphere, "Eye", body, new Vector3(i == 0 ? -0.026f : 0.026f, 0.010f, -0.085f),
                    new Vector3(0.022f, 0.026f, 0.008f), MakeMaterial("MascotEye" + i, "Universal Render Pipeline/Unlit", new Color(0.25f, 0.95f, 1f), 0f));
                eyes[i] = e.GetComponent<Renderer>();
            }
            Part(PrimitiveType.Cylinder, "EarL", body, new Vector3(-0.088f, 0f, 0f), new Vector3(0.035f, 0.008f, 0.035f), accent).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Part(PrimitiveType.Cylinder, "EarR", body, new Vector3(0.088f, 0f, 0f), new Vector3(0.035f, 0.008f, 0.035f), accent).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Part(PrimitiveType.Cylinder, "Antenna", body, new Vector3(0f, 0.095f, 0f), new Vector3(0.006f, 0.022f, 0.006f), visor);
            Part(PrimitiveType.Sphere, "AntennaTip", body, new Vector3(0f, 0.122f, 0f), Vector3.one * 0.018f, accent);

            // Speech bubble above Chispa, facing the student like every label (-Z).
            Text text = Label(root, "Bubble", "", new Vector3(0f, 0.20f, 0f), Quaternion.identity, 900, 300, 0.42f, 36, Color.white);
            GameObject bubble = text.transform.parent.gameObject;
            Part(PrimitiveType.Quad, "BubblePlate", bubble.transform, new Vector3(0f, 0f, 6f), new Vector3(920f, 320f, 1f), dark);
            text.alignment = TextAnchor.MiddleCenter;
            bubble.SetActive(false);

            AudioSource voice = root.gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0.6f;
            voice.spatialize = false;

            var guide = root.gameObject.AddComponent<MascotGuide>();
            Wire(guide, "session", parts.session);
            Wire(guide, "body", body);
            guide.eyes = eyes;
            Wire(guide, "bubbleText", text);
            Wire(guide, "bubble", bubble);
            Wire(guide, "voice", voice);
            Debug.Log("[Loto] Chispa (safety assistant) added.");
        }
    }
}
