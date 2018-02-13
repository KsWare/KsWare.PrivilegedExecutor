using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KsWare.IO.FileSystem;
using KsWare.IO.NamedPipes;

namespace KsWare.PrivilegedExecutor {

	public static class Client {

		private static Process _process;

		public static int ExecuteProcess(string method, params string[] args) {
			var f = Path.ChangeFileName(Assembly.GetExecutingAssembly().Location, "KsWare.IO.FileSystem.PrivilegedExecutor.exe");
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
				var f = Path.ChangeFileName(Assembly.GetExecutingAssembly().Location, "KsWare.PrivilegedExecutor.exe");
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

		// KsWare.IO.FileSystem.PrivilegedExecutor.TestConsole

		public static int TestConsole() { return 0; }

		public static int TestConsole(string p0) { return 1; }

		public static int TestConsole(string p0, string p1) { return 2; }

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
