namespace CSDTP
{

    public interface ISerializable<T> where T : new()
    {
        public void Serialize(BinaryWriter writer);

        public static abstract T Deserialize(BinaryReader reader);
    }
}
