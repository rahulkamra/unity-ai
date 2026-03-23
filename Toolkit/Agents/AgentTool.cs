using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.Infra;

namespace UnityAI.Toolkit.Agents
{
    /// <summary>
    /// Defines a tool that an AI agent can invoke during conversation.
    /// </summary>
    public class AgentTool
    {
        /// <summary>Name of the tool (used by the LLM to invoke it).</summary>
        public string Name { get; }

        /// <summary>Description of what the tool does (shown to the LLM).</summary>
        public string Description { get; }

        /// <summary>Parameter definitions for the tool.</summary>
        public List<ToolParam> Parameters { get; }

        /// <summary>Async handler that receives parsed arguments and returns a JSON result string.</summary>
        public Func<Dictionary<string, object>, Task<string>> Handler { get; }

        public AgentTool(string name, string description, List<ToolParam> parameters, Func<Dictionary<string, object>, Task<string>> handler)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
            Handler = handler;
        }
    }
}
