/*--------------------------------------------------------
					ServerAutoGenerate

- Protobuf 자동화 코드, SwarmServer에 적용
- Template 기반으로 .proto에 해당하는 패킷 자동화 코드 작성
--------------------------------------------------------*/

class ServerAutoGenerate
{
    public static void GenerateServer(List<string> allPackets, Dictionary<string, List<string>> serverDomains, string outputDir)
    {
        // enum PacketID 멤버
        GeneratePacketId(allPackets, outputDir);
        // (Domain)PacketHandler.h 생성
        GenerateDomainPacketHandler(serverDomains);
        // PacketHandler::Init() 함수 구현
        Common.GeneratePacketHandlerInit(ServiceType.Server, serverDomains, outputDir);
    }

    // PacketID 프로토콜 자동화
    private static void GeneratePacketId(List<string> packets, string outputDir)
    {
        string? packetIdPath = Common.GetTemplateFilePath(ServiceType.Server, "PacketId.h");
        if (packetIdPath == null)
        {
            return;
        }
        
        int baseId = 0;
        string[] lines = File.ReadAllLines(packetIdPath);
        List<string> output = new List<string>();
        
        foreach (string line in lines)
        {
            if (line.Contains("// Generate PacketID"))
            {
                foreach (string name in packets)
                {
                    output.Add($"\t{name} = {baseId++},");
                }
            }
            else
            {
                output.Add(line);
            }
        }

        Common.WriteFile(output, outputDir, "PacketId.h");
    }

    // (Domain)PacketHandler.h 생성
    // 도메인별 핸들러 생성
    // 동적으로 생성되는 파일들이라 .bat에서 COPY하지 않고 직접 파일을 건네줌
    private static void GenerateDomainPacketHandler(Dictionary<string, List<string>> domains)
    {
        string serverPath = "..\\..\\..\\SwarmServer\\GameServer\\Packet";
        string? domainHandlerPath = Common.GetTemplateFilePath(ServiceType.Server, "DomainPacketHandler.h");
        if (domainHandlerPath == null)
        {
            return;
        }
        Common.GenerateDomainCode(domains, domainHandlerPath, serverPath);
    }
}
