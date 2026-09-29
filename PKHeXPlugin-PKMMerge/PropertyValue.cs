using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using PKHeX.Core;

namespace PKMMerge;

/// <summary>Display text for a property value, and whether it can be meaningfully compared.</summary>
internal readonly record struct PropertyValue(string Display, bool IsComparable)
{
    public static readonly PropertyValue None = new("", true);

    private delegate Span<T> SpanGetter<in TObj, T>(TObj obj);
    private delegate ReadOnlySpan<T> ReadOnlySpanGetter<in TObj, T>(TObj obj);

    private sealed class SpanAccessor(Func<PKM, byte[]> read, Action<PKM, byte[]>? write)
    {
        public Func<PKM, byte[]> Read { get; } = read;
        public Action<PKM, byte[]>? Write { get; } = write;
    }

    // PropertyInfo instances are cached by EntityBatchEditor, so reference identity is stable.
    private static readonly Dictionary<PropertyInfo, SpanAccessor?> SpanCache = [];

    public static PropertyValue Read(PropertyInfo pi, PKM pk)
    {
        if (!pi.CanRead || pi.GetIndexParameters().Length != 0)
            return Incomparable(pi);

        // Span properties can't go through PropertyInfo.GetValue; read them through a bound getter instead.
        if (pi.PropertyType.IsByRefLike)
            return GetSpanAccessor(pi) is { } span ? new(Convert.ToHexString(span.Read(pk)), true) : Incomparable(pi);

        object? value;
        try { value = pi.GetValue(pk); }
        catch (TargetInvocationException) { return new("<error>", false); }

        return value switch
        {
            null => new("null", true),
            byte[] b => new(Convert.ToHexString(b), true),
            Array a => new(string.Join(", ", a.Cast<object?>()), true),
            PersonalInfo p => new(Convert.ToHexString(p.Write()), true),
            _ when HasDefaultToString(value) => Incomparable(pi),
            _ => new(value.ToString() ?? "null", true),
        };
    }

    public static bool CanCopy(PropertyInfo src, PKM srcPk, PropertyInfo dst, PKM dstPk)
    {
        if (!src.CanRead || src.GetIndexParameters().Length != 0 || dst.GetIndexParameters().Length != 0)
            return false;

        if (src.PropertyType.IsByRefLike || dst.PropertyType.IsByRefLike)
        {
            return GetSpanAccessor(src) is { } s
                && GetSpanAccessor(dst) is { Write: not null } d
                && s.Read(srcPk).Length == d.Read(dstPk).Length;
        }
        return IsWritable(dst);
    }

    /// <summary>True if the property can be set from user-entered text.</summary>
    public static bool CanEdit(PropertyInfo pi)
    {
        if (!pi.CanRead || pi.GetIndexParameters().Length != 0)
            return false;
        if (pi.PropertyType.IsByRefLike)
            return GetSpanAccessor(pi) is { Write: not null };
        if (!IsWritable(pi))
            return false;

        var type = pi.PropertyType;
        return type.IsArray ? type.GetElementType() is { } e && IsScalar(e) : IsScalar(type);
    }

    /// <summary>Parses <paramref name="text"/> in the same format <see cref="Read"/> displays, and writes it to <paramref name="pk"/>.</summary>
    public static bool TryWrite(PropertyInfo pi, PKM pk, string text, out string error)
    {
        error = "";
        try
        {
            if (pi.PropertyType.IsByRefLike)
            {
                if (GetSpanAccessor(pi) is not { Write: { } write } span)
                    return Fail("This value is read-only.", out error);
                if (!TryParseHex(text, span.Read(pk).Length, out var bytes, out error))
                    return false;
                write(pk, bytes);
                return true;
            }

            var type = pi.PropertyType;
            object? value;
            if (type == typeof(byte[]))
            {
                int length = (pi.GetValue(pk) as byte[])?.Length ?? -1;
                if (!TryParseHex(text, length, out var bytes, out error))
                    return false;
                value = bytes;
            }
            else if (type.IsArray)
            {
                var element = type.GetElementType()!;
                var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (pi.GetValue(pk) is Array current && current.Length != parts.Length)
                    return Fail($"Expected {current.Length} values, got {parts.Length}.", out error);

                var array = Array.CreateInstance(element, parts.Length);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!TryParseScalar(element, parts[i], out var item))
                        return Fail($"\"{parts[i]}\" is not a valid {element.Name}.", out error);
                    array.SetValue(item, i);
                }
                value = array;
            }
            else if (!TryParseScalar(type, text, out value))
            {
                return Fail($"\"{text}\" is not a valid {GetTypeName(type)}.", out error);
            }

            pi.SetValue(pk, value);
            return true;
        }
        catch (TargetInvocationException ex)
        {
            return Fail(ex.InnerException?.Message ?? ex.Message, out error);
        }
        catch (ArgumentException ex)
        {
            return Fail(ex.Message, out error);
        }
    }

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }

    private static bool IsWritable(PropertyInfo pi) => pi.SetMethod is { IsPublic: true };

    private static bool IsScalar(Type type) => type == typeof(string) || type.IsEnum || type.IsPrimitive || type == typeof(decimal);

    private static bool TryParseScalar(Type type, string text, out object? value)
    {
        value = null;
        if (type == typeof(string))
        {
            value = text;
            return true;
        }

        text = text.Trim();
        if (type.IsEnum)
            return Enum.TryParse(type, text, ignoreCase: true, out value);
        if (type == typeof(bool))
        {
            bool ok = bool.TryParse(text, out var b);
            value = b;
            return ok;
        }

        try
        {
            value = Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException)
        {
            return false;
        }
    }

    /// <param name="expectedLength">Required byte count, or -1 for any length.</param>
    private static bool TryParseHex(string text, int expectedLength, out byte[] bytes, out string error)
    {
        bytes = [];
        error = "";
        var hex = new string(text.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];

        try { bytes = Convert.FromHexString(hex); }
        catch (FormatException) { return Fail("Invalid hex; use an even number of 0-9/A-F digits.", out error); }

        if (expectedLength >= 0 && bytes.Length != expectedLength)
            return Fail($"Expected {expectedLength} bytes, got {bytes.Length}.", out error);
        return true;
    }

    public static void Copy(PropertyInfo src, PKM srcPk, PropertyInfo dst, PKM dstPk)
    {
        if (src.PropertyType.IsByRefLike || dst.PropertyType.IsByRefLike)
        {
            var data = GetSpanAccessor(src)!.Read(srcPk);
            GetSpanAccessor(dst)!.Write!(dstPk, data);
            return;
        }

        var value = src.GetValue(srcPk);
        // Avoid sharing array instances between the two entities.
        dst.SetValue(dstPk, value is Array a ? a.Clone() : value);
    }

    private static PropertyValue Incomparable(PropertyInfo pi) => new(GetTypeName(pi.PropertyType), false);

    private static bool HasDefaultToString(object value)
    {
        var declaring = value.GetType().GetMethod(nameof(ToString), Type.EmptyTypes)?.DeclaringType;
        return declaring == typeof(object) || declaring == typeof(ValueType);
    }

    private static string GetTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;
        var name = type.Name[..type.Name.IndexOf('`')];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(GetTypeName))}>";
    }

    private static SpanAccessor? GetSpanAccessor(PropertyInfo pi)
    {
        if (SpanCache.TryGetValue(pi, out var cached))
            return cached;

        SpanAccessor? result;
        try { result = CreateSpanAccessor(pi); }
        catch (ArgumentException) { result = null; }
        catch (TargetInvocationException) { result = null; }
        SpanCache[pi] = result;
        return result;
    }

    private static SpanAccessor? CreateSpanAccessor(PropertyInfo pi)
    {
        var type = pi.PropertyType;
        if (!type.IsGenericType || pi.GetMethod is not { IsStatic: false } getter || getter.DeclaringType is not { } owner)
            return null;

        var definition = type.GetGenericTypeDefinition();
        string? factory = definition == typeof(Span<>) ? nameof(CreateWritable)
            : definition == typeof(ReadOnlySpan<>) ? nameof(CreateReadOnly)
            : null;
        var element = type.GetGenericArguments()[0];
        if (factory == null || !element.IsPrimitive)
            return null;

        var method = typeof(PropertyValue).GetMethod(factory, BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(owner, element);
        return (SpanAccessor?)method.Invoke(null, [getter]);
    }

    private static SpanAccessor CreateWritable<TObj, T>(MethodInfo getter) where TObj : PKM where T : unmanaged
    {
        var get = getter.CreateDelegate<SpanGetter<TObj, T>>();
        return new(
            pk => MemoryMarshal.AsBytes(get((TObj)pk)).ToArray(),
            (pk, data) => data.CopyTo(MemoryMarshal.AsBytes(get((TObj)pk))));
    }

    private static SpanAccessor CreateReadOnly<TObj, T>(MethodInfo getter) where TObj : PKM where T : unmanaged
    {
        var get = getter.CreateDelegate<ReadOnlySpanGetter<TObj, T>>();
        return new(pk => MemoryMarshal.AsBytes(get((TObj)pk)).ToArray(), null);
    }
}
