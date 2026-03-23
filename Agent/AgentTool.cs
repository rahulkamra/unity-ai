using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.Infra;

namespace UnityAI.Agent
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

        /// <summary>Handler that receives parsed arguments and returns a JSON result string.</summary>
        public Func<Dictionary<string, object>, string> Handler { get; }

        /// <summary>Async handler for tools that require awaiting (e.g., image generation).</summary>
        public Func<Dictionary<string, object>, Task<string>> AsyncHandler { get; }

        /// <summary>Whether this tool uses an async handler.</summary>
        public bool IsAsync => AsyncHandler != null;

        public AgentTool(string name, string description, List<ToolParam> parameters, Func<Dictionary<string, object>, string> handler)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
            Handler = handler;
        }

        public AgentTool(string name, string description, List<ToolParam> parameters, Func<Dictionary<string, object>, Task<string>> asyncHandler)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
            AsyncHandler = asyncHandler;
        }
    }
}
