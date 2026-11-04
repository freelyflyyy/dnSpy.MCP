namespace dnSpy.MCP.Extension {
    /// <summary>
    /// Represents the state of the MCP service.
    /// Common workflow:
    /// Stopped → Starting → Running → Stopping → Stopped
    /// Error → Faulted
    /// </summary>
    public enum ServiceState {
        Stopped,
        Starting,
        Running,
        Stopping,
        Faulted
    }
}
