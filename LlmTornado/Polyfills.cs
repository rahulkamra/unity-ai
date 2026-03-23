// Polyfills for C# 9-11 features on .NET Framework 4.7.1 (Unity)
// These types are normally provided by the runtime but are missing on older targets.

#if !NET5_0_OR_GREATER

using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    // Required for 'init' property accessors (C# 9)
    internal static class IsExternalInit { }

    // Required for 'required' keyword (C# 11)
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }
        public string FeatureName { get; }
        public bool IsOptional { get; set; }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    // Required for 'required' keyword (C# 11)
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute { }

    // StringSyntax attribute for string format hints
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class StringSyntaxAttribute : Attribute
    {
        public const string Regex = "Regex";
        public const string Json = "Json";
        public const string Uri = "Uri";
        public StringSyntaxAttribute(string syntax) { Syntax = syntax; }
        public StringSyntaxAttribute(string syntax, params object[] arguments) { Syntax = syntax; Arguments = arguments; }
        public string Syntax { get; }
        public object[] Arguments { get; } = Array.Empty<object>();
    }

    // MemberNotNullWhen attribute for nullable analysis
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        public MemberNotNullWhenAttribute(bool returnValue, string member)
        {
            ReturnValue = returnValue;
            Members = new[] { member };
        }
        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }
        public bool ReturnValue { get; }
        public string[] Members { get; }
    }

    // MemberNotNull attribute for nullable analysis
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    internal sealed class MemberNotNullAttribute : Attribute
    {
        public MemberNotNullAttribute(string member)
        {
            Members = new[] { member };
        }
        public MemberNotNullAttribute(params string[] members)
        {
            Members = members;
        }
        public string[] Members { get; }
    }
}

#endif
