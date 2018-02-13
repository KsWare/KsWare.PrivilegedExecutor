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
			Assert.Inconclusive("Not implmented.");
		}

		[TestMethod]
		public void SplitCommandLineTest() {
			const char RS      = '\u001E';
			var        args    = new[] {"A", "B", "C"};
			var        command = string.Join(new string(RS, 1), args);
			var        result  = Helper.SplitCommandLine(command);
			CollectionAssert.AreEqual(args, result);
		}
	}

}
