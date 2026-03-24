using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using Newtonsoft.Json;
using UnityAI.Toolkit.Prompts;
using UnityEngine;

namespace UnityAI.Toolkit.Agents
{
    /// <summary>
    /// A ReAct-style agent with native tool-calling, observable reasoning chain, and typed responses.
    /// Manages a conversation with an LLM and executes registered tools in a loop.
    /// </summary>
    public class ReactAgent
    {
        private const int DefaultMaxIterations = 10;

        private readonly TornadoApi api;
        private readonly ChatModel model;
        private readonly Dictionary<string, AgentTool> toolLookup = new Dictionary<string, AgentTool>();
        private readonly List<AgentTool> registeredTools = new List<AgentTool>();

        private Conversation conversation;
        private string systemPrompt;
        private ChatPromptTemplate promptTemplate;
        private Dictionary<string, string> promptVariables;
        private AgentCallbacks callbacks;
        private int maxIterations = DefaultMaxIterations;

        public ReactAgent(TornadoApi api, ChatModel model)
        {
            this.api = api;
            this.model = model;
        }

        /// <summary>
        /// Registers a tool that the agent can invoke.
        /// </summary>
        public void RegisterTool(AgentTool tool)
        {
            toolLookup[tool.Name] = tool;
            registeredTools.Add(tool);
        }

        /// <summary>
        /// Removes all registered tools.
        /// </summary>
        public void ClearTools()
        {
            toolLookup.Clear();
            registeredTools.Clear();
        }

        /// <summary>
        /// Sets the system prompt for the conversation as a raw string.
        /// Clears any previously set prompt template.
        /// </summary>
        public void SetSystemPrompt(string prompt)
        {
            systemPrompt = prompt;
            promptTemplate = null;
            promptVariables = null;
        }

        /// <summary>
        /// Sets a chat prompt template with variables for the conversation.
        /// The template is applied on each SendAsync call. Clears any raw system prompt.
        /// </summary>
        public void SetPromptTemplate(ChatPromptTemplate template, Dictionary<string, string> variables = null)
        {
            promptTemplate = template;
            promptVariables = variables;
            systemPrompt = null;
        }

        /// <summary>
        /// Updates prompt template variables without replacing the template.
        /// </summary>
        public void SetPromptVariables(Dictionary<string, string> variables)
        {
            promptVariables = variables;
        }

        /// <summary>
        /// Sets callback hooks for observing the agent's reasoning chain.
        /// </summary>
        public void SetCallbacks(AgentCallbacks callbacks)
        {
            this.callbacks = callbacks;
        }

        /// <summary>
        /// Sets the maximum number of tool-calling iterations before the agent stops.
        /// </summary>
        public void SetMaxIterations(int max)
        {
            maxIterations = max;
        }

        /// <summary>
        /// Sends a user message and processes the response with tool calling, returning a typed structured response.
        /// The LLM's final text response is deserialized into TResponse.
        /// For plain text responses, use <c>SendAsync&lt;string&gt;</c>.
        /// </summary>
        public async Task<AgentResponse<TResponse>> SendAsync<TResponse>(string userMessage, CancellationToken cancellationToken = default)
        {
            EnsureConversation();
            ApplyPrompt();
            SetupTools();
            SetupStructuredOutput<TResponse>();

            Debug.Log($"[ReactAgent] SendAsync<{typeof(TResponse).Name}> — model={model}, tools={registeredTools.Count}\n[ReactAgent] User: {ForLog(userMessage)}");

            conversation.AppendUserInput(userMessage);

            AgentResponse<TResponse> agentResponse = new AgentResponse<TResponse>();
            var (text, status) = await ExecuteLoopAsync(agentResponse.ToolCalls, cancellationToken);
            agentResponse.Status = status;

            if (status == AgentStatus.Success && text != null)
            {
                agentResponse.Response = DeserializeResponse<TResponse>(text);
                if (agentResponse.Response == null && typeof(TResponse) != typeof(string))
                {
                    agentResponse.Status = AgentStatus.Error;
                }
            }

            return agentResponse;
        }

        /// <summary>
        /// Sends a one-shot request to the LLM and returns a structured response.
        /// Creates a fresh, isolated conversation — does not touch the agent's conversation history.
        /// </summary>
        public async Task<TResponse> ReasonAsync<TResponse>(string reasoningSystemPrompt, string userMessage, CancellationToken cancellationToken = default)
        {
            Conversation reasoningConversation = api.Chat.CreateConversation(model);
            reasoningConversation.SetSystemMessage(reasoningSystemPrompt);

            string schemaName = typeof(TResponse).Name;
            object schema = GenerateJsonSchema(typeof(TResponse));
            reasoningConversation.RequestParameters.ResponseFormat = ChatRequestResponseFormats.StructuredJson(schemaName, schema);

            Debug.Log($"[ReactAgent] ReasonAsync<{schemaName}> — model={model}\n[ReactAgent] System: {ForLog(reasoningSystemPrompt)}\n[ReactAgent] User: {ForLog(userMessage)}");

            reasoningConversation.AppendUserInput(userMessage);

            string response = await reasoningConversation.GetResponse(cancellationToken);

            Debug.Log($"[ReactAgent] ReasonAsync<{schemaName}> raw response:\n{ForLog(response)}");

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogWarning("[ReactAgent] ReasonAsync received empty response from LLM.");
                return default;
            }

            return DeserializeResponse<TResponse>(response);
        }

        /// <summary>
        /// Resets the conversation history.
        /// </summary>
        public void ResetConversation()
        {
            conversation = null;
        }

        private async Task<(string finalText, AgentStatus status)> ExecuteLoopAsync(
            List<AgentToolCallRecord> toolCallLog,
            CancellationToken cancellationToken)
        {
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                bool toolsWereCalled = false;
                string thoughtText = null;

                Func<List<FunctionCall>, ValueTask> fnHandler = async (List<FunctionCall> calls) =>
                {
                    foreach (FunctionCall call in calls)
                    {
                        toolsWereCalled = true;
                        Dictionary<string, object> args = call.GetArguments();
                        string result;

                        if (callbacks?.OnToolStart != null)
                        {
                            await callbacks.OnToolStart(call.Name, args);
                        }

                        if (toolLookup.TryGetValue(call.Name, out AgentTool matchedTool))
                        {
                            try
                            {
                                result = await matchedTool.Handler(args);
                                Debug.Log($"[ReactAgent] Tool '{call.Name}' → {result}");
                            }
                            catch (Exception ex)
                            {
                                result = $"{{\"error\": \"{ex.Message}\"}}";
                                Debug.LogWarning($"[ReactAgent] Tool '{call.Name}' threw: {ex.Message}");
                                if (callbacks?.OnError != null)
                                {
                                    await callbacks.OnError(call.Name, ex);
                                }
                            }
                        }
                        else
                        {
                            result = $"{{\"error\": \"Unknown tool: {call.Name}\"}}";
                            Debug.LogWarning($"[ReactAgent] Unknown tool: {call.Name}");
                        }

                        if (callbacks?.OnToolEnd != null)
                        {
                            await callbacks.OnToolEnd(call.Name, args, result);
                        }

                        toolCallLog.Add(new AgentToolCallRecord
                        {
                            Thought = thoughtText,
                            ToolName = call.Name,
                            Arguments = args,
                            Result = result
                        });

                        call.Resolve(result);
                    }
                };

                ChatRichResponse response = await conversation.GetResponseRich(fnHandler, cancellationToken);

                if (response.Text != null)
                {
                    thoughtText = response.Text;
                }

                if (callbacks?.OnThought != null && thoughtText != null)
                {
                    await callbacks.OnThought(thoughtText);
                }

                if (!toolsWereCalled)
                {
                    string finalText = response.Text ?? "";
                    Debug.Log($"[ReactAgent] Final response:\n{ForLog(finalText)}");

                    if (callbacks?.OnResponse != null)
                    {
                        await callbacks.OnResponse(finalText);
                    }

                    return (finalText, AgentStatus.Success);
                }

                Debug.Log($"[ReactAgent] Iteration {iteration + 1}: {toolCallLog.Count} tool call(s) so far, looping...");
            }

            Debug.LogWarning($"[ReactAgent] Hit max tool iterations ({maxIterations}).");
            return (null, AgentStatus.MaxIterationsReached);
        }

        private void EnsureConversation()
        {
            if (conversation == null)
            {
                conversation = api.Chat.CreateConversation(model);
            }
        }

        private void ApplyPrompt()
        {
            if (promptTemplate != null)
            {
                promptTemplate.ApplyTo(conversation, promptVariables ?? new Dictionary<string, string>());
            }
            else
            {
                conversation.SetSystemMessage(systemPrompt ?? "");
            }
        }

        private void SetupTools()
        {
            List<Tool> tools = new List<Tool>();
            foreach (AgentTool agentTool in registeredTools)
            {
                Tool tool = new Tool(agentTool.Parameters, agentTool.Name, agentTool.Description, true);
                tools.Add(tool);
            }
            conversation.RequestParameters.Tools = tools;
        }

        private void SetupStructuredOutput<TResponse>()
        {
            if (typeof(TResponse) == typeof(string))
            {
                conversation.RequestParameters.ResponseFormat = null;
                return;
            }

            string schemaName = typeof(TResponse).Name;
            object schema = GenerateJsonSchema(typeof(TResponse));
            conversation.RequestParameters.ResponseFormat = ChatRequestResponseFormats.StructuredJson(schemaName, schema);
        }

        private static TResponse DeserializeResponse<TResponse>(string text)
        {
            if (typeof(TResponse) == typeof(string))
            {
                return (TResponse)(object)text;
            }

            try
            {
                TResponse result = JsonConvert.DeserializeObject<TResponse>(text);
                Debug.Log($"[ReactAgent] Successfully deserialized {typeof(TResponse).Name} response.");
                return result;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[ReactAgent] Failed to deserialize response into {typeof(TResponse).Name}: {ex.Message}\nRaw: {text}");
                return default;
            }
        }

        private static string ForLog(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "(empty)";
            }

            return text;
        }

        private static object GenerateJsonSchema(Type type)
        {
            if (type == typeof(string))
            {
                return new Dictionary<string, object> { { "type", "string" } };
            }

            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            {
                return new Dictionary<string, object> { { "type", "integer" } };
            }

            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                return new Dictionary<string, object> { { "type", "number" } };
            }

            if (type == typeof(bool))
            {
                return new Dictionary<string, object> { { "type", "boolean" } };
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = type.GetGenericArguments()[0];
                return new Dictionary<string, object>
                {
                    { "type", "array" },
                    { "items", GenerateJsonSchema(elementType) }
                };
            }

            if (type.IsClass || type.IsValueType && !type.IsPrimitive && !type.IsEnum)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                Dictionary<string, object> properties = new Dictionary<string, object>();
                List<string> required = new List<string>();

                foreach (FieldInfo field in fields)
                {
                    properties[field.Name] = GenerateJsonSchema(field.FieldType);
                    required.Add(field.Name);
                }

                return new Dictionary<string, object>
                {
                    { "type", "object" },
                    { "properties", properties },
                    { "required", required },
                    { "additionalProperties", false }
                };
            }

            return new Dictionary<string, object> { { "type", "string" } };
        }
    }
}
