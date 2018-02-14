namespace KsWare.PrivilegedExecutor {

	public enum ExitCode {
		Success=0,
		MethodNotFound = -1,
		NotElevated = -2,
		ExceptionOccured = -3,
		InvalidParameter = -4
	}

}