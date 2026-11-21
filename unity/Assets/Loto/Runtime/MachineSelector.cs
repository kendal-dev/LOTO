using UnityEngine;

namespace Loto
{
    /// <summary>
    /// The machines on the table: the hammer mill, the hydraulic press and the pneumatic station (the team's models).
    /// One shows at a time. A retry keeps the same machine; the next student gets the next one. The four LOTO steps
    /// and the Referee are the same for all of them: only the looks, the label and the stored-energy notice change.
    /// </summary>
    public class MachineSelector : MonoBehaviour
    {
        public LotoSession session;
        public GameObject[] machines;
        public string[] names;              // the label on the machine
        public string[] hints;              // VERIFY while the energy is still stored; {0} = seconds left
        public Vector3[] labelPositions;
        public Vector3[] lampPositions;
        public Transform label;
        public UnityEngine.UI.Text labelText;
        public Transform lamp;

        public int Current { get; private set; }
        int _student;

        public string Hint
        {
            get { return hints != null && Current < hints.Length ? hints[Current] : null; }
        }

        void OnEnable() { if (session != null) session.ResetDone += OnReset; }
        void OnDisable() { if (session != null) session.ResetDone -= OnReset; }

        void Start()
        {
            _student = session != null ? session.studentNumber : 1;
            Show(0);
        }

        void OnReset()
        {
            if (session.studentNumber == _student) return;   // REINTENTAR: same student, same machine
            _student = session.studentNumber;
            Next();
        }

        public void Next()
        {
            if (machines == null || machines.Length == 0) return;
            Show((Current + 1) % machines.Length);
        }

        void Show(int index)
        {
            if (machines == null || machines.Length == 0) return;
            Current = index;
            for (int i = 0; i < machines.Length; i++)
                if (machines[i] != null) machines[i].SetActive(i == index);
            if (label != null && labelPositions != null && index < labelPositions.Length) label.localPosition = labelPositions[index];
            if (lamp != null && lampPositions != null && index < lampPositions.Length) lamp.localPosition = lampPositions[index];
            if (labelText != null && names != null && index < names.Length) labelText.text = names[index];
            Debug.Log("[Loto] Machine on the table: " + (names != null && index < names.Length ? names[index] : index.ToString()));
        }
    }
}
