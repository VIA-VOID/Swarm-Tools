using System;
using System.Collections.Generic;
using Google.Protobuf;
//using UnityEngine;

/*-------------------------------------------------------
				PacketHandler

- 자동생성 파일 (수정 X)
- 컨텐츠 로직등에서 PacketHandler를 상속받아, 로직 구현부 구현
--------------------------------------------------------*/
public class PacketHandler
{
    //GenerateHere methods

    // 패킷 핸들러 및 팩토리 등록
    public static void RegisterPacketHandlers(PacketManager packetManager)
    {
        // 패킷 팩토리 등록
        packetManager.RegisterPacketFactories();

        // 패킷 타입 등록
        //GenerateHere packetType

        // 패킷 핸들러 등록
        //GenerateHere register
    }
}
