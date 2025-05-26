enum ServiceType : UInt16
{
    Client,
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

}
