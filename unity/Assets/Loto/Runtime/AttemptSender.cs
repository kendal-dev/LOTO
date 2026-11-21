using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Loto.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Loto
{
    /// <summary>
    /// Sends one attempt to the teacher's server when it completes or fails:
    /// POST {baseUrl}/api/attempts with the JSON of docs/CONTRATO.md. Fails silently (the simulation
    /// never waits for the network). If the reply brings "explanation", the panel shows it.
    /// The address can be changed without a new build: a text file "server.txt" with the address in
    /// the app's data folder (Application.persistentDataPath) wins over the field.
    /// </summary>
    public class AttemptSender : MonoBehaviour
    {
        public LotoSession session;
        public StatusPanel panel;
        [Tooltip("Server address without the final slash. From the headset use the laptop's IP, not localhost.")]
        public string baseUrl = "http://localhost:3000";
        public int timeoutSeconds = 5;

#pragma warning disable 0649   // filled by JsonUtility through reflection
        [Serializable]
        class Reply
        {
            public string id;
            public string explanation;
        }
#pragma warning restore 0649

        void Awake()
        {
            try
            {
                string file = Path.Combine(Application.persistentDataPath, "server.txt");
                if (File.Exists(file))
                {
                    string fromFile = File.ReadAllText(file).Trim();
                    if (fromFile.Length > 0) baseUrl = fromFile;
                }
            }
            catch (Exception e)
            {
                Debug.Log("[Loto] server.txt not read: " + e.Message);
            }
            baseUrl = baseUrl.TrimEnd('/');
            Debug.Log("[Loto] Server: " + baseUrl);
        }

        void OnEnable() { if (session != null) session.AttemptEnded += Send; }
        void OnDisable() { if (session != null) session.AttemptEnded -= Send; }

        int _sendSerial;   // counts the attempts sent; a reply only counts if it belongs to the last one

        void Send()
        {
            if (string.IsNullOrEmpty(baseUrl)) return;
            Referee referee = session.Referee;
            string json = BuildJson(session.SessionId, session.StudentName, referee.Completed, referee.FailCode,
                referee.CurrentStep, session.Seconds, referee.History, DateTime.UtcNow, MachineName());
            _sendSerial++;
            StartCoroutine(Post(json, _sendSerial));
        }

        IEnumerator Post(string json, int serial)
        {
            using (var request = new UnityWebRequest(baseUrl + "/api/attempts", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("[Loto] Attempt not sent (" + request.error + "): the simulation goes on without the server.");
                    yield break;
                }

                Debug.Log("[Loto] Attempt sent, HTTP " + request.responseCode);
                Reply reply = null;
                try { reply = JsonUtility.FromJson<Reply>(request.downloadHandler.text); }
                catch (Exception e) { Debug.Log("[Loto] Reply not understood: " + e.Message); }
                // A slow reply (a model writing the explanation) can arrive after the student retried, or
                // retried and failed again: then it belongs to an older attempt and is not shown (open issue C).
                if (serial != _sendSerial || !session.Ended)
                {
                    Debug.Log("[Loto] Late explanation ignored: it belongs to an earlier attempt.");
                    yield break;
                }
                if (reply != null && panel != null && !string.IsNullOrEmpty(reply.explanation))
                    panel.ShowServerExplanation(reply.explanation);
            }
        }

        /// <summary>The contract's JSON, written by hand so errorCode can be a real null.</summary>
        /// <summary>The machine on the table (MachineSelector), or null in a scene with one machine.</summary>
        static string MachineName()
        {
            MachineSelector selector = UnityEngine.Object.FindAnyObjectByType<MachineSelector>();
            if (selector == null || selector.names == null || selector.Current >= selector.names.Length) return null;
            return selector.names[selector.Current];
        }

        /// <summary>machine: optional ninth field (CONTRATO §1), left out when null.</summary>
        public static string BuildJson(string sessionId, string student, bool ok, string errorCode, int step,
            float seconds, IReadOnlyList<LotoAction> actions, DateTime utcNow, string machine = null)
        {
            var json = new StringBuilder(256);
            json.Append("{\"session\":").Append(Quote(sessionId));
            json.Append(",\"student\":").Append(Quote(student));
            json.Append(",\"ok\":").Append(ok ? "true" : "false");
            json.Append(",\"errorCode\":").Append(errorCode == null ? "null" : Quote(errorCode));
            json.Append(",\"step\":").Append(step.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"seconds\":").Append(seconds.ToString("0.0", CultureInfo.InvariantCulture));
            json.Append(",\"actions\":[");
            for (int i = 0; i < actions.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append(Quote(actions[i].ToString()));
            }
            json.Append("],\"clientTime\":").Append(Quote(utcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)));
            if (machine != null) json.Append(",\"machine\":").Append(Quote(machine));
            json.Append('}');
            return json.ToString();
        }

        static string Quote(string value)
        {
            if (value == null) return "null";
            var text = new StringBuilder(value.Length + 2);
            text.Append('"');
            foreach (char c in value)
            {
                if (c == '"') text.Append("\\\"");
                else if (c == '\\') text.Append("\\\\");
                else if (c == '\n') text.Append("\\n");
                else if (c == '\r') text.Append("\\r");
                else if (c == '\t') text.Append("\\t");
                else if (c < ' ') text.Append("\\u").Append(((int)c).ToString("x4"));
                else text.Append(c);
            }
            text.Append('"');
            return text.ToString();
        }
    }
}
