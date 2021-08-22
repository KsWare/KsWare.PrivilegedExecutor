using System;

namespace KsWare.PrivilegedExecutor {

	internal class OperationFailedException : Exception {

		public OperationFailedException():base("Operation failed.") {}

		public OperationFailedException(string message, Exception innerException):base(message,innerException) {}

		public OperationFailedException(string message):base(message) {}
	}

}