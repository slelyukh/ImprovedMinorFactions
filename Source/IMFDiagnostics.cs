using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ImprovedMinorFactions.Patches
{
    internal static class IMFDiagnostics
    {
        private const long MaxLogBytes = 5L * 1024 * 1024;

        private static readonly object Gate = new object();
        private static StreamWriter? _writer;

        private static readonly string LogPath =
            Path.Combine(Path.GetDirectoryName(typeof(IMFDiagnostics).Assembly.Location) ?? ".", "IMF.log");

        private static void Write(string text)
        {
            lock (Gate)
            {
                try
                {
                    if (_writer == null)
                    {
                        RotateIfTooBig();
                        var stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                    }
                    _writer.Write(text);
                    if (_writer.BaseStream.Length > MaxLogBytes)
                        CloseWriter();   // the next write rotates the file
                }
                catch
                {
                    CloseWriter();
                }
            }
        }

        // keeps one old log, a locked file isn't worth breaking the game over
        private static void RotateIfTooBig()
        {
            try
            {
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxLogBytes)
                {
                    File.Copy(LogPath, LogPath + ".old", true);
                    File.WriteAllText(LogPath, "");
                }
            }
            catch
            {
                // locked by another process, try again next session
            }
        }

        private static void CloseWriter()
        {
            try
            {
                _writer?.Dispose();
            }
            catch
            {
                // nothing to do if the log won't close
            }
            _writer = null;
        }

        internal static void Log(string label, Exception e)
        {
            if (e == null)
                return;
            Write($"[{DateTime.Now:HH:mm:ss.fff}] {label}{Environment.NewLine}{e}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}");
        }

        // per-frame code would fill the log, so only the first error of each kind is written
        private static readonly HashSet<string> LoggedOnce = new HashSet<string>();

        internal static void LogOnce(string label, Exception e)
        {
            lock (Gate)
            {
                if (!LoggedOnce.Add(label))
                    return;
            }
            Log(label, e);
        }

        internal static void ResetLogOnce()
        {
            lock (Gate)
                LoggedOnce.Clear();
        }

        internal static void Info(string message)
        {
            Write($"[{DateTime.Now:HH:mm:ss.fff}] [INFO] {message}{Environment.NewLine}");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        internal static void Verbose(string message)
        {
            Write($"[{DateTime.Now:HH:mm:ss.fff}] [DEBUG] {message}{Environment.NewLine}");
        }
    }
}
