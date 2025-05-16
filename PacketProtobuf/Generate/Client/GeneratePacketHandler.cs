/*--------------------------------------------------------
			    GeneratePacketHandler

- PacketHandler.cs 코드생성
--------------------------------------------------------*/
public class GeneratePacketHandler
{
    public static void Generate(Dictionary<string, List<string>> domains, string templatePath, string outputDir)
    {
        GenerateCode(domains, templatePath, outputDir);
    }

    private static void GenerateCode(Dictionary<string, List<string>> domains, string templatePath, string outputDir)
    {
        string[] lines = File.ReadAllLines(templatePath);
        List<string> output = new List<string>();

        foreach (string line in lines)
        {
            if (line.Contains("//using"))
            {
                output.Add(line.Replace("//using", "using"));
            }
            else if (line.Contains("// Generate DomainClass"))
            {
                MakePacketHandlerDomainClass(domains, output);
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

    private static void MakePacketHandlerDomainClass(Dictionary<string, List<string>> domains, List<string> output)
    {
        // 도메인별 클래스 생성
        foreach (var pair in domains)
        {
            string domain = pair.Key;

            output.Add($"public abstract class {domain}BasePacketHandler");
            output.Add("{");
            output.Add("\tpublic void RegisterPacketHandlers()");
            output.Add("\t{");

            foreach (string proto in pair.Value)
            {
                output.Add($"\t\tPacketManager.Instance.Register<{proto}>(PacketId.{proto}, msg => Handle_{proto}(({proto})msg));");
            }

            output.Add("\t}");
            output.Add("");

            foreach (string proto in pair.Value)
            {
                output.Add($"\tprotected virtual void Handle_{proto}({proto} packet) {{ }}");
            }

            output.Add("}");
            output.Add("");
        }
    }
}
