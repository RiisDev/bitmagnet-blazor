
using System.Diagnostics;
using System.Text;

namespace Bitmagnet.WebUI.Classes.Services
{
	public class LogService
	{
		private static DateTime _currentLogDate = DateTime.Now.Date;
		private static volatile StreamWriter _logStream = InitializeLogStream();

		private static StreamWriter InitializeLogStream()
		{
			string logFile = $@"{AppDomain.CurrentDomain.BaseDirectory}\logs\log_{DateTime.Now:yyyy-MM-dd}.txt";
			string? logDirectory = Path.GetDirectoryName(logFile);
			if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory!);
			if (!File.Exists(logFile)) File.Create(logFile).Close();

			return new StreamWriter(new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), Encoding.ASCII)
			{
				AutoFlush = true
			};
		}

		private static void CheckForNewLogFile()
		{
			DateTime now = DateTime.Now.Date;
			if (now <= _currentLogDate) return;
			lock (_logStream)
			{
				_logStream.Close();
				_currentLogDate = now;
				_logStream = InitializeLogStream();
			}
		}

		private static void LogToFile(string? message, string type)
		{
			CheckForNewLogFile();
			lock (_logStream) _logStream.WriteLine(ParseMessage(message, type));
		}

		private static string ParseMessage(string? message, string type)
		{
			return $"[{DateTime.Now}] {type} {(string.IsNullOrEmpty(message) ? "Undefined Message" : message)}";
		}

		public static Task LogInformation(string? message)
		{
			LogToFile(message, "[INF]");
			if (OperatingSystem.IsWindows())
				LogToEventLog(message, EventLogEntryType.Information);
			return Task.CompletedTask;
		}

		public static Task LogError(string? message)
		{
			LogToFile(message, "[ERR]");
			if (OperatingSystem.IsWindows())
				LogToEventLog(message, EventLogEntryType.Error);
			return Task.CompletedTask;
		}

		public static Task LogWarning(string? message)
		{
			LogToFile(message, "[WARN]");
			if (OperatingSystem.IsWindows())
				LogToEventLog(message, EventLogEntryType.Warning);
			return Task.CompletedTask;
		}

		public static Task LogDebug(string? message)
		{
			LogToFile(message, "[DEBUG]");
			return Task.CompletedTask;
		}

		private static void LogToEventLog(string? message, EventLogEntryType entryType)
		{
			if (!OperatingSystem.IsWindows()) return;

			try
			{
				using EventLog eventLog = new("Application");
				eventLog.Source = "Windows Server Manager";
				eventLog.WriteEntry(message ?? "Undefined Message", entryType);
			}
			catch {/**/}
		}
	}
}