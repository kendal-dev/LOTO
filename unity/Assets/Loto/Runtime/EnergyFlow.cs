using UnityEngine;

namespace Loto
{
    /// <summary>
    /// Makes the energy visible: small lights run along the power cable from the switch box to the machine while
    /// the lever is ON, and the machine body trembles a little. Lever OFF: the lights go out at once and the machine
    /// stands still. Looks only: it never changes the attempt (the Referee decides everything).
    /// </summary>
    public class EnergyFlow : MonoBehaviour
    {
        public Lever lever;
        public Transform[] path;              // cable corners, in order, from the switch box to the machine
        public Transform shaker;              // the machine's visible model (trembles while energized)
        public int lights = 6;
        public float speed = 0.35f;           // meters per second along the cable
        public float lightSize = 0.014f;
        public Color lightColor = new Color(1.00f, 0.62f, 0.10f);
        public float shakeAmplitude = 0.0012f;

        Transform[] _dots;
        float[] _segment;
        float _length;
        Vector3 _shakerRest;

        void Start()
        {
            if (path == null || path.Length < 2) { enabled = false; return; }
            _segment = new float[path.Length - 1];
            for (int i = 0; i < _segment.Length; i++)
            {
                _segment[i] = Vector3.Distance(path[i].position, path[i + 1].position);
                _length += _segment[i];
            }
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", lightColor);
            _dots = new Transform[lights];
            for (int i = 0; i < lights; i++)
            {
                GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = "EnergyDot";
                Destroy(dot.GetComponent<Collider>());
                dot.GetComponent<MeshRenderer>().sharedMaterial = material;
                dot.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                dot.transform.SetParent(transform, false);
                dot.transform.localScale = Vector3.one * lightSize;
                _dots[i] = dot.transform;
            }
            if (shaker != null) _shakerRest = shaker.localPosition;
        }

        void Update()
        {
            bool energized = lever == null || !lever.IsOff;
            for (int i = 0; i < _dots.Length; i++)
            {
                _dots[i].gameObject.SetActive(energized);
                if (!energized) continue;
                float d = Mathf.Repeat(Time.time * speed + i * _length / _dots.Length, _length);
                _dots[i].position = PointAt(d) + Vector3.up * 0.01f;   // on top of the cable, not inside it
            }
            if (shaker != null)
            {
                shaker.localPosition = energized
                    ? _shakerRest + new Vector3(Mathf.Sin(Time.time * 61f), 0f, Mathf.Sin(Time.time * 47f)) * shakeAmplitude
                    : _shakerRest;
            }
        }

        // Distance along the cable -> world point (the path follows the board if it is moved onto a real table).
        Vector3 PointAt(float distance)
        {
            for (int i = 0; i < _segment.Length; i++)
            {
                if (distance <= _segment[i] || i == _segment.Length - 1)
                    return Vector3.Lerp(path[i].position, path[i + 1].position, _segment[i] > 0f ? Mathf.Clamp01(distance / _segment[i]) : 0f);
                distance -= _segment[i];
            }
            return path[path.Length - 1].position;
        }
    }
}
