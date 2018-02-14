using Microsoft.VisualStudio.TestTools.UnitTesting;
using KsWare.PrivilegedExecutor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KsWare.PrivilegedExecutor.Tests {

	[TestClass]
	public class ClientTests {

		private void A() {
			var t = typeof(KsWare.PrivilegedExecutor.TestClass);
			Debug.WriteLine(t.AssemblyQualifiedName);   // KsWare.PrivilegedExecutor.TestClass, KsWare.PrivilegedExecutor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
			Debug.WriteLine(t.FullName);                // KsWare.PrivilegedExecutor.TestClass
			Debug.WriteLine(t.Name);                    // TestClass

			Debug.WriteLine(t.Assembly.FullName);               // KsWare.PrivilegedExecutor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
			Debug.WriteLine(t.Assembly.GetName(false).Name);	// KsWare.PrivilegedExecutor

			t = Type.GetType("KsWare.PrivilegedExecutor.TestClass, KsWare.PrivilegedExecutor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
			t = Type.GetType("KsWare.PrivilegedExecutor.TestClass");
		}

		[TestMethod]
		public void ExecuteProcessTest() {
			var method = "KsWare.PrivilegedExecutor;KsWare.PrivilegedExecutor.TestClass.TestMethod";

			Assert.AreEqual(0, PrivilegedExecutor.Client.ExecuteProcess(method));
			Assert.AreEqual(1, PrivilegedExecutor.Client.ExecuteProcess(method, "1"));
			Assert.AreEqual(2, PrivilegedExecutor.Client.ExecuteProcess(method, "1", "2"));
		}

		[TestMethod()]
		public void ExecuteServiceTest() {
			var method = "KsWare.PrivilegedExecutor;KsWare.PrivilegedExecutor.TestClass.TestMethod";

			Assert.AreEqual(0, PrivilegedExecutor.Client.ExecuteService(method));
			Assert.AreEqual(1, PrivilegedExecutor.Client.ExecuteService(method, "1"));
			Assert.AreEqual(2, PrivilegedExecutor.Client.ExecuteService(method, "1", "2"));
		}
	}
}