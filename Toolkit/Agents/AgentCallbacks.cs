using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityAI.Toolkit.Agents
{
    /// <summary>
    /// Optional callback hooks for observing the agent's reasoning chain.
    /// All callbacks are async and null-checked before invocation.
    /// </summary>
    public class AgentCallbacks
    {
        /// <summary>
        /// Called when the LLM produces reasoning text before a tool call or final answer.
        /// </summary>
        public Func<string, Task> OnThought;

        /// <summary>
        /// Called before a tool is executed. Receives tool name and parsed arguments.
        /// </summary>
        public Func<string, Dictionary<string, object>, Task> OnToolStart;

        /// <summary>
        /// Called after a tool finishes. Receives tool name, arguments, and result string.
        /// </summary>
        public Func<string, Dictionary<string, object>, string, Task> OnToolEnd;

        /// <summary>
        /// Called when the LLM returns its final response (no more tool calls).
        /// </summary>
        public Func<string, Task> OnResponse;

        /// <summary>
        /// Called when a tool throws or an error occurs. Receives context string and exception.
        /// </summary>
        public Func<string, Exception, Task> OnError;
    }
}
