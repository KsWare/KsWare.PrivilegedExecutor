using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KsWare.PrivilegedExecutor.Tests {

	static class Program {

		public static void Main(string[] args) {
			Client.DebugMode = true;

			var test = new ClientTests();
			// test.Setup()
			try {
				test.ExecuteServiceTest();
			}
			catch (Exception ex) {
				System.Console.WriteLine(ex);
				Console.WriteLine(ex);
			}
			finally {
				test.Cleanup();
			}
		}
		
	}
}
