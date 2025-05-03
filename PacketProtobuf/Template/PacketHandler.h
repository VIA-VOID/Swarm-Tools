#pragma once
#include "Protocol/Protocol.pb.h"

using PacketFunc = std::function<void(Session*, BYTE*, uint16)>;

/*
	패킷구조
	[id][size][protobuf data]
	- id: 프로토콜 ID
	- size: 패킷 전체 크기(헤더 포함)
	- data: 직렬화된 protobuf 데이터
*/
struct PacketHeader
{
	uint16 id;
	uint16 size;
};

enum : uint16
{
	// 자동화 코드
	//GenerateHere Enum
};

/*--------------------------------------------------------
					PacketHandler

- protobuf 적용
- 컨텐츠 로직 함수
- 자동화 도구로 생성된 핸들러들이 여기 추가됨
	- SC: Server에서 Client로 패킷 전달
	- CS: Client에서 Server로 패킷 전달
--------------------------------------------------------*/

class PacketHandler
{
public:
	// 함수 테이블 초기화
	static void Init()
	{
		// 자동화 코드
		//GenerateHere Init
	}
	// 함수 테이블에 등록된 함수 실행 (템플릿 HandlePacket 함수 실행)
	static void HandlePacket(Session* session, BYTE* buffer, uint16 len);
	// 전달받은 RunFunc 함수 실행
	template<typename PacketType, typename RunFunc>
	static void HandlePacket(RunFunc func, Session* session, BYTE* buffer, uint16 len);
	// 패킷 전송
	template<typename T>
	static void SendPacket(Session* session, const T& packet, uint16 packetId);
	// 자동화 코드
	//GenerateHere Handler

private:
	// 함수 테이블
	static PacketFunc _handlers[UINT16_MAX];
};

// 전달받은 RunFunc 함수 실행
template<typename PacketType, typename RunFunc>
inline void PacketHandler::HandlePacket(RunFunc func, Session* session, BYTE* buffer, uint16 len)
{
	PacketType packet;
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
	BYTE* payload = buffer + sizeof(PacketHeader);
	uint16 payloadSize = len - sizeof(PacketHeader);

	if (packet.ParseFromArray(payload, payloadSize) == false)
	{
		LOG_ERROR(L"Packet ParseFromArray 실패: " + typeid(RunFunc).name() + L"payloadSize: ", payloadSize);
	}

	func(session, packet);
}

// 패킷 전송
template<typename T>
inline void PacketHandler::SendPacket(Session* session, const T& packet, uint16 packetId)
{
	const uint16 payloadSize = static_cast<uint16>(packet.ByteSizeLong());
	const uint16 totalSize = sizeof(PacketHeader) + payloadSize;

	// sendBuffer 헤더 세팅
	SendBufferRef sendBuffer = ObjectPool<SendBuffer>::MakeShared(totalSize + 1);
	PacketHeader* header = reinterpret_cast<PacketHeader*>(sendBuffer->GetWritePtr());
	header->size = totalSize;
	header->id = packetId;

	// 데이터 세팅
	BYTE* payload = sendBuffer->GetWritePtr() + sizeof(PacketHeader);
	packet.SerializeToArray(payload, payloadSize);

	// 데이터 전송
	sendBuffer->MoveWritePos(totalSize);
	session->Send(sendBuffer->GetReadPtr(), totalSize);
}
