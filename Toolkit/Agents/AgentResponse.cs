using System.Collections.Generic;

namespace UnityAI.Toolkit.Agents
{
    /// <summary>
    /// Response from an AI agent after processing a message, including any tool calls made.
    /// </summary>
    public class AgentResponse
    {
        /// <summary>Final text output from the LLM after all tool calls are resolved.</summary>
        public string Text { get; set; }

        /// <summary>Log of all tool invocations made during this response.</summary>
        public List<AgentToolCallRecord> ToolCalls { get; set; } = new List<AgentToolCallRecord>();
    }

    /// <summary>
    /// Typed response from an AI agent where the final LLM output is structured JSON
    /// deserialized into <typeparamref name="TResponse"/>.
    /// </summary>
    public class AgentResponse<TResponse>
    {
        /// <summary>Structured response deserialized from the LLM's final JSON output.</summary>
        public TResponse Response { get; set; }

        /// <summary>Log of all tool invocations made during this response.</summary>
        public List<AgentToolCallRecord> ToolCalls { get; set; } = new List<AgentToolCallRecord>();
    }

    /// <summary>
    /// Record of a single tool invocation during an agent response.
    /// </summary>
    public class AgentToolCallRecord
    {
        /// <summary>Name of the tool that was called.</summary>
        public string ToolName { get; set; }

        /// <summary>Arguments passed to the tool.</summary>
        public Dictionary<string, object> Arguments { get; set; }

        /// <summary>Result returned by the tool handler.</summary>
        public string Result { get; set; }
    }
}
