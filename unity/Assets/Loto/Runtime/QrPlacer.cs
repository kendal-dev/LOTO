using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Loto
{
    /// <summary>
    /// Anchors the board to a QR code stuck on the real table, the way the Quest tracks a keyboard: MRUK reports QR
    /// codes as trackables (Meta, "Track QR codes in MR Utility Kit"; Quest 3/3S, spatial data permission). The center
    /// of the table top goes on the QR code and follows it if it is moved. Any QR code works; one that says "LOTO" wins.
    /// Without QR support (the editor, the desktop scene) it does nothing and TablePlacer keeps working as before.
    /// </summary>
    public class QrPlacer : MonoBehaviour
    {
        public TablePlacer placer;
        [Tooltip("Meters the QR code has to move before the board follows it.")]
        public float moveThreshold = 0.05f;

        MRUKTrackable _qr;
        Vector3 _placedAt;
        bool _placedOnce;
        bool _hooked;
        float _nextHook;
        MRUK _mruk;

        void Update()
        {
            if (!_hooked && Time.time >= _nextHook)
            {
                _nextHook = Time.time + 1f;
                Hook();
            }
            if (_qr == null || !_qr.IsTracked || placer == null) return;
            Vector3 p = _qr.transform.position;
            if (!_placedOnce || (p - _placedAt).sqrMagnitude > moveThreshold * moveThreshold)
            {
                if (placer.PlaceAt(p, "QR code")) { _placedAt = p; _placedOnce = true; }
            }
        }

        void Hook()
        {
            MRUK mruk = MRUK.Instance;
            if (mruk == null || mruk.SceneSettings == null) return;
            _hooked = true;
            bool supported;
            try { supported = mruk.QRCodeTrackingSupported; }
            catch (System.Exception) { supported = false; }
            if (!supported)
            {
                Debug.Log("[Loto] QR placement off (QR tracking not supported here).");
                return;
            }
            OVRAnchor.TrackerConfiguration config = mruk.SceneSettings.TrackerConfiguration;
            config.QRCodeTrackingEnabled = true;
            mruk.SceneSettings.TrackerConfiguration = config;
            mruk.SceneSettings.TrackableAdded.AddListener(OnAdded);
            mruk.SceneSettings.TrackableRemoved.AddListener(OnRemoved);
            _mruk = mruk;
            Debug.Log("[Loto] QR placement on: show a QR code on the table.");
        }

        void OnAdded(MRUKTrackable trackable)
        {
            if (trackable == null || trackable.TrackableType != OVRAnchor.TrackableType.QRCode) return;
            string payload = trackable.MarkerPayloadString ?? "";
            bool loto = payload.ToUpperInvariant().Contains("LOTO");
            // Keep the first QR code, unless a "LOTO" one shows up later.
            if (_qr == null || loto)
            {
                _qr = trackable;
                _placedOnce = false;
                Debug.Log("[Loto] QR code found: \"" + payload + "\"");
            }
        }

        void OnRemoved(MRUKTrackable trackable)
        {
            if (trackable == _qr) _qr = null;
        }

        void OnDestroy()
        {
            if (_mruk == null || _mruk.SceneSettings == null) return;
            _mruk.SceneSettings.TrackableAdded.RemoveListener(OnAdded);
            _mruk.SceneSettings.TrackableRemoved.RemoveListener(OnRemoved);
        }
    }
}
