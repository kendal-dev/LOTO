using UnityEngine;

namespace Loto
{
    /// <summary>
    /// The stored energy made visible: while PURGAR lets the pressure out, puffs (air, or oil mist on the press) shoot
    /// out of the valve and a hiss plays, both fading with the gauge. Opaque puffs that shrink away instead of fading
    /// out: translucent materials would open holes in the passthrough. Looks and sound only.
    /// </summary>
    public class PurgeJet : MonoBehaviour
    {
        public DangerZone zone;
        public Transform emitter;
        public Vector3 direction = new Vector3(0f, 0.3f, -1f);   // in this object's space: toward the user, a bit up
        public Color color = new Color(0.92f, 0.94f, 0.96f);
        public float size = 0.035f;
        public float hissVolume = 0.5f;

        const int Count = 16;
        const float Life = 0.7f;
        Transform[] _puffs;
        Vector3[] _velocity;
        float[] _age;
        int _next;
        float _spawnAt;
        AudioSource _hiss;

        void Start()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            _puffs = new Transform[Count];
            _velocity = new Vector3[Count];
            _age = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Puff";
                Destroy(puff.GetComponent<Collider>());
                var r = puff.GetComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                puff.transform.SetParent(transform, false);
                puff.SetActive(false);
                _puffs[i] = puff.transform;
            }
            _hiss = gameObject.AddComponent<AudioSource>();
            _hiss.clip = ToneSynth.Hiss();
            _hiss.loop = true;
            _hiss.spatialBlend = 1f;
            _hiss.spatialize = false;
            _hiss.rolloffMode = AudioRolloffMode.Linear;
            _hiss.minDistance = 0.6f;
            _hiss.maxDistance = 6f;
            _hiss.volume = 0f;
            _hiss.Play();
        }

        void Update()
        {
            float pressure = zone != null ? zone.Speed : 0f;
            bool venting = zone != null && zone.Bled && pressure > 0.02f && emitter != null;
            _hiss.volume = venting ? hissVolume * pressure : Mathf.MoveTowards(_hiss.volume, 0f, Time.deltaTime * 2f);

            if (venting && Time.time >= _spawnAt)
            {
                _spawnAt = Time.time + 0.04f;
                Transform p = _puffs[_next];
                p.position = emitter.position;
                Vector3 dir = transform.TransformDirection(direction).normalized + Random.insideUnitSphere * 0.35f;
                _velocity[_next] = dir.normalized * (0.6f + 0.8f * pressure);
                _age[_next] = 0f;
                p.gameObject.SetActive(true);
                _next = (_next + 1) % Count;
            }

            float drag = Mathf.Pow(0.05f, Time.deltaTime);
            for (int i = 0; i < Count; i++)
            {
                Transform p = _puffs[i];
                if (!p.gameObject.activeSelf) continue;
                _age[i] += Time.deltaTime;
                if (_age[i] > Life) { p.gameObject.SetActive(false); continue; }
                _velocity[i] *= drag;
                p.position += (_velocity[i] + Vector3.up * 0.15f) * Time.deltaTime;
                float t = _age[i] / Life;
                p.localScale = Vector3.one * size * (1f + 3f * t) * (1f - t);
            }
        }
    }
}
