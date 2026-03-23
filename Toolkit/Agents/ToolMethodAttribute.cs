using System;

namespace UnityAI.Toolkit.Agents
{
    /// <summary>
    /// Marks a method as an AI tool that can be invoked by the LLM.
    /// The method must return string (JSON result) and can have parameters of type
    /// string, int, float, or bool. Use [ToolParam] on parameters to add descriptions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ToolMethodAttribute : Attribute
    {
        /// <summary>Tool name used by the LLM to invoke this method.</summary>
        public string Name { get; }

        /// <summary>Description shown to the LLM explaining what the tool does.</summary>
        public string Description { get; }

        public ToolMethodAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Describes a parameter on a [ToolMethod] for the LLM.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ToolParamAttribute : Attribute
    {
        /// <summary>Description of this parameter shown to the LLM.</summary>
        public string Description { get; }

        /// <summary>Whether this parameter is required. Defaults to true.</summary>
        public bool Required { get; }

        public ToolParamAttribute(string description, bool required = true)
        {
            Description = description;
            Required = required;
        }
    }
}
