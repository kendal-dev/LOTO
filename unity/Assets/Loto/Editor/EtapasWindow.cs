using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Loto.EditorTools
{
    /// <summary>
    /// Loto > Etapas: keeps a copy of each APK as a named stage of the hackathon (Builds/etapas/, never committed:
    /// Builds/ is in .gitignore) and installs any of them on the Quest with one button, to show how the work moved.
    /// Editor only: it does not change the app or the scene.
    /// </summary>
    public class EtapasWindow : EditorWindow
    {
        const string Folder = "Builds/etapas";

        string _name = "etapa";
        Vector2 _scroll;
        Process _adb;
        string _adbStage;
        string _status = "";

        [MenuItem("Loto/Etapas...")]
        static void Open()
        {
            var window = GetWindow<EtapasWindow>("Etapas LOTO");
            window.minSize = new Vector2(460f, 320f);
        }

        void OnDisable()
        {
            EditorApplication.update -= PollAdb;
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Etapas de la hackathon", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Cada etapa es una copia del APK con nombre y hora. «Instalar» la pone en el Quest " +
                "conectado por USB (adb install -r). No cambia la escena ni el código.", MessageType.None);

            _name = EditorGUILayout.TextField("Nombre de la etapa", _name);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!File.Exists(BuildApk.OutputPath)))
                {
                    if (GUILayout.Button("Guardar el APK actual")) Save(_name);
                }
                if (GUILayout.Button("Build APK y guardar"))
                {
                    DateTime before = File.Exists(BuildApk.OutputPath) ? File.GetLastWriteTime(BuildApk.OutputPath) : DateTime.MinValue;
                    BuildApk.Run();
                    if (File.Exists(BuildApk.OutputPath) && File.GetLastWriteTime(BuildApk.OutputPath) > before) Save(_name);
                    else _status = "El build no dejó un APK nuevo: mirar la consola.";
                }
            }
            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Guardadas", EditorStyles.boldLabel);
            string[] files = Directory.Exists(Folder)
                ? Directory.GetFiles(Folder, "*.apk").OrderBy(f => Path.GetFileName(f)).ToArray()
                : new string[0];
            if (files.Length == 0) EditorGUILayout.LabelField("Todavía no hay etapas.");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (string file in files)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    string note = Path.ChangeExtension(file, ".txt");
                    string info = File.Exists(note) ? File.ReadAllText(note).Trim() : "";
                    EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(file) + "\n" + info, EditorStyles.wordWrappedLabel,
                        GUILayout.MinHeight(34f));
                    using (new EditorGUI.DisabledScope(_adb != null))
                    {
                        if (GUILayout.Button(_adb != null && _adbStage == file ? "Instalando..." : "Instalar", GUILayout.Width(100f))) Install(file);
                    }
                    if (GUILayout.Button("Carpeta", GUILayout.Width(70f))) EditorUtility.RevealInFinder(file);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void Save(string name)
        {
            Directory.CreateDirectory(Folder);
            string safe = new string((name ?? "etapa").Trim().Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '-').ToArray());
            if (safe.Length == 0) safe = "etapa";
            int number = Directory.GetFiles(Folder, "*.apk").Length + 1;
            string target = Path.Combine(Folder, number.ToString("00") + "-" + safe + "-" + DateTime.Now.ToString("ddMM-HHmm") + ".apk");
            File.Copy(BuildApk.OutputPath, target, true);
            string commit = Run("git", "rev-parse --short HEAD");
            File.WriteAllText(Path.ChangeExtension(target, ".txt"),
                DateTime.Now.ToString("dd/MM HH:mm") + " · " + (new FileInfo(target).Length / 1048576f).ToString("F0") + " MB" +
                (string.IsNullOrEmpty(commit) ? "" : " · commit " + commit));
            _status = "Guardada: " + Path.GetFileName(target);
            Debug.Log("[Loto] Stage saved: " + Path.GetFullPath(target));
        }

        void Install(string file)
        {
            string adb = FindAdb();
            if (adb == null) { _status = "No encontré adb. A mano: adb install -r \"" + Path.GetFullPath(file) + "\""; return; }
            try
            {
                _adb = Process.Start(new ProcessStartInfo(adb, "install -r \"" + Path.GetFullPath(file) + "\"")
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                });
                _adbStage = file;
                _status = "Instalando " + Path.GetFileName(file) + " en el Quest...";
                EditorApplication.update -= PollAdb;
                EditorApplication.update += PollAdb;
            }
            catch (Exception e)
            {
                _adb = null;
                _status = "No pude correr adb: " + e.Message;
            }
        }

        // Polled from the editor loop so the install does not freeze Unity.
        void PollAdb()
        {
            if (_adb == null) { EditorApplication.update -= PollAdb; return; }
            if (!_adb.HasExited) return;
            string output = (_adb.StandardOutput.ReadToEnd() + " " + _adb.StandardError.ReadToEnd()).Trim();
            bool ok = _adb.ExitCode == 0 && output.Contains("Success");
            _status = (ok ? "Instalada: " : "Falló la instalación: ") + Path.GetFileName(_adbStage) + (ok ? "" : "\n" + output);
            Debug.Log("[Loto] adb install " + Path.GetFileName(_adbStage) + ": " + output);
            _adb.Dispose();
            _adb = null;
            EditorApplication.update -= PollAdb;
            Repaint();
        }

        // Same search as Tools/build.ps1: adb on the PATH, then the Android SDK that comes with this Unity.
        static string FindAdb()
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string dir in path.Split(Path.PathSeparator))
            {
                try
                {
                    string candidate = Path.Combine(dir.Trim(), "adb.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch (ArgumentException) { }
            }
            string bundled = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe");
            return File.Exists(bundled) ? bundled : null;
        }

        static string Run(string exe, string args)
        {
            try
            {
                using (var p = Process.Start(new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, WorkingDirectory = Directory.GetCurrentDirectory()
                }))
                {
                    string output = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(5000);
                    return output;
                }
            }
            catch (Exception) { return ""; }
        }
    }
}
