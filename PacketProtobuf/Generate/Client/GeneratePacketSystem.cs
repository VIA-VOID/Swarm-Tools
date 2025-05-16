/*--------------------------------------------------------
			    GeneratePacketSystem

- PacketSystem.cs 코드생성
--------------------------------------------------------*/
public class GeneratePacketSystem
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
            if (line.Contains("// Generate AllHandlers"))
            {
                MakeAllHandlersImple(domains, output);
            }
            else
            {
                output.Add(line);
            }
        }

        string outPath = Path.Combine(outputDir, "PacketSystem.cs");
        File.WriteAllLines(outPath, output);
        Console.WriteLine("[SUCCESS] PacketSystem.cs 생성 완료");
    }

    private static void MakeAllHandlersImple(Dictionary<string, List<string>> domains, List<string> output)
    {
        foreach (var domain in domains.Keys)
        {
            output.Add($"\t\t{domain}PacketHandler.Instance.RegisterPacketHandlers();");
        }
    }
}