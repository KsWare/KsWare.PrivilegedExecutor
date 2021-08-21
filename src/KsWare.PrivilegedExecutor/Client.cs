using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KsWare.IO.NamedPipes;
using KsWare.PrivilegedExecutor.Internal;

namespace KsWare.PrivilegedExecutor {

	internal static class TestClass {
		// KsWare.PrivilegedExecutor.TestClass.TestConsole

		public static int TestMethod() { return 0; }

		public static int TestMethod(string p0) { return 1; }

		public static int TestMethod(string p0, string p1) { return 2; }
	}

	public enum CallMode {
		Process=1,
		Service=2,
	}

	public static class Client {

		public static CallMode Mode { get; set; } = CallMode.Process;

		public static TimeSpan ServiceIdleTime { get; set; } = TimeSpan.FromMinutes(5);

		private static Process _process;

		/// <summary>
		/// Executes the specified methode.
		/// </summary>
		/// <param name="type">The type containing the method.</param>
		/// <param name="methodName">Name of the method.</param>
		/// <param name="args">The arguments.</param>
		/// <returns>System.Int32.</returns>
		public static int Execute(Type type, string methodName, params string[] args) {
			var method = type.Assembly.GetName(false).Name + ";" + type.FullName + "." + methodName;
			return Execute(method, args);
		}

		/// <summary>Executes the specified method.</summary>
		/// <param name="method">The full qualified method name.</param>
		/// <param name="args">The arguments.</param>
		/// <returns>System.Int32.</returns>
		/// <exception cref="InvalidOperationException">Invalid Mode.</exception>
		/// <remarks>Full qualified method name: <c>AssemblyName</c> <c>;</c> <c>TypeFullName</c> <c>.</c> <c>MethodName</c>.<br/>
		/// Example: <c>System.IO;System.IO.File.Delete</c></remarks>
		public static int Execute(string method, params string[] args) {
			switch (Mode) {
				case CallMode.Process: return ExecuteProcess(method, args);
				case CallMode.Service: return ExecuteService(method, args);
				default:               throw new InvalidOperationException("Invalid Mode.");
			}
		}

		public static int ExecuteProcess(string method, params string[] args) {
			var f = Helper.ChangeFileName(Assembly.GetExecutingAssembly().Location, "KsWare.PrivilegedExecutor.exe");
			var a = JoinArguments(method) + " " + JoinArguments(args);
			Debug.WriteLine(f             + " " + a);
			var p             = new Process {
				StartInfo        = new ProcessStartInfo {
					FileName        = f,
					Arguments       = a,
					UseShellExecute = true,
					Verb            = "runas",
					LoadUserProfile = false,
					WindowStyle     = ProcessWindowStyle.Hidden
				}
			};
			p.Start();
			p.WaitForExit();
			return p.ExitCode;
		}

		public static int ExecuteService(string method, params string[] args) {
			const string RS = "\u001E";
			if (_process == null) {
				_process = Process.GetProcessesByName("KsWare.PrivilegedExecutor").FirstOrDefault();
			}
			if (_process == null) {
				var f = Helper.ChangeFileName(Assembly.GetExecutingAssembly().Location, "KsWare.PrivilegedExecutor.exe");
				_process = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = f,
						Arguments = "-service -debug",
						UseShellExecute = true,
						Verb = "runas",
						LoadUserProfile = false,
						WindowStyle = ProcessWindowStyle.Hidden
					}
				};
				_process.Start();
			}
			var a = method + (args.Length > 0 ? RS + string.Join(RS, args) : "");
			return SendCommand(a);
		}

		private static int SendCommand(string command) {
			using (var pipeClient = new NamedPipeClient("KsWare.PrivilegedExecutor")) {
				pipeClient.Connect(500); // TODO catch exception
				var response = pipeClient.SendRequest(command);
				return int.Parse(response);
			}
		}

		private static string JoinArguments(params string[] args) {
			// https://stackoverflow.com/a/6040946/2369575
			var result = new string[args.Length];
			for (int i = 0; i < args.Length; i++) {
				var s = Regex.Replace(args[i], @"(\\*)" + "\"", @"$1$1\" + "\"");
				s = "\"" + Regex.Replace(s, @"(\\+)$", @"$1$1") + "\"";
				result[i] = s;
			}
			return string.Join(" ", result);
		}
	}

}
