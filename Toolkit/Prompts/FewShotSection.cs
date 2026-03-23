using System;
using System.Collections.Generic;
using System.Text;

namespace UnityAI.Toolkit.Prompts
{
    /// <summary>
    /// A single input/output example for few-shot prompting.
    /// </summary>
    [Serializable]
    public class FewShotExample
    {
        /// <summary>Example input text.</summary>
        public string Input;

        /// <summary>Expected output text.</summary>
        public string Output;

        public FewShotExample(string input, string output)
        {
            Input = input;
            Output = output;
        }
    }

    /// <summary>
    /// Formats a list of few-shot examples into a prompt section.
    /// The output string can be plugged into any {variable} slot in a PromptTemplate.
    /// </summary>
    public static class FewShotSection
    {
        private const string DefaultExampleTemplate = "Input: {input}\nOutput: {output}";
        private const string DefaultSeparator = "\n\n";

        /// <summary>
        /// Formats examples into a string block using the given per-example template.
        /// The template should contain {input} and {output} placeholders.
        /// </summary>
        public static string Format(
            List<FewShotExample> examples,
            string exampleTemplate = DefaultExampleTemplate,
            string separator = DefaultSeparator)
        {
            if (examples == null || examples.Count == 0)
            {
                return string.Empty;
            }

            PromptTemplate template = new PromptTemplate(exampleTemplate);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < examples.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(separator);
                }

                Dictionary<string, string> variables = new Dictionary<string, string>
                {
                    { "input", examples[i].Input },
                    { "output", examples[i].Output }
                };

                sb.Append(template.Format(variables));
            }

            return sb.ToString();
        }
    }
}
