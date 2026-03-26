namespace World.Scripting.Core {
    public readonly struct YisoScriptToken {
        public YisoScriptTokenType Type { get; }
        public string Value { get; }
        public int Line { get; }

        public YisoScriptToken(YisoScriptTokenType type, string value, int line) {
            Type = type;
            Value = value;
            Line = line;
        }

        public override string ToString() => $"[{Type} '{Value}' L{Line}]";
    }
}