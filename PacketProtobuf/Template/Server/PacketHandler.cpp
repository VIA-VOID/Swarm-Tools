#include "pch.h"
#include "PacketHandler.h"

// 자동생성
// 도메인별 핸들러 include
// Generate include Domain

PacketFunc PacketHandler::_handlers[UINT16_MAX];
PacketClass PacketHandler::_domainHandlerClasses;

// 파생 클래스들의 테이블 등록, 초기화
// 자동생성 코드
void PacketHandler::Init()
{
	// Generate Init

	// 도메인별로 함수 테이블 등록
	for (const auto& handler : _domainHandlerClasses)
	{
		handler->RegisterHandlers(_handlers);
	}
}

// 함수 테이블에 등록된 함수 실행 (템플릿 HandlePacket 함수 실행)
void PacketHandler::HandlePacket(Session* session, BYTE* buffer, uint16 len)
{
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
	_handlers[header->id](session, buffer, len);
}