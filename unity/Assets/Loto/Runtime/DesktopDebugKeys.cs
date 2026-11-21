using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Loto
{
    /// <summary>
    /// Play in the Editor without a headset: turns on the desktop camera and maps keys to the actions,
    /// through the same objects the hands use (the lever really turns, the padlock really snaps).
    ///   1 lever off · 2 padlock on · 3 verify · 4 touch the machine · 5 padlock off · 6 lever on
    ///   R retry / next (only when the attempt ended) · N new attempt with the next student
    /// The keys also work in the headset build with a keyboard, which nobody will plug in.
    /// </summary>
    public class DesktopDebugKeys : MonoBehaviour
    {
        public LotoSession session;
        public Lever lever;
        public Padlock padlock;
        public VerifyButton verifyButton;
        public DangerZone dangerZone;
        public Camera desktopCamera;

        void Start()
        {
            bool desktop = !XRSettings.isDeviceActive;
            if (desktopCamera != null) desktopCamera.enabled = desktop;
            Debug.Log(desktop
                ? "[Loto] Desktop mode (no headset). Keys: 1 lever off, 2 padlock on, 3 verify, 4 touch machine, 5 padlock off, 6 lever on, R retry, N next student."
                : "[Loto] Headset mode: " + XRSettings.loadedDeviceName);
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) lever.ForceAngle(lever.offAngle);
            if (keyboard.digit2Key.wasPressedThisFrame) padlock.DebugPutOn();
            if (keyboard.digit3Key.wasPressedThisFrame) verifyButton.Press();
            if (keyboard.digit4Key.wasPressedThisFrame) dangerZone.Touch();
            if (keyboard.digit5Key.wasPressedThisFrame) padlock.DebugTakeOff();
            if (keyboard.digit6Key.wasPressedThisFrame)
            {
                // With the padlock on, the lever cannot go up: report the try, as a hand pulling would.
                if (lever.Locked) session.OnLeverOn();
                else lever.ForceAngle(0f);
            }
            if (keyboard.rKey.wasPressedThisFrame && session.Ended) session.RetryOrNext();
            if (keyboard.nKey.wasPressedThisFrame) session.ResetAttempt(true);
            if (keyboard.mKey.wasPressedThisFrame)
            {
                MachineSelector selector = Object.FindAnyObjectByType<MachineSelector>();
                if (selector != null && selector.CanSwitch) selector.Next();
            }
            if (keyboard.pKey.wasPressedThisFrame && dangerZone != null) dangerZone.Bleed();
        }
    }
}
