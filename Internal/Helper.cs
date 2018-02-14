using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
			var                t          = args[0].Split(';');
			var assemblyName = t.Length == 2 ? t[0] : null;
			t = (t.Length == 2 ? t[1] : t[0]).Split('.');
			var                typeName   = String.Join(".", t, 0, t.Length - 1);
			var                methodName = t[t.Length                      - 1];
			var assembly = assemblyName != null ? LoadAssembly(assemblyName) : null;
			if (assemblyName != null && assembly == null) return (int) ExitCode.MethodNotFound;
			var                type       = assembly!=null ? assembly.GetType(typeName, false) : Type.GetType(typeName,false);
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

		internal static Assembly LoadAssembly(string name) {
			var isFullName       = name.Contains("Version=");
			var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			var assembly = isFullName
				? loadedAssemblies.FirstOrDefault(a => a.FullName            == name)
				: loadedAssemblies.FirstOrDefault(a => a.GetName(false).Name == name);
			if(assembly!=null) return assembly;
			name = isFullName ? new AssemblyName(name).Name : name;
			try {
				return Assembly.Load(name);
			}
			catch (IOException ex) {
				Debug.WriteLine(ex.Message);
				return null;
			}
		}

		// copy from KsWare.IO.FileSystem.Path
		public static string ChangeFileName(string path, string fileName) {
			if (path     == null) throw new ArgumentNullException(nameof(path));
			if (fileName == null) throw new ArgumentNullException(nameof(fileName));
			var p     = path.LastIndexOf("\\");
			var path0 = path.Substring(0, p);
			var path1 = path.Substring(p + 1);
			return path0 + "\\" + fileName;
		}

	}
}
