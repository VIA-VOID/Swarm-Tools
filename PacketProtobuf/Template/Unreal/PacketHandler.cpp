#include "PacketHandler.h"

// 자동생성
// 도메인별 핸들러 include
// Generate include Domain

FPacketFunc FPacketHandler::Handlers[UINT16_MAX];
FPacketClass FPacketHandler::DomainHandlerClasses;

// 파생 클래스들의 테이블 등록, 초기화
// 자동생성 코드
void FPacketHandler::Init()
{
	// Generate Init

	// 도메인별로 함수 테이블 등록
	for (const auto& Handler : DomainHandlerClasses)
	{
		Handler->RegisterHandlers();
	}
}

// 함수 테이블에 등록된 함수 실행 (템플릿 HandlePacket 함수 실행)
void FPacketHandler::HandlePacket(FSession* Session, BYTE* Buffer, uint16 Len)
{
	const FPacketHeader* Header = reinterpret_cast<FPacketHeader*>(Buffer);
	Handlers[Header->PacketId](Session, Buffer, Len);
}
