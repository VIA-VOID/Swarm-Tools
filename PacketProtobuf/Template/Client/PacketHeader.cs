public enum PacketId
{
    //GenerateHere packetId
}

public struct PacketHeader
{
    // 패킷 식별자
    public ushort Id;
    // 헤더를 포함한 전체 패킷 크기
    public ushort Size;
    public static readonly int HeaderSize = sizeof(ushort) + sizeof(ushort);
}