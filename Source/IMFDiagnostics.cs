using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// Writes IMF.log next to the mod's dll, for bug reports. Errors and info lines are always written, Verbose only in debug builds.
    /// The file is rotated to IMF.log.old past 5 MB. Writing never throws, a log problem shouldn't break the game.
    /// </summary>
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

        /// <summary>
        /// Moves the log to IMF.log.old when it's too big. Keeps one old log, a locked file isn't worth breaking the game over.
        /// </summary>
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

        /// <summary>
        /// Writes an exception with its stack trace.
        /// </summary>
        /// <param name="label">what was being done when it failed</param>
        internal static void Log(string label, Exception e)
        {
            if (e == null)
                return;
            Write($"[{DateTime.Now:HH:mm:ss.fff}] {label}{Environment.NewLine}{e}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}");
        }

        // per-frame code would fill the log, so only the first error of each kind is written
        private static readonly HashSet<string> LoggedOnce = new HashSet<string>();

        /// <summary>
        /// Writes an exception only the first time this label is seen. Per-frame code would fill the log otherwise.
        /// </summary>
        internal static void LogOnce(string label, Exception e)
        {
            lock (Gate)
            {
                if (!LoggedOnce.Add(label))
                    return;
            }
            Log(label, e);
        }

        /// <summary>
        /// Lets LogOnce labels be written again, called when a new game starts.
        /// </summary>
        internal static void ResetLogOnce()
        {
            lock (Gate)
                LoggedOnce.Clear();
        }

        /// <summary>
        /// Writes an info line.
        /// </summary>
        internal static void Info(string message)
        {
            Write($"[{DateTime.Now:HH:mm:ss.fff}] [INFO] {message}{Environment.NewLine}");
        }

        /// <summary>
        /// Writes a debug line. Calls to this are compiled out of release builds.
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void Verbose(string message)
        {
            Write($"[{DateTime.Now:HH:mm:ss.fff}] [DEBUG] {message}{Environment.NewLine}");
        }
    }
}
