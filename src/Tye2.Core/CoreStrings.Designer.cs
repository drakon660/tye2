// Generated from CoreStrings.resx — update this file if you modify the resx.

#nullable enable
using System.Reflection;


namespace Tye2.Core
{
    internal static partial class CoreStrings
    {
        private static global::System.Resources.ResourceManager? s_resourceManager;
        public static global::System.Resources.ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new global::System.Resources.ResourceManager(typeof(CoreStrings)));
        public static global::System.Globalization.CultureInfo? Culture { get; set; }
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("defaultValue")]
        internal static string? GetResourceString(string resourceKey, string? defaultValue = null) =>  ResourceManager.GetString(resourceKey, Culture) ?? defaultValue;

        private static string GetResourceString(string resourceKey, string[]? formatterNames)
        {
           var value = GetResourceString(resourceKey) ?? "";
           if (formatterNames != null)
           {
               for (var i = 0; i < formatterNames.Length; i++)
               {
                   value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
               }
           }
           return value;
        }

        /// <summary>Expected scalar value for key: "{key}".</summary>
        public static string @ExpectedYamlScalar => GetResourceString("ExpectedYamlScalar")!;
        /// <summary>Expected scalar value for key: "{key}".</summary>
        internal static string FormatExpectedYamlScalar(object? key)
           => string.Format(Culture, GetResourceString("ExpectedYamlScalar", new[] { "key" }), key);

        /// <summary>Expected yaml sequence for key: "{key}".</summary>
        public static string @ExpectedYamlSequence => GetResourceString("ExpectedYamlSequence")!;
        /// <summary>Expected yaml sequence for key: "{key}".</summary>
        internal static string FormatExpectedYamlSequence(object? key)
           => string.Format(Culture, GetResourceString("ExpectedYamlSequence", new[] { "key" }), key);

        /// <summary>Configuration validation failed. Extensions must provide a name.</summary>
        public static string @ExtensionMustProvideAName => GetResourceString("ExtensionMustProvideAName")!;
        /// <summary>Ingress bindings must be http or https.</summary>
        public static string @IngressBindingMustBeHttpOrHttps => GetResourceString("IngressBindingMustBeHttpOrHttps")!;
        /// <summary>Ingress rules references a service that does not exist.</summary>
        public static string @IngressRuleMustReferenceService => GetResourceString("IngressRuleMustReferenceService")!;
        /// <summary>Cannot have multiple {0} bindings without names. Please specify names for each {0} binding.</summary>
        public static string @MultipleBindingWithoutName => GetResourceString("MultipleBindingWithoutName")!;
        /// <summary>Cannot have multiple {0} bindings without names. Please specify names for each {0} binding.</summary>
        internal static string FormatMultipleBindingWithoutName(object? p0)
           => string.Format(Culture, GetResourceString("MultipleBindingWithoutName") ?? "", p0);

        /// <summary>Cannot have multiple {0} bindings with the same name.</summary>
        public static string @MultipleBindingWithSameName => GetResourceString("MultipleBindingWithSameName")!;
        /// <summary>Cannot have multiple {0} bindings with the same name.</summary>
        internal static string FormatMultipleBindingWithSameName(object? p0)
           => string.Format(Culture, GetResourceString("MultipleBindingWithSameName") ?? "", p0);

        /// <summary>Cannot have multiple {0} bindings with the same port.</summary>
        public static string @MultipleBindingWithSamePort => GetResourceString("MultipleBindingWithSamePort")!;
        /// <summary>Cannot have multiple {0} bindings with the same port.</summary>
        internal static string FormatMultipleBindingWithSamePort(object? p0)
           => string.Format(Culture, GetResourceString("MultipleBindingWithSamePort") ?? "", p0);

        /// <summary>A prober must be configured for the {0} probe.</summary>
        public static string @ProberRequired => GetResourceString("ProberRequired")!;
        /// <summary>A prober must be configured for the {0} probe.</summary>
        internal static string FormatProberRequired(object? p0)
           => string.Format(Culture, GetResourceString("ProberRequired") ?? "", p0);

        /// <summary>"successThreshold" for {0} probe must be set to "1".</summary>
        public static string @SuccessThresholdMustBeOne => GetResourceString("SuccessThresholdMustBeOne")!;
        /// <summary>"successThreshold" for {0} probe must be set to "1".</summary>
        internal static string FormatSuccessThresholdMustBeOne(object? p0)
           => string.Format(Culture, GetResourceString("SuccessThresholdMustBeOne") ?? "", p0);

        /// <summary>"{value}" must be a boolean value (true/false).</summary>
        public static string @MustBeABoolean => GetResourceString("MustBeABoolean")!;
        /// <summary>"{value}" must be a boolean value (true/false).</summary>
        internal static string FormatMustBeABoolean(object? value)
           => string.Format(Culture, GetResourceString("MustBeABoolean", new[] { "value" }), value);

        /// <summary>"{value}" value must be an integer.</summary>
        public static string @MustBeAnInteger => GetResourceString("MustBeAnInteger")!;
        /// <summary>"{value}" value must be an integer.</summary>
        internal static string FormatMustBeAnInteger(object? value)
           => string.Format(Culture, GetResourceString("MustBeAnInteger", new[] { "value" }), value);

        /// <summary>"{value}" value must be an IP address, "*" or "localhost".</summary>
        public static string @MustBeAnIPAddress => GetResourceString("MustBeAnIPAddress")!;
        /// <summary>"{value}" value must be an IP address, "*" or "localhost".</summary>
        internal static string FormatMustBeAnIPAddress(object? value)
           => string.Format(Culture, GetResourceString("MustBeAnIPAddress", new[] { "value" }), value);

        /// <summary>"{value}" value cannot be negative.</summary>
        public static string @MustBePositive => GetResourceString("MustBePositive")!;
        /// <summary>"{value}" value cannot be negative.</summary>
        internal static string FormatMustBePositive(object? value)
           => string.Format(Culture, GetResourceString("MustBePositive", new[] { "value" }), value);

        /// <summary>"{value}" value must be greater than zero.</summary>
        public static string @MustBeGreaterThanZero => GetResourceString("MustBeGreaterThanZero")!;
        /// <summary>"{value}" value must be greater than zero.</summary>
        internal static string FormatMustBeGreaterThanZero(object? value)
           => string.Format(Culture, GetResourceString("MustBeGreaterThanZero", new[] { "value" }), value);

        /// <summary>Cannot have both "{0}" and "{1}" set for a service. Only one of project, image, and executable can be set for a given service.</summary>
        public static string @ProjectImageExecutableExclusive => GetResourceString("ProjectImageExecutableExclusive")!;
        /// <summary>Cannot have both "{0}" and "{1}" set for a service. Only one of project, image, and executable can be set for a given service.</summary>
        internal static string FormatProjectImageExecutableExclusive(object? p0, object? p1)
           => string.Format(Culture, GetResourceString("ProjectImageExecutableExclusive") ?? "", p0, p1);

        /// <summary>Services must have unique names.</summary>
        public static string @ServiceMustHaveUniqueNames => GetResourceString("ServiceMustHaveUniqueNames")!;
        /// <summary>Unexpected node type in the tye configuration file. Expected "{expected}" but got "{actual}".</summary>
        public static string @UnexpectedType => GetResourceString("UnexpectedType")!;
        /// <summary>Unexpected node type in the tye configuration file. Expected "{expected}" but got "{actual}".</summary>
        internal static string FormatUnexpectedType(object? expected, object? actual)
           => string.Format(Culture, GetResourceString("UnexpectedType", new[] { "expected", "actual" }), expected, actual);

        /// <summary>Unexpected key "{key}" in the tye configuration file.</summary>
        public static string @UnrecognizedKey => GetResourceString("UnrecognizedKey")!;
        /// <summary>Unexpected key "{key}" in the tye configuration file.</summary>
        internal static string FormatUnrecognizedKey(object? key)
           => string.Format(Culture, GetResourceString("UnrecognizedKey", new[] { "key" }), key);

        /// <summary>Unexpected node type in the tye configuration file. Expected one of ({expected}) but got "{actual}".</summary>
        public static string @UnexpectedTypes => GetResourceString("UnexpectedTypes")!;
        /// <summary>Unexpected node type in the tye configuration file. Expected one of ({expected}) but got "{actual}".</summary>
        internal static string FormatUnexpectedTypes(object? expected, object? actual)
           => string.Format(Culture, GetResourceString("UnexpectedTypes", new[] { "expected", "actual" }), expected, actual);

        /// <summary>Path "{path}" was not found.</summary>
        public static string @PathNotFound => GetResourceString("PathNotFound")!;
        /// <summary>Path "{path}" was not found.</summary>
        internal static string FormatPathNotFound(object? path)
           => string.Format(Culture, GetResourceString("PathNotFound", new[] { "path" }), path);

        /// <summary>Expected a value for environment variable "{key}".</summary>
        public static string @ExpectedEnvironmentVariableValue => GetResourceString("ExpectedEnvironmentVariableValue")!;
        /// <summary>Expected a value for environment variable "{key}".</summary>
        internal static string FormatExpectedEnvironmentVariableValue(object? key)
           => string.Format(Culture, GetResourceString("ExpectedEnvironmentVariableValue", new[] { "key" }), key);
    }
}
