using System;
using System.IO;
using UnityEngine;

namespace Loto
{
    /// <summary>
    /// The headset keeps about one minute of logcat, so the app writes its own log:
    /// [persistentDataPath]/loto.log (on the Quest: /sdcard/Android/data/com.equipo.lotoxr/files/loto.log).
    /// Starts by itself; nothing to add to the scene.
    /// </summary>
    public static class LotoLog
    {
        static StreamWriter _writer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Begin()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "loto.log");
                _writer = new StreamWriter(path, false);
                _writer.AutoFlush = true;
                _writer.WriteLine("LOTO XR log, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ", Unity " + Application.unityVersion);
                Application.logMessageReceived -= Write;
                Application.logMessageReceived += Write;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Loto] Log file not opened: " + e.Message);
            }
        }

        static void Write(string message, string stackTrace, LogType type)
        {
            if (_writer == null) return;
            try
            {
                _writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + type + " " + message);
                if (type == LogType.Exception || type == LogType.Error) _writer.WriteLine(stackTrace);
            }
            catch (Exception)
            {
                // Never let logging break the app.
            }
        }
    }
}
