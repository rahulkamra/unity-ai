using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityAI.Toolkit.Prompts
{
    /// <summary>
    /// A reusable text template with {variable} placeholders.
    /// Supports variable substitution, partial formatting, and concatenation.
    /// </summary>
    [Serializable]
    public class PromptTemplate
    {
        private readonly string template;
        private readonly List<string> variables;

        private static readonly Regex VariablePattern = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        /// <summary>
        /// The raw template string with {variable} placeholders.
        /// </summary>
        public string Template => template;

        /// <summary>
        /// Creates a prompt template from a string with {variable} placeholders.
        /// </summary>
        public PromptTemplate(string template)
        {
            this.template = template ?? throw new ArgumentNullException(nameof(template));
            variables = ExtractVariables(template);
        }

        /// <summary>
        /// Returns the set of variable names found in the template.
        /// </summary>
        public List<string> GetVariables() => new List<string>(variables);

        /// <summary>
        /// Resolves all variables and returns the final string.
        /// Throws if any required variable is missing from the dictionary.
        /// </summary>
        public string Format(Dictionary<string, string> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            string result = VariablePattern.Replace(template, (Match match) =>
            {
                string name = match.Groups[1].Value;
                if (variables.TryGetValue(name, out string value))
                {
                    return value;
                }

                throw new ArgumentException($"Missing required variable: '{name}'");
            });

            return result;
        }

        /// <summary>
        /// Pre-fills some variables, returning a new PromptTemplate with fewer remaining placeholders.
        /// Variables not in the dictionary are left as {placeholders}.
        /// </summary>
        public PromptTemplate Partial(Dictionary<string, string> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            string result = VariablePattern.Replace(template, (Match match) =>
            {
                string name = match.Groups[1].Value;
                if (variables.TryGetValue(name, out string value))
                {
                    return value;
                }

                return match.Value;
            });

            return new PromptTemplate(result);
        }

        /// <summary>
        /// Concatenates this template with another, separated by the given separator.
        /// </summary>
        public PromptTemplate Concat(PromptTemplate other, string separator = "\n\n")
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new PromptTemplate(template + separator + other.template);
        }

        /// <summary>
        /// Implicit conversion from string for convenience.
        /// </summary>
        public static implicit operator PromptTemplate(string template) => new PromptTemplate(template);

        /// <summary>
        /// Returns the raw template string.
        /// </summary>
        public override string ToString() => template;

        private static List<string> ExtractVariables(string template)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            MatchCollection matches = VariablePattern.Matches(template);

            foreach (Match match in matches)
            {
                string name = match.Groups[1].Value;
                if (seen.Add(name))
                {
                    result.Add(name);
                }
            }

            return result;
        }
    }
}
