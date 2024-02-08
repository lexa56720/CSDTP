namespace CSDTP.Packets
{

    public interface IPacket : IPacketInfo
    {
        public Type TypeOfPacket { get; }

        public object? DataObj { get; }


        public void SerializePacket(BinaryWriter writer);
        public void SerializeUnprotectedCustomData(BinaryWriter writer);
        public void SerializeProtectedCustomData(BinaryWriter writer);


        public void DeserializePacket(BinaryReader reader);
        public void DeserializeUnprotectedCustomData(BinaryReader writer);
        public void DeserializeProtectedCustomData(BinaryReader writer);

    }
}
