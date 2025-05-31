#pragma once
#include "PacketHandler.h"
#include "Protocol/Protocol.pb.h"

class FSession;

/*--------------------------------------------------------
				// DomainPacketHandler

- 자동생성된 패킷 핸들러 클래스
- Handle_* 함수 구현
--------------------------------------------------------*/

class DomainPacketHandler : public FPacketHandler
{
public:
	// 자동생성
	// 함수 테이블 등록
	virtual void RegisterHandlers() override
	{
		// Generate RegisterHandler
	}

private:
	// 자동생성
	// Generate Handler
};
