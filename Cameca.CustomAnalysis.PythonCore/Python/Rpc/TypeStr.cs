using System;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc;

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

    public static string For<T>() => For(typeof(T));

    public static string For(Type type) => type switch
    {
        Type t when t == typeof(float) => "<f4",
        Type t when t == typeof(double) => "<f8",
        Type t when t == typeof(int) => "<i4",
        Type t when t == typeof(long) => "<i8",
        Type t when t == typeof(short) => "<i2",
        Type t when t == typeof(sbyte) => "|i1",
        Type t when t == typeof(uint) => "<u4",
        Type t when t == typeof(ulong) => "<u8",
        Type t when t == typeof(ushort) => "<u2",
        Type t when t == typeof(byte) => "|u1",
        Type t when t == typeof(bool) => "|b1",
        _ => throw new NotSupportedException($"No typestr for {type.Name}")
    };
}
