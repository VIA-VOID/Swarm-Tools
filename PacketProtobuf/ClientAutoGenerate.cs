using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

/*--------------------------------------------------------
					ClientAutoGenerate

- Protobuf 자동화 코드, SwarmClient(Unity)에 적용
- Template 기반으로 .proto에 해당하는 패킷 자동화 코드 작성
--------------------------------------------------------*/
namespace PacketProtobuf
{
    internal class ClientAutoGenerate
    {
        public static void GenerateClient(string[] args)
        {
            string protoPath = args.Length > 0 ? args[0] : "./";
            string[] protoFiles = Directory.GetFiles(protoPath, "*.proto");

            // 템플릿 파일 경로
            string handlerTemplatePath = Path.Combine("..", "Template", "Client", "PacketHandler.cs");
            string managerTemplatePath = Path.Combine("..", "Template", "Client", "PacketManager.cs");
            string headerTemplatePath = Path.Combine("..", "Template", "Client", "PacketHeader.cs");

            // 템플릿 파일 존재 확인
            if (File.Exists(handlerTemplatePath) == false ||
                File.Exists(managerTemplatePath) == false ||
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

            List<string> serverPackets = allPackets.Where(p => p.StartsWith("SC_")).OrderBy(p => p).ToList();
            List<string> clientPackets = allPackets.Where(p => p.StartsWith("CS_")).OrderBy(p => p).ToList();

            GeneratePacketHeader(allPackets, headerTemplatePath, protoPath);
            GeneratePacketManager(allPackets, managerTemplatePath, protoPath);
            GeneratePacketHandler(serverPackets, handlerTemplatePath, protoPath);
        }

        private static void GeneratePacketHeader(List<string> packets, string templatePath, string outputDir)
        {
            string[] lines = File.ReadAllLines(templatePath);
            List<string> output = new List<string>();
            int baseId = 0;

            foreach (string line in lines)
            {
                if (line.Contains("//GenerateHere packetId"))
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

            string outPath = Path.Combine(outputDir, "PacketHeader.cs");
            File.WriteAllLines(outPath, output);
            Console.WriteLine("[SUCCESS] PacketHeader.cs 생성 완료");
        }

        private static void GeneratePacketManager(List<string> packets, string templatePath, string outputDir)
        {
            string[] lines = File.ReadAllLines(templatePath);
            List<string> output = new List<string>();

            foreach (string line in lines)
            {
                if (line.Contains("//using UnityEngine"))
                {
                    output.Add(line.Replace("//using", "using"));
                }
                else if (line.Contains("//GenerateHere PacketFactory"))
                {
                    foreach (string name in packets)
                    {
                        output.Add($"\t\tRegisterFactory(PacketId.{name}, () => new Google.Protobuf.Protocol.{name}());");
                    }
                }
                else if (line.Contains("//Debug"))
                {
                    output.Add(line.Replace("//Debug", "Debug"));
                }
                else
                {
                    output.Add(line);
                }
            }

            string outPath = Path.Combine(outputDir, "PacketManager.cs");
            File.WriteAllLines(outPath, output);
            Console.WriteLine("[SUCCESS] PacketManager.cs 생성 완료");
        }

        private static void GeneratePacketHandler(List<string> packets, string templatePath, string outputDir)
        {
            string[] lines = File.ReadAllLines(templatePath);
            List<string> output = new List<string>();

            foreach (string line in lines)
            {
                if (line.Contains("//using UnityEngine"))
                {
                    output.Add(line.Replace("//using", "using"));
                }
                else if (line.Contains("//GenerateHere methods"))
                {
                    foreach (string name in packets)
                    {
                        output.Add($"\t// {name} 패킷 처리");
                        output.Add($"\tpublic static void Handle_{name}(Google.Protobuf.Protocol.{name} packet)");
                        output.Add("\t{");
                        output.Add($"\t\tDebug.Log($\"Handle_{name} 호출: {{packet}}\");");
                        output.Add("\t}");
                        output.Add("");
                    }
                }
                else if (line.Contains("//GenerateHere packetType"))
                {
                    foreach (string name in packets)
                    {
                        output.Add($"\t\tpacketManager.RegisterType<Google.Protobuf.Protocol.{name}>(PacketId.{name});");
                    }
                }
                else if (line.Contains("//GenerateHere register"))
                {
                    foreach (string name in packets)
                    {
                        output.Add($"\t\tpacketManager.Register(PacketId.{name}, msg => Handle_{name}((Google.Protobuf.Protocol.{name})msg));");
                    }
                }
                else
                {
                    output.Add(line);
                }
            }

            string outPath = Path.Combine(outputDir, "PacketHandler.cs");
            File.WriteAllLines(outPath, output);
            Console.WriteLine("[SUCCESS] PacketHandler.cs 생성 완료");
        }
    }
}