using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public static class BinaryWriterExtensions
{
    public static byte[] Serialize(IBinarySerializable serializable)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            serializable.Serialize(writer);
            return ms.ToArray();
        }
    }

    public static T Deserialize<T>(byte[] byteArr) where T : IBinarySerializable, new()
    {
        using (MemoryStream ms = new MemoryStream(byteArr))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            T item = new T();
            item.Deserialize(reader);
            return item;
        }
    }

    public static void Write(this BinaryWriter writer, byte[] byteArr, bool a)
    {
        if (byteArr == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(byteArr.Length);
        writer.Write(byteArr);
    }

    public static void Write(this BinaryWriter writer, IEnumerable<byte> collection)
    {
        if (collection == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(collection.Count());
        foreach (var item in collection)
            writer.Write(item);
    }

    public static void Write(this BinaryWriter writer, IEnumerable<int> collection)
    {
        if (collection == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(collection.Count());
        foreach (var item in collection)
            writer.Write(item);
    }

    public static void Write(this BinaryWriter writer, IEnumerable<string> collection)
    {
        if (collection == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(collection.Count());
        foreach (var item in collection)
            writer.Write(item);
    }

    public static void Write(this BinaryWriter writer, IEnumerable<Vector2> collection)
    {
        if (collection == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(collection.Count());
        foreach (var item in collection)
            writer.Write(item);
    }

    public static void Write(this BinaryWriter writer, IEnumerable<Vector3> collection)
    {
        if (collection == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(collection.Count());
        foreach (var item in collection)
            writer.Write(item);
    }

    public static void Write<T>(this BinaryWriter writer, IEnumerable<T> collection) where T : IBinarySerializable, new()
    {
        if (collection == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(collection.Count());
        foreach (var item in collection)
            item.Serialize(writer);
    }

    public static void Write<T>(this BinaryWriter writer, IDictionary<int, T> collection) where T : IBinarySerializable, new()
    {
        if (collection == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(collection.Count());
        foreach (var pair in collection)
        {
            writer.Write(pair.Key);
            pair.Value.Serialize(writer);
        }
    }

    public static void Write(this BinaryWriter writer, Vector2 value)
    {
        writer.Write(value.x);
        writer.Write(value.y);
    }

    public static void Write(this BinaryWriter writer, Vector3 value)
    {
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
    }

    public static void Write(this BinaryWriter writer, Vector4 value)
    {
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
        writer.Write(value.w);
    }

    public static void Write(this BinaryWriter writer, Quaternion value)
    {
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
        writer.Write(value.w);
    }


    public static byte[] ReadByteArray(this BinaryReader reader)
    {
        int length = reader.ReadInt32();
        return reader.ReadBytes(length);
    }

    public static NativeArray<byte> ReadNativeByteArray(this BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length == 0) return default;
        return new NativeArray<byte>(reader.ReadBytes(length), Allocator.Temp);
    }

    public static List<int> ReadInt32List(this BinaryReader reader)
    {
        int length = reader.ReadInt32();
        var list = new List<int>(length);
        for (int i = 0; i < length; i++)
            list.Add(reader.ReadInt32());
        return list;
    }

    public static List<string> ReadStringList(this BinaryReader reader)
    {
        int length = reader.ReadInt32();
        var list = new List<string>(length);
        for (int i = 0; i < length; i++)
            list.Add(reader.ReadString());
        return list;
    }

    public static int[] ReadInt32Array(this BinaryReader reader)
    {
        int length = reader.ReadInt32();
        var arr = new int[length];
        for (int i = 0; i < length; i++)
            arr[i] = reader.ReadInt32();
        return arr;
    }

    public static Vector2[] ReadVector2Array(this BinaryReader reader)
    {
        int length = reader.ReadInt32();
        var arr = new Vector2[length];
        for (int i = 0; i < length; i++)
            arr[i] = reader.ReadVector2();
        return arr;
    }

    public static Vector3[] ReadVector3Array(this BinaryReader reader)
    {
        int length = reader.ReadInt32();
        var arr = new Vector3[length];
        for (int i = 0; i < length; i++)
            arr[i] = reader.ReadVector3();
        return arr;
    }

    public static List<T> ReadSerializableList<T>(this BinaryReader reader) where T : IBinarySerializable, new ()
    {
        int length = reader.ReadInt32();
        var list = new List<T>(length);
        for (int i = 0; i < length; i++)
        {
            var item = new T();
            list.Add(item);
            item.Deserialize(reader);
        }
        return list;
    }

    public static Dictionary<int, T> ReadSerializableDict<T>(this BinaryReader reader) where T : IBinarySerializable, new()
    {
        int length = reader.ReadInt32();
        var dict = new Dictionary<int, T>(length);
        for (int i = 0; i < length; i++)
        {
            var item = new T();
            dict.Add(reader.ReadInt32(), item);
            item.Deserialize(reader);
        }
        return dict;
    }

    public static Vector2 ReadVector2(this BinaryReader reader)
    {
        return new Vector2(reader.ReadSingle(), reader.ReadSingle());
    }

    public static Vector3 ReadVector3(this BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public static Vector4 ReadVector4(this BinaryReader reader)
    {
        return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public static Quaternion ReadQuaternion(this BinaryReader reader)
    {
        return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}

public interface IBinarySerializable
{
    void Serialize(BinaryWriter writer);
    void Deserialize(BinaryReader reader);
}