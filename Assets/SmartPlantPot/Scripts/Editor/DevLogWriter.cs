using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace SmartPlantPot.EditorTools
{
    /// <summary>
    /// Development helper: mirrors script compile messages and Console errors/warnings into plain
    /// text files under Logs/ (SmartPlantPot_compile.log, SmartPlantPot_console.log), so they can be
    /// read or shared without screenshots of the Console window. Editor only, never in builds.
    /// </summary>
    [InitializeOnLoad]
    static class DevLogWriter
    {
        const string CompileLog = "Logs/SmartPlantPot_compile.log";
        const string ConsoleLog = "Logs/SmartPlantPot_console.log";
        static readonly object Gate = new object();

        static DevLogWriter()
        {
            CompilationPipeline.compilationStarted -= OnCompileStarted;
            CompilationPipeline.compilationStarted += OnCompileStarted;
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyFinished;
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        static void OnCompileStarted(object context)
        {
            Write(CompileLog, "== compile started " + DateTime.Now.ToString("HH:mm:ss") + "\n", false);
        }

        static void OnAssemblyFinished(string assembly, CompilerMessage[] messages)
        {
            var sb = new StringBuilder();
            int errors = 0;
            foreach (var m in messages)
            {
                if (m.type == CompilerMessageType.Error) errors++;
                if (m.type == CompilerMessageType.Info) continue;
                sb.Append(m.type).Append(": ").Append(m.message).Append('\n');
            }
            sb.Insert(0, "-- " + Path.GetFileName(assembly) + (errors > 0 ? "  FAILED (" + errors + " errors)" : "  ok") + "\n");
            Write(CompileLog, sb.ToString(), true);
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                Write(ConsoleLog, "== play mode " + DateTime.Now.ToString("HH:mm:ss") + "\n", false);
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + type + ": " + condition + "\n";
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                string[] frames = (stackTrace ?? "").Split('\n');
                for (int i = 0; i < frames.Length && i < 8; i++)
                    if (frames[i].Trim().Length > 0) line += "    " + frames[i].Trim() + "\n";
            }
            Write(ConsoleLog, line, true);
        }

        static void Write(string path, string text, bool append)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    if (append) File.AppendAllText(path, text, Encoding.UTF8);
                    else File.WriteAllText(path, text, Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // never let logging break the editor
            }
        }
    }
}
