using System.IO;
using System.Text;



namespace Pan.AddressableManagers.Editor
{
    /// <summary>
    /// 에디터 생성기가 만드는 C# 파일의 인코딩과 줄바꿈 형식을 통일합니다.
    /// </summary>
    internal static class GeneratedCSharpFile
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);


        /// <summary>
        /// 자동 생성 파일임을 알리는 공통 헤더를 작성합니다.
        /// </summary>
        internal static void AppendHeader(StringBuilder source, string purpose)
        {
            source.AppendLine("///======================================================================================================================================================");
            source.Append("//? ").AppendLine(purpose);
            source.AppendLine("//! 수동 편집 금지: 생성 원본을 수정한 뒤 다시 생성해야 합니다.");
            source.AppendLine("///======================================================================================================================================================");
            source.AppendLine();
        }


        /// <summary>
        /// C# 생성 결과를 UTF-8 BOM 없음과 Windows CRLF 형식으로 기록합니다.
        /// </summary>
        internal static void Write(string path, string source)
        {
            string normalized = source
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", "\r\n");

            File.WriteAllText(path, normalized, Utf8WithoutBom);
        }
    }
}
