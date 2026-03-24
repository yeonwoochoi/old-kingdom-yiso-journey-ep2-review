using System;

namespace World.Scripting.Core {
    public class YisoScriptException : Exception {
        public int Line { get; }

        public YisoScriptException(string message, int line = 0)
            : base(line > 0 ? $"[{line}번째 줄] {message}" : message) {
            Line = line;
        }
    }
}
