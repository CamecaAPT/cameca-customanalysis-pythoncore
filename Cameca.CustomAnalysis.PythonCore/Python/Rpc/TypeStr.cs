using System;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc;

public abstract record TypeResolver
{
	public sealed record Fixed(string TypeStr) : TypeResolver;
	public sealed record String : TypeResolver
	{
		public string Resolve(long length) => $"|S{length}";
	}
}

public static class TypeStr
{
    // Parse a typestr like "<f4" into a (Type, int itemSize) pair
    public static (Type Type, int ItemSize) Parse(string typestr)
    {
        // strip endian prefix
        var s = typestr.TrimStart('<', '>', '|');
        return s switch
        {
            "f4" => (typeof(float), 4),
            "f8" => (typeof(double), 8),
            "i1" => (typeof(sbyte), 1),
            "i2" => (typeof(short), 2),
            "i4" => (typeof(int), 4),
            "i8" => (typeof(long), 8),
            "u1" => (typeof(byte), 1),
            "u2" => (typeof(ushort), 2),
            "u4" => (typeof(uint), 4),
            "u8" => (typeof(ulong), 8),
            "b1" => (typeof(bool), 1),
            _ => throw new NotSupportedException($"Unknown typestr: {typestr}")
        };
    }

    public static TypeResolver For<T>() => For(typeof(T));

    public static TypeResolver For(Type type) => type switch
    {
        Type t when t == typeof(float) => new TypeResolver.Fixed("<f4"),
        Type t when t == typeof(double) => new TypeResolver.Fixed("<f8"),
        Type t when t == typeof(int) => new TypeResolver.Fixed("<i4"),
        Type t when t == typeof(long) => new TypeResolver.Fixed("<i8"),
        Type t when t == typeof(short) => new TypeResolver.Fixed("<i2"),
        Type t when t == typeof(sbyte) => new TypeResolver.Fixed("|i1"),
        Type t when t == typeof(uint) => new TypeResolver.Fixed("<u4"),
        Type t when t == typeof(ulong) => new TypeResolver.Fixed("<u8"),
        Type t when t == typeof(ushort) => new TypeResolver.Fixed("<u2"),
        Type t when t == typeof(byte) => new TypeResolver.Fixed("|u1"),
        Type t when t == typeof(bool) => new TypeResolver.Fixed("|b1"),
        Type t when t == typeof(string) => new TypeResolver.String(),
        _ => throw new NotSupportedException($"No typestr for {type.Name}")
    };
}
