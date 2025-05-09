using System.Text.RegularExpressions;

/*--------------------------------------------------------
					ServerAutoGenerate

- Protobuf 자동화 코드, SwarmServer에 적용
- Template 기반으로 .proto에 해당하는 패킷 자동화 코드 작성
--------------------------------------------------------*/
namespace PacketProtobuf
{
    internal class ServerAutoGenerate
    {
        public static void GenerateServer(string[] args)
        {
            string protoPath = args.Length > 0 ? args[0] : "./";
            string[] protoFiles = Directory.GetFiles(protoPath, "*.proto");
            string templatePath = Path.Combine("..", "Template", "Server", "PacketHandler.h");

            if (File.Exists(templatePath) == false)
            {
                Console.WriteLine("[ERROR] 템플릿 파일 없음: " + templatePath);
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

            List<string> serverPackets = allPackets.Where(p => p.StartsWith("SC_")).OrderBy(p => p).ToList();
            List<string> clientPackets = allPackets.Where(p => p.StartsWith("CS_")).OrderBy(p => p).ToList();

            GenerateHandlerFile("ServerPacketHandler", serverPackets, templatePath, protoPath);
            GenerateHandlerFile("ClientPacketHandler", clientPackets, templatePath, protoPath);
        }

        private static void GenerateHandlerFile(string className, List<string> packets, string templatePath, string outputDir)
        {
            string[] lines = File.ReadAllLines(templatePath);
            List<string> output = new List<string>();
            int baseId = 0;

            foreach (string line in lines)
            {
                if (line.Contains("PacketHandler"))
                {
                    output.Add(line.Replace("PacketHandler", className));
                }
                else if (line.Contains("//GenerateHere Enum"))
                {
                    foreach (string name in packets)
                    {
                        output.Add($"\t{name} = {baseId++},");
                    }
                }
                else if (line.Contains("//GenerateHere Init"))
                {
                    foreach (string name in packets)
                    {
                        output.Add($"\t\t_handlers[{name}] = [](Session* session, BYTE* buffer, int32 len)");
                        output.Add("\t\t\t{");
                        output.Add($"\t\t\t\t{className}::HandlePacket<Protocol::{name}>(Handle_{name}, session, buffer, len);");
                        output.Add("\t\t\t};");
                    }
                }
                else if (line.Contains("//GenerateHere Handler"))
                {
                    foreach (string name in packets)
                    {
                        output.Add($"\tstatic void Handle_{name}(Session* session, Protocol::{name}& packet);");
                    }
                }
                else
                {
                    output.Add(line);
                }
            }

            string outPath = Path.Combine(outputDir, className + ".h");
            File.WriteAllLines(outPath, output);
            Console.WriteLine($"[SUCCESS] {className}.h 생성 완료");
        }
    }
}
