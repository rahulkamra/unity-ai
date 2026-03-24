using System.Collections.Generic;

namespace UnityAI.Toolkit.Agents
{
    /// <summary>
    /// Status of the agent's execution.
    /// </summary>
    public enum AgentStatus
    {
        Success,
        MaxIterationsReached,
        Error
    }

    /// <summary>
    /// Typed response from a ReactAgent where the final LLM output is
    /// deserialized into <typeparamref name="TResponse"/>.
    /// For plain text responses, use <c>AgentResponse&lt;string&gt;</c>.
    /// </summary>
    public class AgentResponse<TResponse>
    {
        /// <summary>Structured response deserialized from the LLM's final output.</summary>
        public TResponse Response { get; set; }

        /// <summary>Execution status of the agent run.</summary>
        public AgentStatus Status { get; set; }

        /// <summary>Log of all tool invocations made during this response.</summary>
        public List<AgentToolCallRecord> ToolCalls { get; set; } = new List<AgentToolCallRecord>();
    }

    /// <summary>
    /// Record of a single tool invocation during an agent response.
    /// </summary>
    public class AgentToolCallRecord
    {
        /// <summary>LLM reasoning text that preceded this tool call.</summary>
        public string Thought { get; set; }

        /// <summary>Name of the tool that was called.</summary>
        public string ToolName { get; set; }

        /// <summary>Arguments passed to the tool.</summary>
        public Dictionary<string, object> Arguments { get; set; }

        /// <summary>Result returned by the tool handler.</summary>
        public string Result { get; set; }
    }
}
