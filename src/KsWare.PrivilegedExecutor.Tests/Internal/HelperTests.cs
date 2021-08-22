using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KsWare.PrivilegedExecutor.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KsWare.PrivilegedExecutor.Tests.Internal {

	[TestClass]
	public class HelperTests {

		[TestMethod]
		public void IsElevatedTest() {
			var b = Helper.IsElevated;
		}

		[TestMethod]
		public void ShiftTest() {
			var args   = new[] {"A", "B", "C"};
			var result = Helper.Shift(args);
			CollectionAssert.AreEqual(new[] {"B", "C"}, result);
		}

		[TestMethod]
		public void ExecuteGenericTest() {
//			Assert.Inconclusive("Not implmented.");

			var method = "KsWare.PrivilegedExecutor;KsWare.PrivilegedExecutor.TestClass.TestMethod";

			Assert.AreEqual(0, Helper.ExecuteGeneric(new[] {method}));
			Assert.AreEqual(1, Helper.ExecuteGeneric(new[] {method, "1"}));
			Assert.AreEqual(2, Helper.ExecuteGeneric(new[] {method, "1", "2"}));

			Assert.AreEqual((int) ExitCode.MethodNotFound, Helper.ExecuteGeneric(new[] {
				"KsWare.PrivilegedExecutor;KsWare.PrivilegedExecutor.TestClass.TestMethodNotExist" }));

			Assert.AreEqual((int) ExitCode.MethodNotFound,
				Helper.ExecuteGeneric(new[] {"KsWare.AssemblyNotExists;KsWare.PrivilegedExecutor.TestClass.TestMethod"}));
		}



		[TestMethod]
		public void SplitCommandLineTest() {
			const char RS      = '\u001E';
			var        args    = new[] {"A", "B", "C"};
			var        command = string.Join(new string(RS, 1), args);
			var        result  = Helper.SplitCommandLine(command);
			CollectionAssert.AreEqual(args, result);
		}
		
		[TestMethod]
		public void LoadAssenblyTest() {
			var loadedAssemblies0 = AppDomain.CurrentDomain.GetAssemblies();

			var assemblies = new Assembly[4];
			assemblies[0]=(Helper.LoadAssembly("KsWare.PrivilegedExecutor")); // referenced in current assembly
			assemblies[1]=(Helper.LoadAssembly("KsWare.IO.NamedPipes"));	  // not referenced in current assembly
//TODO		assemblies[2]=(Helper.LoadAssembly("NuGet"));                     // not referenced
			assemblies[3]=(Helper.LoadAssembly("AssemblyNotExists"));         // not existing => null

			Assert.IsNotNull(assemblies[0]);
			Assert.IsNotNull(assemblies[1]);
//			Assert.IsNotNull(assemblies[2]);
			Assert.IsNull(assemblies[3]);

			// should be 2 more
			var loadedAssemblies1 = AppDomain.CurrentDomain.GetAssemblies();
		}
	}

}
