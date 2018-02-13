using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace KsWare.PrivilegedExecutor.Internal {

	public class Helper {

		internal static bool IsElevated {
			get {
				bool isElevated;
				using (var identity = WindowsIdentity.GetCurrent()) {
					var principal = new WindowsPrincipal(identity);
					isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
				}
				return isElevated;
			}
		}

		internal static string[] Shift(string[] args) {
			if (args.Length == 0) throw new ArgumentException("No arguments to shift.");
			return args.Skip(1).ToArray();
		}

		internal static int ExecuteGeneric(string[] args) {
			const BindingFlags flags      = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			var                t          = args[0].Split('.');
			var                typeName   = String.Join(".", t, 0, t.Length - 1);
			var                methodName = t[t.Length                      - 1];
			var                type       = typeof(IO.FileSystem.AssemblyInfo).Assembly.GetType(typeName, false);
			if (type == null) return (int) ExitCode.MethodNotFound;
			var method = type.GetMethods(flags).FirstOrDefault(m =>
				m.Name == methodName && m.ReturnType == typeof(int) && m.GetParameters().Length == args.Length - 1);
			if (method == null) return (int) ExitCode.MethodNotFound;
			Console.WriteLine($"IsElevated: {Helper.IsElevated}");
			Console.WriteLine($"Method: {args[0]}");
			try {
				var ret = (int) method.Invoke(null, flags, null, args.Skip(1).Cast<object>().ToArray(), CultureInfo.CurrentCulture);
				Console.WriteLine($"Result: {ret}");
				return ret;
			}
			catch (Exception ex) {
				Console.WriteLine($"Exception: {ex}");
				Console.WriteLine($"Exception.Message: {ex.Message}");
				return (int) ExitCode.ExceptionOccured;
			}
		}

		internal static string[] SplitCommandLine(string command) {
			const char RS   = '\u001E';
			var        args = command.Split(RS);
			return args;
		}

	}
}
