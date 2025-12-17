namespace ANTLRTest
{
    /// <summary>
    /// Represents a variable with type, value, and scope.
    /// </summary>
    public class Variable
    {
        public string Type { get; set; }
        public object? Value { get; set; }
        public string Scope { get; set; }

        public Variable(string type, object? value, string scope)
        {
            Type = type;
            Value = value;
            Scope = scope;
        }
    }
}