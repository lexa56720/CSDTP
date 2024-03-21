namespace AutoSerializer
{
    public static class BinaryRWExtensions
    {
        private static Dictionary<Type, Func<BinaryReader, object>> ReadMethods = new()
        {
            { typeof(bool),(BinaryReader r)=>r.ReadBoolean() },
            { typeof(short),(BinaryReader r)=>r.ReadInt16() },
            { typeof(int),(BinaryReader r)=>r.ReadInt32() },
            { typeof(float),(BinaryReader r)=>r.ReadSingle() },
            { typeof(double),(BinaryReader r)=>r.ReadDouble() },
            { typeof(string),(BinaryReader r)=>r.ReadString() },
            { typeof(byte), (BinaryReader r) => r.ReadByte() },
            { typeof(sbyte), (BinaryReader r) => r.ReadSByte() },
            { typeof(char), (BinaryReader r) => r.ReadChar() },
            { typeof(decimal), (BinaryReader r) => r.ReadDecimal() },
            { typeof(long), (BinaryReader r) => r.ReadInt64() },
            { typeof(ulong), (BinaryReader r) => r.ReadUInt64() },
            { typeof(uint), (BinaryReader r) => r.ReadUInt32() },
            { typeof(ushort), (BinaryReader r) => r.ReadUInt16() },

            { typeof(int[]),(BinaryReader r)=>r.ReadInt32Array() },
            { typeof(long[]), (BinaryReader r) => r.ReadInt64Array() },
            { typeof(byte[]), (BinaryReader r) => r.ReadByteArray() },
            { typeof(DateTime), (BinaryReader r) => r.ReadTime() },
        };
        private static Dictionary<Type, Action<BinaryWriter, object>> WriteMethods = new()
        {
            { typeof(short),(w,o)=>w.Write((short)o) },
            { typeof(bool),(w,o)=>w.Write((bool)o) },
            { typeof(int),(w,o)=>w.Write((int)o) },
            { typeof(float),(w,o)=>w.Write((float)o) },
            { typeof(double),(w,o)=>w.Write((double)o) },
            { typeof(string),(w,o)=>w.Write((string)o) },
        };
        public static void Write(this BinaryWriter writer, object? obj, Type type)
        {
            WriteMethods[type](writer, obj);
        }
        public static object? Read(this BinaryReader reader, Type type)
        {
            return ReadMethods[type](reader);
        }


        public static void Write(this BinaryWriter writer, DateTime time)
        {
            writer.Write(time.ToBinary());
        }
        public static DateTime ReadTime(this BinaryReader reader)
        {
            return DateTime.FromBinary(reader.ReadInt64());
        }

        public static void Write(this BinaryWriter writer, int[] array)
        {
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                writer.Write(array[i]);
        }
        public static int[] ReadInt32Array(this BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var array = new int[length];
            for (int i = 0; i < length; i++)
                array[i] = reader.ReadInt32();
            return array;
        }

        public static void Write(this BinaryWriter writer, long[] array)
        {
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                writer.Write(array[i]);
        }
        public static long[] ReadInt64Array(this BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var array = new long[length];
            for (int i = 0; i < length; i++)
                array[i] = reader.ReadInt64();
            return array;
        }

        public static void WriteBytes(this BinaryWriter writer, byte[] array)
        {
            writer.Write(array.Length);
            writer.Write(array);
        }
        public static byte[] ReadByteArray(this BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var array = reader.ReadBytes(length);
            return array;
        }


        public static void Write<T>(this BinaryWriter writer, T[] array) where T : ISerializable<T>, new()
        {
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                array[i].Serialize(writer);
        }
        public static T[] Read<T>(this BinaryReader reader) where T : ISerializable<T>, new()
        {
            var length = reader.ReadInt32();
            var array = new T[length];
            for (int i = 0; i < length; i++)
                array[i] = T.Deserialize(reader);
            return array;
        }
    }
}
