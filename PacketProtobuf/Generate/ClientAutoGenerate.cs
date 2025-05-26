/*--------------------------------------------------------
					ClientAutoGenerate

- Protobuf 자동화 코드, SwarmClient및 DummyClient에 적용
- Template 기반으로 .proto에 해당하는 패킷 자동화 코드 작성
--------------------------------------------------------*/

class ClientAutoGenerate
{
    public static void GenerateUnrealClient(List<string> allPackets, Dictionary<string, List<string>> clientDomains, string outputDir)
    {
    }

    public static void GenerateDummyClient(List<string> allPackets, Dictionary<string, List<string>> clientDomains, string outputDir)
    {
        // (Domain)PacketHandler.h 생성
        GenerateDomainPacketHandler(clientDomains);
    }

    // (Domain)PacketHandler.h 생성
    // 도메인별 핸들러 생성
    // 동적으로 생성되는 파일들이라 .bat에서 XCOPY하지 않고 직접 파일을 건네줌
    private static void GenerateDomainPacketHandler(Dictionary<string, List<string>> domains)
    {
        string dummyClientPath = "..\\..\\..\\SwarmServer\\DummyClient\\Packet";
        string? domainHandlerPath = Common.GetTemplateFilePath(ServiceType.Client, "DomainPacketHandler.h");
        if (domainHandlerPath == null)
        {
            return;
        }
        Common.GenerateDomainCode(domains, domainHandlerPath, dummyClientPath);
    }
}
