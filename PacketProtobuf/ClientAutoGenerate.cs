using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/*--------------------------------------------------------
					ClientAutoGenerate

- Protobuf 자동화 코드, SwarmClient(Unity)에 적용
- Template 기반으로 .proto에 해당하는 패킷 자동화 코드 작성
--------------------------------------------------------*/
public class ClientAutoGenerate
{
    public static void GenerateClient(string[] args)
    {
        string protoPath = args.Length > 0 ? args[0] : "./";
        string[] protoFiles = Directory.GetFiles(protoPath, "*.proto");

        // 템플릿 파일 경로
        string handlerTemplatePath = Path.Combine("..", "Template", "Client", "PacketHandler.cs");
        string headerTemplatePath = Path.Combine("..", "Template", "Client", "PacketHeader.cs");
        string systemTemplatePath = Path.Combine("..", "Template", "Client", "PacketSystem.cs");

        // 템플릿 파일 존재 확인
        if (File.Exists(handlerTemplatePath) == false ||
            File.Exists(systemTemplatePath) == false ||
            File.Exists(headerTemplatePath) == false)
        {
            Console.WriteLine("[ERROR] 템플릿 파일 없음");
            return;
        }

        List<string> allPackets = new List<string>();
        foreach (string file in protoFiles)
        {
            foreach (string line in File.ReadLines(file))
            {
                Match match = Regex.Match(line, @"message\s+(CS_\w+|SC_\w+)");
                if (match.Success)
                {
                    allPackets.Add(match.Groups[1].Value);
                }
            }
        }

        allPackets.Sort();
        List<string> clientPackets = allPackets.Where(p => p.StartsWith("SC_")).OrderBy(p => p).ToList();
        Dictionary<string, List<string>> clientDomains = new Dictionary<string, List<string>>();

        // 도메인별로 저장
        SaveDomains(clientPackets, clientDomains);

        // 코드 자동생성
        // PacketHeader.cs
        GeneratePacketHeader.Generate(allPackets, headerTemplatePath, protoPath);
        // PacketHandler.cs
        GeneratePacketHandler.Generate(clientDomains, handlerTemplatePath, protoPath);
        // PacketSystem.cs
        GeneratePacketSystem.Generate(clientDomains, systemTemplatePath, protoPath);
    }

    private static void SaveDomains(List<string> packets, Dictionary<string, List<string>> domains)
    {
        // 도메인별로 저장
        foreach (string name in packets)
        {
            string clsName = name.Split('_')[1];
            string key = char.ToUpper(clsName[0]) + clsName.Substring(1).ToLower();

            if (domains.ContainsKey(key) == false)
            {
                domains[key] = new List<string>();
            }
            domains[key].Add(name);
        }
    }

}
