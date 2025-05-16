using System.Text.RegularExpressions;

/*--------------------------------------------------------
					Program

- Protobuf 자동화 코드
- Template/*.h 기반으로 .proto에 해당하는
  패킷 자동화 코드 작성
--------------------------------------------------------*/

public class Program
{
    static void Main(string[] args)
    {
        // 서버 자동화 코드 실행
        ServerAutoGenerate.GenerateServer(args);
        // 클라이언트(unity) 자동화 코드 실행
        ClientAutoGenerate.GenerateClient(args);
    }
}
