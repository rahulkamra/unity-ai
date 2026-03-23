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
using UnityEngine;

namespace UnityAI.Agent
{
    /// <summary>
    /// Reusable AI agent with native tool-calling support.
    /// Manages a conversation with an LLM and executes registered tools in a loop.
    /// </summary>
    public class AIAgent
    {
        private const int MaxToolIterations = 10;

        private readonly TornadoApi api;
        private readonly ChatModel model;
        private readonly Dictionary<string, AgentTool> toolLookup = new Dictionary<string, AgentTool>();
        private readonly List<AgentTool> registeredTools = new List<AgentTool>();

        private Conversation conversation;
        private string systemPrompt;

        public AIAgent(TornadoApi api, ChatModel model)
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
        /// Sets the system prompt for the conversation.
        /// </summary>
        public void SetSystemPrompt(string prompt)
        {
            systemPrompt = prompt;
        }

        /// <summary>
        /// Sends a user message and processes the response, executing any tool calls in a loop.
        /// </summary>
        public async Task<AgentResponse> SendAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            EnsureConversation();
            conversation.SetSystemMessage(systemPrompt ?? "");

            // Set tools on the conversation
            List<Tool> tools = new List<Tool>();
            foreach (AgentTool agentTool in registeredTools)
            {
                Tool tool = new Tool(agentTool.Parameters, agentTool.Name, agentTool.Description, true);
                tools.Add(tool);
            }
            conversation.RequestParameters.Tools = tools;

            Debug.Log($"[AIAgent] SendAsync — model={model}, tools={registeredTools.Count}\n[AIAgent] System: {ForLog(systemPrompt)}\n[AIAgent] User: {ForLog(userMessage)}");

            conversation.AppendUserInput(userMessage);

            AgentResponse agentResponse = new AgentResponse();

            for (int iteration = 0; iteration < MaxToolIterations; iteration++)
            {
                bool toolsWereCalled = false;

                Func<List<FunctionCall>, ValueTask> fnHandler = async (List<FunctionCall> calls) =>
                {
                    foreach (FunctionCall call in calls)
                    {
                        toolsWereCalled = true;
                        Dictionary<string, object> args = call.GetArguments();
                        string result;

                        if (toolLookup.TryGetValue(call.Name, out AgentTool matchedTool))
                        {
                            try
                            {
                                if (matchedTool.IsAsync)
                                {
                                    result = await matchedTool.AsyncHandler(args);
                                }
                                else
                                {
                                    result = matchedTool.Handler(args);
                                }
                                Debug.Log($"[AIAgent] Tool '{call.Name}' → {result}");
                            }
                            catch (Exception ex)
                            {
                                result = $"{{\"error\": \"{ex.Message}\"}}";
                                Debug.LogWarning($"[AIAgent] Tool '{call.Name}' threw: {ex.Message}");
                            }
                        }
                        else
                        {
                            result = $"{{\"error\": \"Unknown tool: {call.Name}\"}}";
                            Debug.LogWarning($"[AIAgent] Unknown tool: {call.Name}");
                        }

                        agentResponse.ToolCalls.Add(new AgentToolCallRecord
                        {
                            ToolName = call.Name,
                            Arguments = args,
                            Result = result
                        });

                        call.Resolve(result);
                    }
                };

                ChatRichResponse response = await conversation.GetResponseRich(fnHandler, cancellationToken);

                if (!toolsWereCalled)
                {
                    agentResponse.Text = response.Text ?? "";
                    Debug.Log($"[AIAgent] SendAsync response:\n{ForLog(agentResponse.Text)}");
                    return agentResponse;
                }

                // Tools were called — loop to let the LLM process results
                Debug.Log($"[AIAgent] Iteration {iteration + 1}: {agentResponse.ToolCalls.Count} tool call(s) so far, looping...");
            }

            Debug.LogWarning($"[AIAgent] Hit max tool iterations ({MaxToolIterations}).");
            agentResponse.Text = "[Agent reached maximum tool call iterations]";
            return agentResponse;
        }

        /// <summary>
        /// Sends a user message and processes the response with tool calling, returning a typed structured response.
        /// The LLM's final text response is structured JSON matching TResponse (via response_format).
        /// </summary>
        public async Task<AgentResponse<TResponse>> SendAsync<TResponse>(string userMessage, CancellationToken cancellationToken = default)
        {
            EnsureConversation();
            conversation.SetSystemMessage(systemPrompt ?? "");

            // Set tools on the conversation
            List<Tool> tools = new List<Tool>();
            foreach (AgentTool agentTool in registeredTools)
            {
                Tool tool = new Tool(agentTool.Parameters, agentTool.Name, agentTool.Description, true);
                tools.Add(tool);
            }
            conversation.RequestParameters.Tools = tools;

            // Set structured JSON response format
            string schemaName = typeof(TResponse).Name;
            object schema = GenerateJsonSchema(typeof(TResponse));
            conversation.RequestParameters.ResponseFormat = ChatRequestResponseFormats.StructuredJson(schemaName, schema);

            Debug.Log($"[AIAgent] SendAsync<{schemaName}> — model={model}, tools={registeredTools.Count}\n[AIAgent] System: {ForLog(systemPrompt)}\n[AIAgent] User: {ForLog(userMessage)}");

            conversation.AppendUserInput(userMessage);

            AgentResponse<TResponse> agentResponse = new AgentResponse<TResponse>();

            for (int iteration = 0; iteration < MaxToolIterations; iteration++)
            {
                bool toolsWereCalled = false;

                Func<List<FunctionCall>, ValueTask> fnHandler = async (List<FunctionCall> calls) =>
                {
                    foreach (FunctionCall call in calls)
                    {
                        toolsWereCalled = true;
                        Dictionary<string, object> args = call.GetArguments();
                        string result;

                        if (toolLookup.TryGetValue(call.Name, out AgentTool matchedTool))
                        {
                            try
                            {
                                if (matchedTool.IsAsync)
                                {
                                    result = await matchedTool.AsyncHandler(args);
                                }
                                else
                                {
                                    result = matchedTool.Handler(args);
                                }
                                Debug.Log($"[AIAgent] Tool '{call.Name}' → {result}");
                            }
                            catch (Exception ex)
                            {
                                result = $"{{\"error\": \"{ex.Message}\"}}";
                                Debug.LogWarning($"[AIAgent] Tool '{call.Name}' threw: {ex.Message}");
                            }
                        }
                        else
                        {
                            result = $"{{\"error\": \"Unknown tool: {call.Name}\"}}";
                            Debug.LogWarning($"[AIAgent] Unknown tool: {call.Name}");
                        }

                        agentResponse.ToolCalls.Add(new AgentToolCallRecord
                        {
                            ToolName = call.Name,
                            Arguments = args,
                            Result = result
                        });

                        call.Resolve(result);
                    }
                };

                ChatRichResponse response = await conversation.GetResponseRich(fnHandler, cancellationToken);

                if (!toolsWereCalled)
                {
                    string rawText = response.Text ?? "";
                    Debug.Log($"[AIAgent] SendAsync<{schemaName}> raw response:\n{ForLog(rawText)}");
                    try
                    {
                        agentResponse.Response = JsonConvert.DeserializeObject<TResponse>(rawText);
                        Debug.Log($"[AIAgent] Successfully deserialized {schemaName} from structured response.");
                    }
                    catch (JsonException ex)
                    {
                        Debug.LogError($"[AIAgent] Failed to deserialize structured response into {schemaName}: {ex.Message}\nRaw: {rawText}");
                    }
                    return agentResponse;
                }

                Debug.Log($"[AIAgent] Iteration {iteration + 1}: {agentResponse.ToolCalls.Count} tool call(s) so far, looping...");
            }

            Debug.LogWarning($"[AIAgent] Hit max tool iterations ({MaxToolIterations}).");
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

            Debug.Log($"[AIAgent] ReasonAsync<{schemaName}> — model={model}\n[AIAgent] System: {ForLog(reasoningSystemPrompt)}\n[AIAgent] User: {ForLog(userMessage)}");

            reasoningConversation.AppendUserInput(userMessage);

            string response = await reasoningConversation.GetResponse(cancellationToken);

            Debug.Log($"[AIAgent] ReasonAsync<{schemaName}> raw response:\n{ForLog(response)}");

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogWarning("[AIAgent] ReasonAsync received empty response from LLM.");
                return default;
            }

            try
            {
                TResponse result = JsonConvert.DeserializeObject<TResponse>(response);
                Debug.Log($"[AIAgent] Successfully deserialized {schemaName} response.");
                return result;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[AIAgent] Failed to deserialize response into {schemaName}: {ex.Message}\nRaw: {response}");
                return default;
            }
        }

        /// <summary>
        /// Resets the conversation history.
        /// </summary>
        public void ResetConversation()
        {
            conversation = null;
        }

        private void EnsureConversation()
        {
            if (conversation == null)
            {
                conversation = api.Chat.CreateConversation(model);
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
