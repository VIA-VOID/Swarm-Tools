using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Google.Protobuf;
//using UnityEngine;

/*-------------------------------------------------------
				PacketManager

- protobuf 적용
- 컨텐츠 로직 함수
- 자동화 도구로 생성된 핸들러들이 여기 추가됨
	- SC: Server에서 Client로 패킷 전달
	- CS: Client에서 Server로 패킷 전달
--------------------------------------------------------*/
public class PacketManager
{
    // 패킷 ID에 따른 핸들러 매핑
    private Dictionary<ushort, Action<IMessage>> _handlers = new Dictionary<ushort, Action<IMessage>>();
    // 패킷 생성 딕셔너리 - 패킷 ID에 따라 패킷 객체 생성
    private Dictionary<ushort, Func<IMessage>> _packetFactories = new Dictionary<ushort, Func<IMessage>>();
    // 타입에 따른 패킷 ID 매핑
    private Dictionary<Type, ushort> _typeToId = new Dictionary<Type, ushort>();

    // 패킷 핸들러 등록
    public void Register(ushort packetId, Action<IMessage> handler)
    {
        _handlers[packetId] = handler;
    }

    // 패킷 생성기 등록
    public void RegisterFactory(ushort packetId, Func<IMessage> factory)
    {
        _packetFactories[packetId] = factory;
    }

    // 패킷 타입 등록
    // 타입으로부터 ID 찾기
    public void RegisterType<T>(ushort packetId) where T : IMessage
    {
        _typeToId[typeof(T)] = packetId;
    }

    // 패킷 팩토리 메서드들 등록 (자동 생성)
    public void RegisterPacketFactories()
    {
        //GenerateHere PacketFactory
    }

    // 수신한 패킷 처리
    public void OnRecvPacket(ArraySegment<byte> buffer)
    {
        int offset = buffer.Offset;

        // 패킷 ID 추출
        ushort packetId = BitConverter.ToUInt16(buffer.Array, offset);
        offset += sizeof(ushort);

        // 패킷 크기 추출
        ushort packetSize = BitConverter.ToUInt16(buffer.Array, offset);
        offset += sizeof(ushort);

        // 패킷 생성
        if (_packetFactories.TryGetValue(packetId, out var factory) == false)
        {
            //Debug.LogError($"패킷 생성기를 찾을 수 없습니다: ID={packetId}");
            return;
        }

        IMessage packet = factory.Invoke();
        if (packet == null)
        {
            //Debug.LogError($"패킷 생성 실패: ID={packetId}");
            return;
        }

        // 패킷 파싱
        ArraySegment<byte> dataSegment = new ArraySegment<byte>(
            buffer.Array,
            buffer.Offset + PacketHeader.HeaderSize,
            packetSize - PacketHeader.HeaderSize
        );

        // Protobuf 데이터 파싱
        packet.MergeFrom(dataSegment.Array, dataSegment.Offset, dataSegment.Count);

        // 핸들러 호출
        if (_handlers.TryGetValue(packetId, out var handler))
        {
            handler.Invoke(packet);
        }
        else
        {
            //Debug.LogError($"패킷 핸들러를 찾을 수 없습니다: ID={packetId}");
        }
    }

    // 패킷 타입으로부터 ID 찾기
    private ushort GetPacketId<T>() where T : IMessage
    {
        if (_typeToId.TryGetValue(typeof(T), out ushort id))
        {
            return id;
        }

        //Debug.LogError($"패킷 ID를 찾을 수 없습니다: Type={typeof(T).Name}");
        return 0;
    }

    // 제네릭 타입 패킷 생성 및 처리
    public void MakePacket<T>(ArraySegment<byte> buffer) where T : IMessage, new()
    {
        T packet = new T();

        int offset = buffer.Offset;

        // 패킷 ID 추출
        ushort id = BitConverter.ToUInt16(buffer.Array, offset);
        offset += sizeof(ushort);

        // 패킷 크기 추출
        ushort size = BitConverter.ToUInt16(buffer.Array, offset);
        offset += sizeof(ushort);

        // 패킷 파싱
        ArraySegment<byte> segment = new ArraySegment<byte>(
            buffer.Array,
            buffer.Offset + PacketHeader.HeaderSize,
            size - PacketHeader.HeaderSize
        );

        packet.MergeFrom(segment.Array, segment.Offset, segment.Count);

        // 핸들러 호출
        if (_handlers.TryGetValue(id, out var action))
        {
            action.Invoke(packet);
        }
    }

    // 전송할 패킷 버퍼 생성
    public ArraySegment<byte> MakeSendBuffer<T>(T packet) where T : IMessage
    {
        int payloadSize = packet.CalculateSize();
        ushort packetId = GetPacketId<T>();
        ushort totalSize = (ushort)(payloadSize + PacketHeader.HeaderSize);

        byte[] buffer = new byte[totalSize];
        int offset = 0;

        // 패킷 ID
        Array.Copy(BitConverter.GetBytes(packetId), 0, buffer, offset, sizeof(ushort));
        offset += sizeof(ushort);

        // 헤더 작성
        BitConverter.GetBytes(packetId).CopyTo(buffer, offset);
        offset += sizeof(ushort);
        BitConverter.GetBytes(totalSize).CopyTo(buffer, offset);
        offset += sizeof(ushort);

        // 본문 직렬화
        using (MemoryStream ms = new MemoryStream(buffer, offset, payloadSize, writable: true))
        {
            CodedOutputStream cos = new CodedOutputStream(ms);
            packet.WriteTo(cos);
            cos.Flush();
        }

        return new ArraySegment<byte>(buffer, 0, totalSize);
    }
}