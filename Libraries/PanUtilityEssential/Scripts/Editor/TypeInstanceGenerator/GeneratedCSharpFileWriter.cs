using System.IO;
using System.Text;



namespace Pan.Util.Editors
{
    /// <summary>
    /// 타입 인스턴스 생성 결과를 프로젝트 표준 C# 파일 형식으로 기록합니다.
    /// </summary>
    internal static class GeneratedCSharpFileWriter
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);


        /// <summary>
        /// 생성 결과를 UTF-8 BOM 없음과 Windows CRLF 형식으로 저장합니다.
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
