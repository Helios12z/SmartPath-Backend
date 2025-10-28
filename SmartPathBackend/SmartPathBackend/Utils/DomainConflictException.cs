namespace SmartPathBackend.Utils
{
    public sealed class DomainConflictException: Exception
    {
        public string Code { get; }
        public string? Field { get; }
        public DomainConflictException(string code, string message, string? field = null)
            : base(message) { Code = code; Field = field; }
    }
}
