using System;
using System.Runtime.InteropServices;

namespace Cameca.CustomAnalysis.PythonCore;

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
            "f4" => (typeof(float), Marshal.SizeOf(typeof(float))),
            "f8" => (typeof(double), Marshal.SizeOf(typeof(double))),
            "i1" => (typeof(sbyte), Marshal.SizeOf(typeof(sbyte))),
            "i2" => (typeof(short), Marshal.SizeOf(typeof(short))),
            "i4" => (typeof(int), Marshal.SizeOf(typeof(int))),
            "i8" => (typeof(long), Marshal.SizeOf(typeof(long))),
            "u1" => (typeof(byte), Marshal.SizeOf(typeof(byte))),
            "u2" => (typeof(ushort), Marshal.SizeOf(typeof(ushort))),
            "u4" => (typeof(uint), Marshal.SizeOf(typeof(uint))),
            "u8" => (typeof(ulong), Marshal.SizeOf(typeof(ulong))),
            "b1" => (typeof(bool), Marshal.SizeOf(typeof(bool))),
			string when s.StartsWith("S") => (typeof(string), int.Parse(s[1..])),
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
