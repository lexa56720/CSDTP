namespace AutoSerializer
{
    public interface ISerializer
    {
        public void SerializePartial<T>(ISerializable<T> obj, BinaryWriter writer) where T : new();
        public void DeserializePartial(BinaryReader reader, ref object result);
    }
    public static class Serializer
    {
        public static ISerializer? SerializerProvider { get; set; }

        public static void Serialize<T>(ISerializable<T> obj, BinaryWriter writer) where T : new()
        {
            if (SerializerProvider != null)
                SerializerProvider.SerializePartial(obj, writer);
        }

        public static object Deserialize<T>(BinaryReader reader) where T : new()
        {
            var result = (object)new T();
            if (SerializerProvider != null)
                SerializerProvider.DeserializePartial(reader, ref result);

            return result;
        }
    }
}
