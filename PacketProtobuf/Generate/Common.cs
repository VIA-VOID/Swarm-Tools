enum ServiceType : UInt16
{
    Dummy,
    Unreal,
    Server
}

// 공통 유틸리티 함수
class Common
{
    // template 파일 경로 가져오기
    public static string? GetTemplateFilePath(ServiceType type, string fileName)
    {
        string templatePath = Path.Combine("..", "Template", type.ToString(), fileName);
        if (File.Exists(templatePath) == false)
        {
            Console.WriteLine("[ERROR] 템플릿 파일 없음");
            return null;
        }
        return templatePath;
    }

    // 도메인별로 저장
    public static void SaveDomains(List<string> packets, Dictionary<string, List<string>> domains)
    {
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

    // 파일쓰기
    public static void WriteFile(List<string> output, string outputDir, string fileName)
    {
        string outPath = Path.Combine(outputDir, fileName);
        File.WriteAllLines(outPath, output);
        Console.WriteLine($"[SUCCESS] {fileName} 생성 완료");
    }

    // 상대경로->절대경로로 파일 쓰기
    public static void FullPathWriteFile(List<string> output, string outputDir, string fileName)
    {
        string fullPath = Path.GetFullPath(Path.Combine(outputDir, fileName));
        File.WriteAllLines(fullPath, output);
        Console.WriteLine($"[SUCCESS] {fileName} 생성 완료");
    }

    // DomainPacketHandler 자동생성
    public static void GenerateDomainCode(Dictionary<string, List<string>> domains, string filePath, string outputDirPath)
    {
        string[] lines = File.ReadAllLines(filePath);

        foreach (var pair in domains)
        {
            string domain = pair.Key;
            List<string> protocols = pair.Value;
            List<string> output = new List<string>();

            foreach (string line in lines)
            {
                if (line.Contains("// DomainPacketHandler"))
                {
                    output.Add($"\t\t\t\t{domain}PacketHandler");
                }
                else if (line.Contains("DomainPacketHandler"))
                {
                    output.Add($"class {domain}PacketHandler : public PacketHandler");
                }
                else if (line.Contains("// Generate RegisterHandler"))
                {
                    foreach (string name in protocols)
                    {
                        output.Add($"\t\tRegisterPacket<Protocol::{name}>(PacketID::{name}, Handle_{name});");
                    }

                }
                else if (line.Contains("// Generate Handler"))
                {
                    foreach (string name in protocols)
                    {
                        output.Add($"\tstatic void Handle_{name}(SessionRef session, const Protocol::{name}& packet);");
                    }
                }
                else
                {
                    output.Add(line);
                }
            }

            FullPathWriteFile(output, outputDirPath, $"{domain}PacketHandler.h");
        }
    }

    // PacketHandler.cpp 자동생성
    public static void GeneratePacketHandlerInit(ServiceType serviceType, Dictionary<string, List<string>> domains, string outputDir)
    {
        string? packetHandlerPath = Common.GetTemplateFilePath(serviceType, "PacketHandler.cpp");
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
                    if(serviceType == ServiceType.Unreal)
                    {
                        output.Add($"\tDomainHandlerClasses.Add(MakeUnique<F{domain}PacketHandler>());");
                    }
                    else
                    {
                        output.Add($"\t_domainHandlerClasses.emplace_back(ObjectPool<{domain}PacketHandler>::MakeUnique());");
                    }
                }
            }
            else
            {
                output.Add(line);
            }
        }

        string fileName = "PacketHandler.cpp";
        
        if (serviceType == ServiceType.Unreal)
        {
            fileName = "UE_PacketHandler.cpp";
        }
        else if(serviceType == ServiceType.Dummy)
        {
            fileName = "DM_PacketHandler.cpp";
        }

        WriteFile(output, outputDir, fileName);
    }
}
