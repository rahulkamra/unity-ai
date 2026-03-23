using System;
using System.Collections.Generic;
using LlmTornado;
using LlmTornado.Chat;

namespace UnityAI.Toolkit.Prompts
{
    /// <summary>
    /// A template that produces a sequence of ChatMessages with variable substitution.
    /// Use the builder methods to define the message structure, then Format() or ApplyTo() to resolve.
    /// </summary>
    public class ChatPromptTemplate
    {
        private readonly List<ChatPromptEntry> entries = new List<ChatPromptEntry>();

        /// <summary>
        /// Adds a system message template.
        /// </summary>
        public ChatPromptTemplate System(string template)
        {
            entries.Add(new ChatPromptEntry(ChatMessageRoles.System, new PromptTemplate(template)));
            return this;
        }

        /// <summary>
        /// Adds a user message template.
        /// </summary>
        public ChatPromptTemplate User(string template)
        {
            entries.Add(new ChatPromptEntry(ChatMessageRoles.User, new PromptTemplate(template)));
            return this;
        }

        /// <summary>
        /// Adds an assistant message template.
        /// </summary>
        public ChatPromptTemplate Assistant(string template)
        {
            entries.Add(new ChatPromptEntry(ChatMessageRoles.Assistant, new PromptTemplate(template)));
            return this;
        }

        /// <summary>
        /// Adds a message with an explicit role and template.
        /// </summary>
        public ChatPromptTemplate Message(ChatMessageRoles role, string template)
        {
            entries.Add(new ChatPromptEntry(role, new PromptTemplate(template)));
            return this;
        }

        /// <summary>
        /// Returns the set of all variable names across all message templates.
        /// </summary>
        public List<string> GetVariables()
        {
            HashSet<string> seen = new HashSet<string>();
            List<string> result = new List<string>();

            foreach (ChatPromptEntry entry in entries)
            {
                foreach (string variable in entry.Template.GetVariables())
                {
                    if (seen.Add(variable))
                    {
                        result.Add(variable);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves all variables and returns the list of ChatMessages.
        /// </summary>
        public List<ChatMessage> Format(Dictionary<string, string> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            List<ChatMessage> messages = new List<ChatMessage>(entries.Count);

            foreach (ChatPromptEntry entry in entries)
            {
                string content = entry.Template.Format(variables);
                messages.Add(new ChatMessage(entry.Role, content));
            }

            return messages;
        }

        /// <summary>
        /// Resolves variables and applies the messages to a Conversation.
        /// Sets the system message via SetSystemMessage, appends all other messages.
        /// </summary>
        public void ApplyTo(Conversation conversation, Dictionary<string, string> variables)
        {
            if (conversation == null)
            {
                throw new ArgumentNullException(nameof(conversation));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            bool systemSet = false;

            foreach (ChatPromptEntry entry in entries)
            {
                string content = entry.Template.Format(variables);

                if (entry.Role == ChatMessageRoles.System && !systemSet)
                {
                    conversation.SetSystemMessage(content);
                    systemSet = true;
                }
                else
                {
                    conversation.AppendMessage(entry.Role, content);
                }
            }

            if (!systemSet)
            {
                conversation.SetSystemMessage("");
            }
        }

        /// <summary>
        /// Pre-fills some variables, returning a new ChatPromptTemplate with fewer remaining placeholders.
        /// </summary>
        public ChatPromptTemplate Partial(Dictionary<string, string> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            ChatPromptTemplate result = new ChatPromptTemplate();

            foreach (ChatPromptEntry entry in entries)
            {
                PromptTemplate partialTemplate = entry.Template.Partial(variables);
                result.entries.Add(new ChatPromptEntry(entry.Role, partialTemplate));
            }

            return result;
        }

        private struct ChatPromptEntry
        {
            public ChatMessageRoles Role;
            public PromptTemplate Template;

            public ChatPromptEntry(ChatMessageRoles role, PromptTemplate template)
            {
                Role = role;
                Template = template;
            }
        }
    }
}
