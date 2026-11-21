using UnityEngine;
using UnityEngine.UI;

namespace Loto
{
    /// <summary>
    /// The "T" of LOTO: while the padlock is locked on the lever, a DANGER / DO NOT OPERATE tag hangs from it
    /// with the name of the student who locked it and the time. It only reads Padlock.Snapped, so it works the
    /// same with the hand grab, the key 2 and the plan B button CANDADO. Visual only: the Referee does not
    /// know about the tag (the 4 steps do not change).
    /// </summary>
    public class LockoutTag : MonoBehaviour
    {
        public LotoSession session;
        public Padlock padlock;
        public GameObject tagRoot;
        public Text tagText;

        bool _shown;

        void Start()
        {
            if (tagRoot != null) tagRoot.SetActive(false);
        }

        void LateUpdate()
        {
            bool show = padlock != null && padlock.Snapped;
            if (show == _shown) return;
            _shown = show;
            if (show && tagText != null && session != null)
            {
                tagText.text = "<color=#B00000>PELIGRO</color>\nNO OPERAR\n<size=26>Bloqueado por:\n" +
                    session.StudentName + "\n" + System.DateTime.Now.ToString("dd/MM HH:mm") + "</size>";
            }
            if (tagRoot != null) tagRoot.SetActive(show);
        }
    }
}
