using System;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc;

public static class TypeStr
{
    public static Type Parse(string typestr)
    {
        // strip endian prefix
        var s = typestr.TrimStart('<', '>', '|');
        return s switch
        {
            "f4" => typeof(float),
            "f8" => typeof(double),
            "i1" => typeof(sbyte),
            "i2" => typeof(short),
            "i4" => typeof(int),
            "i8" => typeof(long),
            "u1" => typeof(byte),
            "u2" => typeof(ushort),
            "u4" => typeof(uint),
            "u8" => typeof(ulong),
            "b1" => typeof(bool),
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
