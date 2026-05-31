using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class DescriptionTokenResolver
{
    private static readonly Regex TokenPattern = new Regex(@"\{([^{}]+)\}", RegexOptions.Compiled);
    private static readonly HashSet<string> ReportedWarnings = new HashSet<string>();

    public static string Resolve(ItemDefinition definition, int stackSize = 1)
    {
        return ResolveItemDescription(definition, null, stackSize, true, out _);
    }

    public static string Resolve(ItemRuntime runtime)
    {
        return runtime == null
            ? string.Empty
            : ResolveItemDescription(runtime.Definition, runtime, runtime.StackSize, true, out _);
    }

    public static string Resolve(ModifierDefinition definition, int attachedItemStackSize = 1)
    {
        return ResolveModifierDescription(definition, null, attachedItemStackSize, true, out _);
    }

    public static string Resolve(ModifierRuntime runtime)
    {
        return runtime == null
            ? string.Empty
            : ResolveModifierDescription(runtime.Definition, runtime, runtime.AttachedItemStackSize, true, out _);
    }

    public static bool Validate(ItemDefinition definition)
    {
        ResolveItemDescription(definition, null, 1, true, out bool isValid);
        return isValid;
    }

    public static bool Validate(ModifierDefinition definition)
    {
        ResolveModifierDescription(definition, null, 1, true, out bool isValid);
        return isValid;
    }

    private static string ResolveItemDescription(
        ItemDefinition definition,
        ItemRuntime runtime,
        int stackSize,
        bool warn,
        out bool isValid)
    {
        if (definition == null)
        {
            isValid = true;
            return string.Empty;
        }

        int resolvedStackSize = Mathf.Max(1, stackSize);
        return ResolveText(
            definition.description,
            token => TryResolveItemToken(definition, runtime, resolvedStackSize, token, out float value)
                ? TokenResolution.Success(value)
                : TokenResolution.Failure(),
            $"item '{definition.itemId}'",
            warn,
            out isValid);
    }

    private static string ResolveModifierDescription(
        ModifierDefinition definition,
        ModifierRuntime runtime,
        int stackSize,
        bool warn,
        out bool isValid)
    {
        if (definition == null)
        {
            isValid = true;
            return string.Empty;
        }

        int resolvedStackSize = Mathf.Max(1, stackSize);
        return ResolveText(
            definition.description,
            token => TryResolveModifierToken(definition, runtime, resolvedStackSize, token, out float value)
                ? TokenResolution.Success(value)
                : TokenResolution.Failure(),
            $"modifier '{definition.modifierId}'",
            warn,
            out isValid);
    }

    private static string ResolveText(
        string description,
        Func<Token, TokenResolution> resolve,
        string ownerLabel,
        bool warn,
        out bool isValid)
    {
        bool valid = true;
        if (string.IsNullOrWhiteSpace(description))
        {
            isValid = true;
            return string.Empty;
        }

        string unmatchedText = TokenPattern.Replace(description, string.Empty);
        if (unmatchedText.IndexOf('{') >= 0 || unmatchedText.IndexOf('}') >= 0)
        {
            valid = false;
            WarnOnce($"{ownerLabel} has unmatched braces in its description.", warn);
        }

        string resolvedDescription = TokenPattern.Replace(description, match =>
        {
            if (!TryParseToken(match.Groups[1].Value, out Token token))
            {
                valid = false;
                WarnOnce($"{ownerLabel} has malformed description token '{match.Value}'.", warn);
                return match.Value;
            }

            TokenResolution resolution = resolve(token);
            if (!resolution.IsResolved)
            {
                valid = false;
                WarnOnce($"{ownerLabel} has unresolved description token '{match.Value}'.", warn);
                return match.Value;
            }

            return FormatValue(resolution.Value, token.Format);
        });

        isValid = valid;
        return resolvedDescription;
    }

    private static bool TryResolveItemToken(
        ItemDefinition definition,
        ItemRuntime runtime,
        int stackSize,
        Token token,
        out float value)
    {
        switch (token.Namespace)
        {
            case "item":
                return TryResolveItemStat(definition, runtime, stackSize, token.Key, out value);
            case "player":
                return TryResolvePlayerStat(definition, stackSize, token.Key, out value);
            case "param":
                return TryResolveItemParameter(definition, runtime, stackSize, token.Key, out value);
            case "":
                return TryResolveItemStat(definition, runtime, stackSize, token.Key, out value) ||
                       TryResolvePlayerStat(definition, stackSize, token.Key, out value) ||
                       TryResolveItemParameter(definition, runtime, stackSize, token.Key, out value);
            default:
                value = 0f;
                return false;
        }
    }

    private static bool TryResolveModifierToken(
        ModifierDefinition definition,
        ModifierRuntime runtime,
        int stackSize,
        Token token,
        out float value)
    {
        switch (token.Namespace)
        {
            case "modifier":
                return TryResolveModifierField(definition, stackSize, token.Key, out value);
            case "param":
                return TryResolveModifierParameter(definition, runtime, stackSize, token.Key, out value);
            case "":
                return TryResolveModifierParameter(definition, runtime, stackSize, token.Key, out value) ||
                       TryResolveModifierField(definition, stackSize, token.Key, out value);
            default:
                value = 0f;
                return false;
        }
    }

    private static bool TryResolveItemStat(
        ItemDefinition definition,
        ItemRuntime runtime,
        int stackSize,
        string key,
        out float value)
    {
        if (!Enum.TryParse(key, true, out ItemStatType statType) ||
            !ItemStatUtility.TryGetItemStat(definition.itemStats, statType, stackSize, out value))
        {
            value = 0f;
            return false;
        }

        value = runtime != null ? runtime.GetItemStat(statType, value) : value;
        return true;
    }

    private static bool TryResolvePlayerStat(ItemDefinition definition, int stackSize, string key, out float value)
    {
        if (Enum.TryParse(key, true, out PlayerStatType statType) &&
            ItemStatUtility.TryGetPlayerStat(definition.playerStats, statType, stackSize, out value))
        {
            return true;
        }

        value = 0f;
        return false;
    }

    private static bool TryResolveItemParameter(
        ItemDefinition definition,
        ItemRuntime runtime,
        int stackSize,
        string key,
        out float value)
    {
        if (!ItemStatUtility.TryGetParameter(definition.parameters, key, stackSize, out value))
        {
            return false;
        }

        value = runtime != null ? runtime.GetParameter(key, value) : value;
        return true;
    }

    private static bool TryResolveModifierParameter(
        ModifierDefinition definition,
        ModifierRuntime runtime,
        int stackSize,
        string key,
        out float value)
    {
        return runtime != null
            ? TryGetRuntimeModifierParameter(definition, runtime, key, out value)
            : ItemStatUtility.TryGetParameter(definition.parameters, key, stackSize, out value);
    }

    private static bool TryGetRuntimeModifierParameter(
        ModifierDefinition definition,
        ModifierRuntime runtime,
        string key,
        out float value)
    {
        if (!ItemStatUtility.TryGetParameter(definition.parameters, key, runtime.AttachedItemStackSize, out _))
        {
            value = 0f;
            return false;
        }

        value = runtime.GetParameter(key);
        return true;
    }

    private static bool TryResolveModifierField(ModifierDefinition definition, int stackSize, string key, out float value)
    {
        if (string.Equals(key, "procCoefficient", StringComparison.OrdinalIgnoreCase))
        {
            value = definition.procCoefficient;
            return true;
        }

        if (string.Equals(key, "procCoefficientPerStack", StringComparison.OrdinalIgnoreCase))
        {
            value = definition.procCoefficient;
            return true;
        }

        if (string.Equals(key, "procCoefficientTotal", StringComparison.OrdinalIgnoreCase))
        {
            value = definition.procCoefficient * Mathf.Max(1, stackSize);
            return true;
        }

        if (string.Equals(key, "attachedItemStack", StringComparison.OrdinalIgnoreCase))
        {
            value = Mathf.Max(1, stackSize);
            return true;
        }

        if (string.Equals(key, "attachedItemAdditionalStacks", StringComparison.OrdinalIgnoreCase))
        {
            value = Mathf.Max(0, stackSize - 1);
            return true;
        }

        value = 0f;
        return false;
    }

    private static bool TryParseToken(string content, out Token token)
    {
        token = default;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        string[] formatParts = content.Split('|');
        if (formatParts.Length > 2)
        {
            return false;
        }

        string format = formatParts.Length == 2 ? formatParts[1].Trim().ToLowerInvariant() : "number";
        if (format != "number" && format != "percent" && format != "x")
        {
            return false;
        }

        string lookup = formatParts[0].Trim();
        int namespaceSeparator = lookup.IndexOf(':');
        string tokenNamespace = namespaceSeparator >= 0 ? lookup.Substring(0, namespaceSeparator).Trim().ToLowerInvariant() : string.Empty;
        string key = namespaceSeparator >= 0 ? lookup.Substring(namespaceSeparator + 1).Trim() : lookup;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        token = new Token(tokenNamespace, key, format);
        return true;
    }

    private static string FormatValue(float value, string format)
    {
        switch (format)
        {
            case "percent":
                return $"{FormatNumber(value * 100f)}%";
            case "x":
                return $"{FormatNumber(value)}x";
            default:
                return FormatNumber(value);
        }
    }

    private static string FormatNumber(float value)
    {
        return value.ToString("0.##");
    }

    private static void WarnOnce(string message, bool warn)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (warn && ReportedWarnings.Add(message))
        {
            Debug.LogWarning($"DescriptionTokenResolver: {message}");
        }
#endif
    }

    private readonly struct Token
    {
        public Token(string tokenNamespace, string key, string format)
        {
            Namespace = tokenNamespace;
            Key = key;
            Format = format;
        }

        public string Namespace { get; }
        public string Key { get; }
        public string Format { get; }
    }

    private readonly struct TokenResolution
    {
        private TokenResolution(bool isResolved, float value)
        {
            IsResolved = isResolved;
            Value = value;
        }

        public bool IsResolved { get; }
        public float Value { get; }

        public static TokenResolution Success(float value) => new TokenResolution(true, value);
        public static TokenResolution Failure() => new TokenResolution(false, 0f);
    }
}
