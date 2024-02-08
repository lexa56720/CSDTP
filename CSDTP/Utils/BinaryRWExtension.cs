namespace CSDTP.Utils
{
    public static class BinaryRWExtension
    {
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
