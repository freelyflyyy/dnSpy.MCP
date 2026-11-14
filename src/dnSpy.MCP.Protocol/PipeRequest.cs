namespace dnSpy.MCP.Protocol {
    public sealed class PipeRequest {
        public int ProtocolVersion { get; }
        public Guid RequestId { get; }
        public string? Operation { get; }
        public string? Script { get; }
        public Guid? ExpectedInstanceId { get; }

        public PipeRequest(int ProtocolVersion,
            Guid RequestId,
            string? Operation,
            string? Script,
            Guid? ExpectedInstanceId) {

            this.ProtocolVersion = ProtocolVersion;
            this.RequestId = RequestId;
            this.Operation = Operation;
            this.Script = Script;
            this.ExpectedInstanceId = ExpectedInstanceId;
        }
    }
}