#if NETSTANDARD2_1
using System;

namespace System.Runtime.CompilerServices
{
    // Support for init-only setters, records, and required members on older frameworks
    public sealed class IsExternalInit { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public string FeatureName { get; }
        public bool RequiresOptIn { get; init; }

        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        public const string RefSafetyRules = nameof(RefSafetyRules);
        public const string NullableReferenceTypes = nameof(NullableReferenceTypes);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class SetsRequiredMembersAttribute : Attribute { }
}
#endif
