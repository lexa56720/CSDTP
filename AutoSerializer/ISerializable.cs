namespace AutoSerializer
{
    public interface ISerializable<T> where T : new()
    {
        public void Serialize(BinaryWriter writer)
        {
            Serializer.Serialize<T>(this, writer);
        }

        public static virtual T Deserialize(BinaryReader reader)
        {
            return (T)Serializer.Deserialize<T>(reader);
        }
    }

}
