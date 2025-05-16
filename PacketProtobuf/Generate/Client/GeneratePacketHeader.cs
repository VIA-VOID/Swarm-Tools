/*--------------------------------------------------------
			    GeneratePacketHeader

- PacketHeader.cs 코드생성
--------------------------------------------------------*/

public class GeneratePacketHeader
{
    public static void Generate(List<string> packets, string templatePath, string outputDir)
    {
        GenerateCode(packets, templatePath, outputDir);
    }

    private static void GenerateCode(List<string> packets, string templatePath, string outputDir)
    {
        string[] lines = File.ReadAllLines(templatePath);
        List<string> output = new List<string>();
        int baseServerNum = 10001;
        int baseClientNum = 30001;

        foreach (string line in lines)
        {
            if (line.Contains("// Generate PacketId"))
            {
                foreach (string name in packets)
                {
                    if (name.StartsWith("SC"))
                    {
                        output.Add($"\t{name} = {baseClientNum++},");
                    }
                    else if (name.StartsWith("CS"))
                    {
                        output.Add($"\t{name} = {baseServerNum++},");
                    }
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
}
