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

		private static bool ServiceMode;
		private static bool DebugMode;

		private static Mutex Mutex { get; set; }

		public static int Main(params string[] args) {
			if (args.Length == 0) {
				Console.WriteLine("Privileged Executor for KsWare.IO.FileSystem v1.0");
				Console.WriteLine("Copyright (c) 2018 by KsWare. All rights reserved.");
				Console.WriteLine();
				Console.WriteLine("This programm is not indended to be used by a user.");
				Console.Write("Press any key for exit...");
				Console.ReadKey(true);
				return 0;
			}

			Console.WriteLine($"IsElevated: {Helper.IsElevated}");
			Console.WriteLine($"UserName: {Environment.UserName}");
			Console.WriteLine($"UserInteractive: {Environment.UserInteractive}");
			Console.WriteLine(args[0]);

			if (!Helper.IsElevated) Environment.Exit((int) ExitCode.NotElevated);

			while (args.Length > 0 && args[0].StartsWith("-")) {
				switch (args[0].ToLowerInvariant()) {
					case "-s": case "-service": ServiceMode = true; break;
					case "-d": case "-debug": DebugMode = true; break;
					default:
						//Debug.WriteLine($"Warning: Unknown parameter ignored. '{args[0]}'"); break;
						Environment.Exit((int) ExitCode.InvalidParameter); break;
				}
				args = Helper.Shift(args);
			}
			if (ServiceMode) {
				RunAsService();
				return 0;
			}

			if (args.Length == 0) Environment.Exit((int) ExitCode.InvalidParameter);

			switch (args[0]) {
//				case "KsWare.IO.FileSystem.VolumeMountPoint.SetVolumeMountPointConsole": Environment.ExitCode=KsWare.IO.FileSystem.VolumeMountPoint.SetVolumeMountPointConsole(args[1],args[2]); break;
				default:
					Environment.ExitCode = ExecuteGeneric(args);
					break;
			}
			if (Enum.IsDefined(typeof(ExitCode), Environment.ExitCode))
				Console.WriteLine($"ExitReason: {(ExitCode) Environment.ExitCode}");
			Console.WriteLine($"ExitCode: {Environment.ExitCode}");
			return Environment.ExitCode;
//			Thread.Sleep(5000);
		}

		private static void RunAsService() {
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
			NamedPipeServerStreams                    pipeServer         = null;
			var                                exit               = false;
			var lastTransmission = DateTime.MinValue;
			Timer timer=null;

			try {
				pipeServer = new NamedPipeServerStreams("KsWare.PrivilegedExecutor");
				StreamReader sr = pipeServer.Reader;
				StreamWriter sw = pipeServer.Writer;

				consoleExitHandler = (sender, e) => {
					Console.WriteLine($"[SERVER] Close because {e.ExitReason}. Shutting down...");
					exit              = true;
					pipeServer?.Close();
				};
				Console.Exit += consoleExitHandler;

				lastTransmission = DateTime.Now;
				timer=new Timer(state => {
					Console.WriteLine("[SERVER] Close because idle since 5 minutes. Shutting down...");
					exit = true;
					pipeServer?.Close();
				},null,TimeSpan.FromMinutes(5),TimeSpan.FromMilliseconds(-1));

				while (!exit) {
					try {
						pipeServer.WaitForConnection();
						string command = sr.ReadLine();
						if(command==null) continue;
						Console.WriteLine($"[SERVER] Received: {command}");
						lastTransmission=DateTime.Now;
						timer.Change(TimeSpan.FromMinutes(5), TimeSpan.FromMilliseconds(-1));
						if (command == "-close") {
							Console.WriteLine("[SERVER] Close requested by client. Shutting down...");
							break;
						}
						var args     = SplitCommandLine(command);
						var exitcode = ExecuteGeneric(args);
						sw.WriteLine(exitcode);
						sw.Flush();
					}
					catch (IOException ex) { // Catch the IOException that is raised if the pipe is broken or disconnected.
						Console.WriteLine($"[SERVER] Error: {ex.Message}");
					}
					catch (Exception ex) {
						Console.WriteLine(@"[SERVER] ERROR {ex.Message}");
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

			}
			finally {
				timer?.Dispose();
				if (consoleExitHandler != null) Console.Exit -= consoleExitHandler;
				pipeServer?.Dispose();
			}

		}

	}

}
