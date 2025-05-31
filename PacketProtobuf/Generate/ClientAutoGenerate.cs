/*--------------------------------------------------------
					ClientAutoGenerate

- Protobuf 자동화 코드, SwarmClient및 DummyClient에 적용
- Template 기반으로 .proto에 해당하는 패킷 자동화 코드 작성
--------------------------------------------------------*/

class ClientAutoGenerate
{
    public static void GenerateUnrealClient(List<string> allPackets, Dictionary<string, List<string>> clientDomains, string outputDir)
    {
        // enum EPacketID 멤버
        GenerateUnrealPacketId(allPackets, outputDir);
        // (Domain)PacketHandler.h 생성
        GenerateUnrealDomainCode(clientDomains);
        // PacketHandler::Init() 함수 구현
        GenerateUnrealPacketHandlerInit(clientDomains, outputDir);
    }

    public static void GenerateDummyClient(List<string> allPackets, Dictionary<string, List<string>> clientDomains, string outputDir)
    {
        // (Domain)PacketHandler.h 생성
        GenerateDomainPacketHandler(clientDomains);
    }

    // (Domain)PacketHandler.h 생성
    // 도메인별 핸들러 생성
    // 동적으로 생성되는 파일들이라 .bat에서 COPY하지 않고 직접 파일을 건네줌
    private static void GenerateDomainPacketHandler(Dictionary<string, List<string>> domains)
    {
        string dummyClientPath = "..\\..\\..\\SwarmServer\\DummyClient\\Packet";
        string? domainHandlerPath = Common.GetTemplateFilePath(ServiceType.Dummy, "DomainPacketHandler.h");
        if (domainHandlerPath == null)
        {
            return;
        }
        Common.GenerateDomainCode(domains, domainHandlerPath, dummyClientPath);
    }

    // PacketID 프로토콜 자동화
    private static void GenerateUnrealPacketId(List<string> packets, string outputDir)
    {
        string? packetIdPath = Common.GetTemplateFilePath(ServiceType.Unreal, "PacketId.h");
        if (packetIdPath == null)
        {
            return;
        }

        int baseId = 0;
        string[] lines = File.ReadAllLines(packetIdPath);
        List<string> output = new List<string>();

        foreach (string line in lines)
        {
            if (line.Contains("// Generate EPacketID"))
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

        Common.WriteFile(output, outputDir, "UE_PacketId.h");
    }

    // (Domain)PacketHandler.h 생성
    // 도메인별 핸들러 생성
    // 동적으로 생성되는 파일들이라 .bat에서 COPY하지 않고 직접 파일을 건네줌
    public static void GenerateUnrealDomainCode(Dictionary<string, List<string>> domains)
    {
        string clientPath = "..\\..\\..\\SwarmClient\\Source\\SwarmClient\\Packet";
        string? domainHandlerPath = Common.GetTemplateFilePath(ServiceType.Unreal, "DomainPacketHandler.h");
        if (domainHandlerPath == null)
        {
            return;
        }

        string[] lines = File.ReadAllLines(domainHandlerPath);

        foreach (var pair in domains)
        {
            string domain = pair.Key;
            List<string> protocols = pair.Value;
            List<string> output = new List<string>();

            foreach (string line in lines)
            {
                if (line.Contains("// DomainPacketHandler"))
                {
                    output.Add($"\t\t\t\tF{domain}PacketHandler");
                }
                else if (line.Contains("DomainPacketHandler"))
                {
                    output.Add($"class F{domain}PacketHandler : public FPacketHandler");
                }
                else if (line.Contains("// Generate RegisterHandler"))
                {
                    foreach (string name in protocols)
                    {
                        output.Add($"\t\tRegisterPacket<Protocol::{name}>(EPacketID::{name}, Handle_{name});");
                    }

                }
                else if (line.Contains("// Generate Handler"))
                {
                    foreach (string name in protocols)
                    {
                        output.Add($"\tstatic void Handle_{name}(FSession* Session, Protocol::{name}& Packet);");
                    }
                }
                else
                {
                    output.Add(line);
                }
            }

            Common.FullPathWriteFile(output, clientPath, $"{domain}PacketHandler.h");
        }
    }

    // PacketHandler::Init() 자동화
    private static void GenerateUnrealPacketHandlerInit(Dictionary<string, List<string>> domains, string outputDir)
    {
        string? packetHandlerPath = Common.GetTemplateFilePath(ServiceType.Unreal, "PacketHandler.cpp");
        if (packetHandlerPath == null)
        {
            return;
        }
        string[] lines = File.ReadAllLines(packetHandlerPath);
        List<string> output = new List<string>();

        foreach (string line in lines)
        {
            if (line.Contains("// Generate include Domain"))
            {
                foreach (var pair in domains)
                {
                    string domain = pair.Key;
                    output.Add($"#include \"{domain}PacketHandler.h\"");
                }
            }
            else if (line.Contains("// Generate Init"))
            {
                foreach (var pair in domains)
                {
                    string domain = pair.Key;
                    output.Add($"\tDomainHandlerClasses.Add(MakeUnique<F{domain}PacketHandler>());");
                }
            }
            else
            {
                output.Add(line);
            }
        }

        Common.WriteFile(output, outputDir, "UE_PacketHandler.cpp");
    }
}
