// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//

namespace SinclairCC.MakeMeAdmin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Simple logger for client-side logging to assist with troubleshooting.
    /// </summary>
    public class ClientLogger
    {
        private static readonly object lockObject = new object();
        private static List<LogEntry> logEntries = new List<LogEntry>();
        private static readonly int MaxEntries = 1000; // Keep last 1000 entries in memory

        /// <summary>
        /// Log entry structure.
        /// </summary>
        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Category { get; set; }
            public string Message { get; set; }
            public string Details { get; set; }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] [{2}] {3}",
                    Timestamp, Level, Category, Message);
                if (!string.IsNullOrEmpty(Details))
                {
                    sb.AppendLine();
                    sb.Append("  Details: ").Append(Details);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Log levels.
        /// </summary>
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        public static void Log(LogLevel level, string category, string message, string details = null)
        {
            lock (lockObject)
            {
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    Category = category,
                    Message = message,
                    Details = details
                };

                logEntries.Add(entry);

                // Keep only the last MaxEntries entries
                if (logEntries.Count > MaxEntries)
                {
                    logEntries.RemoveAt(0);
                }

                // Also write to file if enabled
                WriteToFile(entry);
            }
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        public static void Debug(string category, string message, string details = null)
        {
            Log(LogLevel.Debug, category, message, details);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        public static void Info(string category, string message, string details = null)
        {
            Log(LogLevel.Info, category, message, details);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void Warning(string category, string message, string details = null)
        {
            Log(LogLevel.Warning, category, message, details);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void Error(string category, string message, string details = null)
        {
            Log(LogLevel.Error, category, message, details);
        }

        /// <summary>
        /// Gets all log entries.
        /// </summary>
        public static List<LogEntry> GetEntries()
        {
            lock (lockObject)
            {
                return new List<LogEntry>(logEntries);
            }
        }

        /// <summary>
        /// Gets log entries filtered by level.
        /// </summary>
        public static List<LogEntry> GetEntries(LogLevel minLevel)
        {
            lock (lockObject)
            {
                return logEntries.Where(e => e.Level >= minLevel).ToList();
            }
        }

        /// <summary>
        /// Clears all log entries.
        /// </summary>
        public static void Clear()
        {
            lock (lockObject)
            {
                logEntries.Clear();
            }
        }

        /// <summary>
        /// Exports logs to a file.
        /// </summary>
        public static void ExportToFile(string filePath)
        {
            lock (lockObject)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                    {
                        writer.WriteLine("Make Me Admin Client Log");
                        writer.WriteLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        writer.WriteLine(new string('=', 80));
                        writer.WriteLine();

                        foreach (var entry in logEntries)
                        {
                            writer.WriteLine(entry.ToString());
                            writer.WriteLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Silently fail if we can't write to file
                    System.Diagnostics.Debug.WriteLine("Failed to export logs: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Writes a log entry to file.
        /// </summary>
        private static void WriteToFile(LogEntry entry)
        {
            try
            {
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Make Me Admin",
                    "Logs");

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string logFile = Path.Combine(logDirectory,
                    string.Format("ClientLog_{0:yyyyMMdd}.txt", DateTime.Now));

                using (StreamWriter writer = new StreamWriter(logFile, true, Encoding.UTF8))
                {
                    writer.WriteLine(entry.ToString());
                }
            }
            catch
            {
                // Silently fail if we can't write to file
            }
        }
    }
}
