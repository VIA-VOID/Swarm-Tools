using System.Text.RegularExpressions;

/*--------------------------------------------------------
					Program

- Protobuf 서버/클라이언트 자동화 코드
- Template/*.h 기반으로 .proto에 해당하는
  패킷 자동화 코드 작성
--------------------------------------------------------*/

public class Program
{
    static void Main(string[] args)
    {
        string protoPath = args.Length > 0 ? args[0] : "./";
        string[] protoFiles = Directory.GetFiles(protoPath, "*.proto");
        
        // .proto의 모든 프로토콜 가져오기
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

        List<string> clientPackets = allPackets.Where(p => p.StartsWith("SC_")).ToList();
        List<string> serverPackets = allPackets.Where(p => p.StartsWith("CS_")).ToList();
        List<string> sortedPackets = clientPackets.Concat(serverPackets).ToList();

        // 도메인별로 저장
        Dictionary<string, List<string>> clientDomains = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> serverDomains = new Dictionary<string, List<string>>();
        
        Common.SaveDomains(clientPackets, clientDomains);
        Common.SaveDomains(serverPackets, serverDomains);

        // 클라이언트 자동화 코드 실행
        ClientAutoGenerate.GenerateDummyClient(sortedPackets, clientDomains, protoPath);
        ClientAutoGenerate.GenerateUnrealClient(sortedPackets, clientDomains, protoPath);
        // 서버 자동화 코드 실행
        ServerAutoGenerate.GenerateServer(sortedPackets, serverDomains, protoPath);
    }
}
