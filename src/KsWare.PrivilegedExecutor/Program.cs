using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using KsWare.IO.NamedPipes.Internal;
using KsWare.PrivilegedExecutor.Internal;
using Console = KsWare.Console;

using static KsWare.PrivilegedExecutor.Internal.Helper;

namespace KsWare.PrivilegedExecutor {

	public static class Program {

		private static bool BackgroundMode;
		private static bool DebugMode;
		private static bool SingleInstance = false;

		private static Mutex Mutex { get; set; }

		public static int Main(params string[] args) {
			try {
				if (args.Length == 0) {
					Console.WriteLine("Privileged Executor");
					Console.WriteLine("Copyright (c) 2018-2021 by KsWare. All rights reserved.");
					Console.WriteLine();
					Console.WriteLine($"IsElevated: {Helper.IsElevated}");
					Console.WriteLine($"UserName: {Environment.UserName}");
					Console.WriteLine($"UserInteractive: {Environment.UserInteractive}");
					Console.WriteLine();
					Console.WriteLine("This program is not intended to be executed by a regular user.");
					Console.Write("Press any key for exit...");
					Console.ReadKey(true);
					return (int)ExitCode.Success;
				}
				Console.WriteLine($"StartParameter: {string.Join(" ",args)}");
				Console.WriteLine($"IsElevated: {Helper.IsElevated}");
				Console.WriteLine($"UserName: {Environment.UserName}");
				Console.WriteLine($"UserInteractive: {Environment.UserInteractive}");

				while (args.Length > 0 && args[0].StartsWith("-")) {
					switch (args[0].ToLowerInvariant()) {
						case "-b": case "--background": BackgroundMode = true; break;
						case "-d": case "--debug": DebugMode = true; break;
						case "-s": case "--singleinstance": SingleInstance = true; break;
						default:
							Console.WriteLine($"ERR: Unknown start parameter '{args[0]}'");
							return (int)ExitCode.InvalidParameter;
					}
					args = Helper.Shift(args);
				}

				if (!Helper.IsElevated) return (int)ExitCode.NotElevated;

				if (BackgroundMode) {
					RunInBackground();
					if (DebugMode) {
						Console.Write("[SERVER DEBUG MODE] Press any key to exit...");
						Console.ReadKey(true); //keep open at exit
					}
					return (int)ExitCode.Success;
				}

				if (args.Length == 0) Environment.Exit((int)ExitCode.InvalidParameter);

				switch (args[0]) {
//					case "KsWare.IO.FileSystem.VolumeMountPoint.SetVolumeMountPointConsole": Environment.ExitCode=KsWare.IO.FileSystem.VolumeMountPoint.SetVolumeMountPointConsole(args[1],args[2]); break;
					default:
						return ExecuteGeneric(args);
				}
			}
			finally {
				if(DebugMode) Console.WriteLine(Enum.IsDefined(typeof(ExitCode), Environment.ExitCode)
					? $"ExitReason: {(ExitCode)Environment.ExitCode}"
					: $"ExitCode: {Environment.ExitCode}");
			}
//			Thread.Sleep(5000);
		}

		private static void RunInBackground() {
			Console.ShowWindow(DebugMode);
//			Mutex = new Mutex(true, @"Global\KsWare.PrivilegedExecutor");
//			var thread=new Thread(MessageLoop){IsBackground = true,Name = "ServiceWorker"};
//			thread.Start();
//			thread.Join();
			MessageLoop();
		}

		private static void MessageLoop() {
			// TODO use NamedPipeServer
			EventHandler<ConsoleExitEventArgs> consoleExitHandler = null;
			NamedPipeServerStreams pipeServer = null;
			var exit = false;
			var lastTransmission = DateTime.MinValue;
			Timer timer = null;

			try {
				var pipeName = "KsWare.PrivilegedExecutor" + (SingleInstance ? "" : Process.GetCurrentProcess().Id.ToString());
				pipeServer = new NamedPipeServerStreams(pipeName);
				Console.WriteLine($"Named pipe created '{pipeName}*'");
				var reader = pipeServer.Reader;
				var writer = pipeServer.Writer;

				consoleExitHandler = (sender, e) => {
					Console.WriteLine($"[SERVER] Close because {e.ExitReason}. Shutting down...");
					exit = true;
					pipeServer?.Close();
				};
				Console.Exit += consoleExitHandler;

				lastTransmission = DateTime.Now;
				timer = new Timer(state => {
					Console.WriteLine("Close because idle since 5 minutes. Shutting down...");
					exit = true;
					pipeServer?.Close();
				}, null, TimeSpan.FromMinutes(5), TimeSpan.FromMilliseconds(-1));

				while (!exit) {
					try {
						Console.WriteLine("WaitForConnection...");
						pipeServer.WaitForConnection();
						var command = reader.ReadLine();
						if (command == null) continue;
						Console.WriteLine($"Command received: {command}");
						lastTransmission = DateTime.Now;
						timer.Change(TimeSpan.FromMinutes(5), TimeSpan.FromMilliseconds(-1));
						if (command == "-close") {
							Console.WriteLine("Server close requested by client. Shutting down...");
							break;
						}

						var args = SplitCommandLine(command);
						var exitCode = ExecuteGeneric(args);
						writer.WriteLine(exitCode);
						writer.Flush();
					}
					catch (IOException ex) {
						// Catch the IOException that is raised if the pipe is broken or disconnected.
						Console.WriteLine($"ERROR: {ex.Message}");
					}
					catch (Exception ex) {
						Console.WriteLine($"ERROR: {ex.Message}");
					}
					finally {
						pipeServer.WaitForPipeDrain();
						if (pipeServer.IsConnected) {
							pipeServer.Disconnect();
						}
					}
				}
			}
			catch (Exception ex) {
				Console.WriteLine($@"ERROR: {ex.Message}");
			}
			finally {
				timer?.Dispose();
				if (consoleExitHandler != null) Console.Exit -= consoleExitHandler;
				pipeServer?.Dispose();
			}

		}

		
	}

}
