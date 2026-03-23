using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LlmTornado.Infra;
using UnityEngine;

namespace UnityAI.Toolkit.Agents
{
    /// <summary>
    /// Scans an object for methods marked with [ToolMethod] and creates AgentTool instances
    /// automatically from method signatures and attributes.
    /// </summary>
    public static class AgentToolBuilder
    {
        /// <summary>
        /// Scans the target object for all methods with [ToolMethod] and returns AgentTool instances.
        /// Methods must return string or Task&lt;string&gt; and have parameters of type string, int, float, or bool.
        /// </summary>
        public static List<AgentTool> CreateToolsFrom(object target)
        {
            List<AgentTool> tools = new List<AgentTool>();
            Type type = target.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (MethodInfo method in type.GetMethods(flags))
            {
                ToolMethodAttribute toolAttr = method.GetCustomAttribute<ToolMethodAttribute>();
                if (toolAttr == null)
                    continue;

                bool isAsync = method.ReturnType == typeof(Task<string>);
                bool isSync = method.ReturnType == typeof(string);

                if (!isSync && !isAsync)
                {
                    Debug.LogWarning($"[AgentToolBuilder] Skipping '{toolAttr.Name}': return type must be string or Task<string>, got {method.ReturnType.Name}");
                    continue;
                }

                List<ToolParam> toolParams = BuildParamList(method);

                if (isAsync)
                {
                    Func<Dictionary<string, object>, Task<string>> asyncHandler = BuildAsyncHandler(target, method);
                    tools.Add(new AgentTool(toolAttr.Name, toolAttr.Description, toolParams, asyncHandler));
                }
                else
                {
                    Func<Dictionary<string, object>, string> handler = BuildHandler(target, method);
                    tools.Add(new AgentTool(toolAttr.Name, toolAttr.Description, toolParams, handler));
                }
            }

            return tools;
        }

        private static List<ToolParam> BuildParamList(MethodInfo method)
        {
            List<ToolParam> toolParams = new List<ToolParam>();
            ParameterInfo[] parameters = method.GetParameters();

            foreach (ParameterInfo param in parameters)
            {
                ToolParamAttribute paramAttr = param.GetCustomAttribute<ToolParamAttribute>();
                string description = paramAttr != null ? paramAttr.Description : param.Name;
                bool required = paramAttr == null || paramAttr.Required;

                ToolParamAtomicTypes atomicType = MapType(param.ParameterType);
                if (atomicType == (ToolParamAtomicTypes)(-1))
                {
                    Debug.LogWarning($"[AgentToolBuilder] Unsupported parameter type '{param.ParameterType.Name}' on '{method.Name}.{param.Name}', defaulting to string");
                    atomicType = ToolParamAtomicTypes.String;
                }

                toolParams.Add(new ToolParam(param.Name, description, atomicType, required));
            }

            return toolParams;
        }

        private static ToolParamAtomicTypes MapType(Type type)
        {
            if (type == typeof(string))
                return ToolParamAtomicTypes.String;
            if (type == typeof(int))
                return ToolParamAtomicTypes.Int;
            if (type == typeof(float))
                return ToolParamAtomicTypes.Float;
            if (type == typeof(bool))
                return ToolParamAtomicTypes.Bool;

            return (ToolParamAtomicTypes)(-1);
        }

        private static Func<Dictionary<string, object>, string> BuildHandler(object target, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();

            return (Dictionary<string, object> args) =>
            {
                object[] invokeArgs = BuildInvokeArgs(parameters, args);
                return (string)method.Invoke(target, invokeArgs);
            };
        }

        private static Func<Dictionary<string, object>, Task<string>> BuildAsyncHandler(object target, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();

            return (Dictionary<string, object> args) =>
            {
                object[] invokeArgs = BuildInvokeArgs(parameters, args);
                return (Task<string>)method.Invoke(target, invokeArgs);
            };
        }

        private static object[] BuildInvokeArgs(ParameterInfo[] parameters, Dictionary<string, object> args)
        {
            object[] invokeArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo param = parameters[i];
                string paramName = param.Name;

                if (!args.TryGetValue(paramName, out object rawValue))
                {
                    invokeArgs[i] = GetDefault(param.ParameterType);
                    continue;
                }

                invokeArgs[i] = ConvertArg(rawValue, param.ParameterType);
            }

            return invokeArgs;
        }

        private static object ConvertArg(object raw, Type targetType)
        {
            if (raw == null)
                return GetDefault(targetType);

            if (targetType == typeof(string))
                return raw.ToString();

            if (targetType == typeof(int))
                return Convert.ToInt32(raw);

            if (targetType == typeof(float))
                return Convert.ToSingle(raw);

            if (targetType == typeof(bool))
                return Convert.ToBoolean(raw);

            return Convert.ChangeType(raw, targetType);
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }
    }
}
