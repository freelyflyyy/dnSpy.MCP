namespace dnSpy.MCP.Protocol {
    public sealed class PipeResponse<T> where T : class {
        public int ProtocolVersion { get; }
        public Guid RequestId { get; }
        public T? Result { get; }
        public string? Error { get; }

        public PipeResponse(int ProtocolVersion,
            Guid RequestId,
            T? Result,
            string? Error) {
            this.ProtocolVersion = ProtocolVersion;
            this.RequestId = RequestId;
            this.Result = Result;
            this.Error = Error;
        }
    }
}
