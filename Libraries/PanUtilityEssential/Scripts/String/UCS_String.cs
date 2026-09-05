using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;



//? 문자열 관련들이 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_String
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 전역 공용 <see cref="StringBuilder"/>
        /// </summary>
        private static StringBuilder stringBuilderPublic;



        ///<summary>
        /// 전역 공용 <see cref="StringBuilder"/> 를 얻는다
        /// </summary>
        public static StringBuilder GetStringBuilderPublic(bool beforeClear = true)
        {
            if (stringBuilderPublic == null)
            {
                stringBuilderPublic = new StringBuilder();
            }

            if (beforeClear) stringBuilderPublic.Clear();

            return stringBuilderPublic;
        }



        ///======================================================================================================================================================



        //? 문자열 사이 삽입



        /// <summary>
        /// 문자열 배열을 받아 각 요소 사이에 지정된 구분자를 삽입하여 결합된 문자열을 반환합니다.
        /// </summary>
        /// <param name="separator">각 요소 사이에 삽입할 구분자 문자열</param>
        /// <param name="strings">결합할 문자열 목록</param>
        /// <returns>구분자가 삽입된 결합된 문자열</returns>
        public static string CombineWithSeparator(string separator, params string[] strings)
        {
            //? string.Join 메서드를 사용하여 문자열 배열을 구분자와 함께 결합합니다.
            return string.Join(separator, strings);
        }



        /// <summary>
        /// 문자열 배열을 받아 각 요소 사이에 지정된 구분자를 삽입하여 결합된 문자열을 반환합니다.
        /// </summary>
        /// <param name="separator">각 요소 사이에 삽입할 구분자 문자</param>
        /// <param name="strings">결합할 문자열 목록</param>
        /// <returns>구분자가 삽입된 결합된 문자열</returns>
        public static string CombineWithSeparator(char separator, params string[] strings)
        {
            //? string.Join 메서드를 사용하여 문자열 배열을 구분자와 함께 결합합니다.
            return string.Join(separator, strings);
        }



        /// <summary>
        /// 문자열 배열을 받아 각 요소 사이에 특정 문자열을 삽입하여 결합된 문자열을 반환합니다.
        /// </summary>
        /// <param name="separator">각 요소 사이에 삽입할 문자열</param>
        /// <param name="var">결합할 문자열 배열</param>
        /// <returns>구분자가 삽입된 결합된 문자열</returns>
        public static string JoinWithSeparator(this string[] var, string separator)
        {
            return string.Join(separator, var);
        }



        /// <summary>
        /// 문자열 배열을 받아 각 요소 사이에 특정 문자를 삽입하여 결합된 문자열을 반환합니다.
        /// </summary>
        /// <param name="separator">각 요소 사이에 삽입할 문자</param>
        /// <param name="var">결합할 문자열 배열</param>
        /// <returns>구분자가 삽입된 결합된 문자열</returns>
        public static string JoinWithSeparator(this string[] var, char separator)
        {
            return string.Join(separator, var);
        }



        /// <summary>
        /// 문자열 배열을 받아 각 요소 사이에 줄바꿈 문자를 삽입하여 결합된 문자열을 반환합니다.
        /// </summary>
        /// <param name="strings">결합할 문자열 배열</param>
        /// <returns>줄바꿈 문자가 삽입된 결합된 문자열</returns>
        public static string CombineWithLineBreak(params string[] strings)
        {
            //? 문자열 배열이 null이거나 비어 있는지 확인합니다.
            if (strings == null || strings.Length == 0)
            {
                return string.Empty;
            }

            //? string.Join 메서드를 사용하여 문자열 배열을 줄바꿈(Environment.NewLine)과 함께 결합합니다.
            return string.Join(Environment.NewLine, strings);
        }



        /// <summary>
        /// 문자열 배열을 받아 각 요소 사이에 줄바꿈 문자를 삽입하여 결합된 문자열을 반환합니다.
        /// </summary>
        /// <param name="var">결합할 문자열 배열</param>
        /// <returns>줄바꿈 문자가 삽입된 결합된 문자열</returns>
        public static string _CombineWithLineBreak(this string[] var)
        {
            return CombineWithLineBreak(var);
        }



        /// <summary>
        /// 지정된 StringBuilder에서 대상 부분 문자열을 찾아서 앞뒤로 특정 문자열을 추가합니다.
        /// </summary>
        /// <param name="originalStringBuilder">처리할 원본 StringBuilder입니다.</param>
        /// <param name="targetSubstring">앞뒤로 추가할 부분 문자열입니다.</param>
        /// <param name="prefix">대상 부분 문자열 앞에 추가할 문자열입니다.</param>
        /// <param name="suffix">대상 부분 문자열 뒤에 추가할 문자열입니다.</param>
        public static void WrapTargetSubstring(this StringBuilder originalStringBuilder, string targetSubstring, string prefix, string suffix)
        {
            // 입력 파라미터 유효성 검사
            if (originalStringBuilder == null || string.IsNullOrEmpty(targetSubstring))
            {
                return;
            }

            int currentIndex = 0;
            int targetIndex;

            // 원본 StringBuilder를 순회하며 대상 부분 문자열을 감쌉니다.
            while ((targetIndex = originalStringBuilder.ToString().IndexOf(targetSubstring, currentIndex)) != -1)
            {
                // 대상 부분 문자열 앞에 prefix 추가
                originalStringBuilder.Insert(targetIndex, prefix);
                // 대상 부분 문자열 뒤에 suffix 추가
                originalStringBuilder.Insert(targetIndex + prefix.Length + targetSubstring.Length, suffix);
                // 현재 인덱스를 업데이트하여 다음 대상 부분 문자열로 이동
                currentIndex = targetIndex + prefix.Length + targetSubstring.Length + suffix.Length;
            }
        }



        /// <summary>
        /// 지정된 문자열에서 대상 부분 문자열을 찾아서 앞뒤로 특정 문자열을 추가하여 반환합니다.
        /// </summary>
        /// <param name="originalText">처리할 원본 문자열입니다.</param>
        /// <param name="targetSubstring">앞뒤로 추가할 부분 문자열입니다.</param>
        /// <param name="prefix">대상 부분 문자열 앞에 추가할 문자열입니다.</param>
        /// <param name="suffix">대상 부분 문자열 뒤에 추가할 문자열입니다.</param>
        /// <returns>대상 부분 문자열이 앞뒤로 지정된 문자열로 감싸진 새로운 문자열을 반환합니다.</returns>
        public static string WrapTargetSubstring(this string originalText, string targetSubstring, string prefix, string suffix)
        {
            if (string.IsNullOrEmpty(originalText) || string.IsNullOrEmpty(targetSubstring))
            {
                return originalText;
            }

            var originalBuilder = new StringBuilder(originalText);
            WrapTargetSubstring(originalBuilder, targetSubstring, prefix, suffix);
            return originalBuilder.ToString();
        }



        /// <summary>
        /// 지정된 문자열에서 대상 부분 문자열을 찾아서 앞뒤로 특정 문자열을 추가하여 반환합니다.
        /// </summary>
        /// <param name="originalText">처리할 원본 문자열입니다.</param>
        /// <param name="targetSubstring">앞뒤로 추가할 부분 문자열입니다.</param>
        /// <param name="prefix">대상 부분 문자열 앞에 추가할 문자열입니다.</param>
        /// <param name="suffix">대상 부분 문자열 뒤에 추가할 문자열입니다.</param>
        /// <returns>대상 부분 문자열이 앞뒤로 지정된 문자열로 감싸진 새로운 문자열을 반환합니다.</returns>
        public static string WrapTargetSubstring(this object originalText, string targetSubstring, string prefix, string suffix)
        {
            return WrapTargetSubstring(originalText.ToString(), targetSubstring, prefix, suffix);
        }



        /// <summary>
        /// 지정된 문자열에서 대상 부분 문자열을 찾아서 앞뒤로 특정 문자열을 추가하여 반환합니다.
        /// </summary>
        /// <param name="originalText">처리할 원본 문자열입니다.</param>
        /// <param name="targetSubstring">앞뒤로 추가할 부분 문자열입니다.</param>
        /// <param name="prefix">대상 부분 문자열 앞에 추가할 문자열입니다.</param>
        /// <param name="suffix">대상 부분 문자열 뒤에 추가할 문자열입니다.</param>
        /// <returns>대상 부분 문자열이 앞뒤로 지정된 문자열로 감싸진 새로운 문자열을 반환합니다.</returns>
        public static string WrapTargetSubstring(this object originalText, object targetSubstring, string prefix, string suffix)
        {
            return WrapTargetSubstring(originalText.ToString(), targetSubstring.ToString(), prefix, suffix);
        }



        /// <summary>
        /// 지정된 문자열에서 앞뒤로 특정 문자열을 추가하여 반환합니다.
        /// </summary>
        /// <param name="originalText">처리할 원본 문자열입니다.</param>
        /// <param name="prefix">대상 부분 문자열 앞에 추가할 문자열입니다.</param>
        /// <param name="suffix">대상 부분 문자열 뒤에 추가할 문자열입니다.</param>
        public static string WrapTargetSubstring(this string originalText, string prefix, string suffix)
        {
            if (string.IsNullOrEmpty(originalText))
            {
                return originalText;
            }

            var originalBuilder = new StringBuilder(originalText);
            WrapTargetSubstring(originalBuilder, originalText, prefix, suffix);
            return originalBuilder.ToString();
        }



        /// <summary>
        /// 지정된 문자열에서 앞뒤로 특정 문자열을 추가하여 반환합니다.
        /// </summary>
        /// <param name="originalText">처리할 원본 문자열입니다.</param>
        /// <param name="prefix">대상 부분 문자열 앞에 추가할 문자열입니다.</param>
        /// <param name="suffix">대상 부분 문자열 뒤에 추가할 문자열입니다.</param>
        public static string WrapTargetSubstring(object originalText, string prefix, string suffix)
        {
            return WrapTargetSubstring(originalText.ToString(), prefix, suffix);
        }



        ///======================================================================================================================================================



        //? 알파벳



        /// <summary>
        /// 입력받은 문자열에서 첫 번째로 발견되는 영문 대문자를 소문자로 변환합니다.
        /// </summary>
        /// <param name="input">변환하고자 하는 문자열입니다.</param>
        /// <returns>
        /// 첫 번째 영문 대문자가 소문자로 변환된 문자열을 반환합니다.
        /// 만약 입력 문자열에 영문 대문자가 없으면 원본 문자열을 그대로 반환합니다.
        /// </returns>
        public static string LowerFirst(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return Char.ToLowerInvariant(input[0]) + input.Substring(1);
        }



        /// <summary>
        /// 입력받은 문자열에서 첫 번째로 발견되는 영문 대문자를 소문자로 변환합니다.
        /// </summary>
        public static string _LowerFirst(this string var)
        {
            return LowerFirst(var);
        }



        ///======================================================================================================================================================



        //? StringBuilder



        /// <summary>
        /// StringBuilder의 내용을 문자열로 변환하며, 필요에 따라 마지막 줄바꿈을 제거합니다.
        /// </summary>
        /// <param name="stringBuilder">변환할 StringBuilder 객체입니다.</param>
        /// <param name="removeLastLineBreak">마지막 줄바꿈을 제거할지 여부를 결정합니다.</param>
        /// <returns>StringBuilder 내용이 변환된 문자열을 반환하며, 설정에 따라 마지막 줄바꿈이 제거될 수 있습니다.</returns>
        public static string StringBuilderToString(StringBuilder stringBuilder, bool removeLastLineBreak)
        {
            // 입력 파라미터 유효성 검사
            if (stringBuilder == null)
            {
                throw new ArgumentNullException(nameof(stringBuilder), "StringBuilder 객체가 null일 수 없습니다.");
            }

            // StringBuilder의 내용을 문자열로 변환
            string result = stringBuilder.ToString();

            // removeLastLineBreak가 true이고 결과 문자열이 줄바꿈으로 끝나는 경우, 마지막 줄바꿈 제거
            if (removeLastLineBreak && result.EndsWith("\n"))
            {
                // 마지막 문자가 줄바꿈인지 확인 후 제거
                return result.TrimEnd('\n');
            }

            return result;
        }



        /// <summary>
        /// StringBuilder의 내용을 문자열로 변환하며, 필요에 따라 마지막 줄바꿈을 제거합니다.
        /// </summary>
        /// <param name="builder">변환할 StringBuilder 객체입니다.</param>
        /// <param name="removeLastLineBreak">마지막 줄바꿈을 제거할지 여부를 결정합니다.</param>
        /// <returns>StringBuilder 내용이 변환된 문자열을 반환하며, 설정에 따라 마지막 줄바꿈이 제거될 수 있습니다.</returns>
        public static string ToString(this StringBuilder stringBuilder, bool removeLastLineBreak)
        {
            // 입력 파라미터 유효성 검사
            if (stringBuilder == null)
            {
                throw new ArgumentNullException(nameof(stringBuilder), "StringBuilder 객체가 null일 수 없습니다.");
            }

            // StringBuilder의 내용을 문자열로 변환
            string result = stringBuilder.ToString();

            // removeLastLineBreak가 true이고 결과 문자열이 줄바꿈으로 끝나는 경우, 마지막 줄바꿈 제거
            if (removeLastLineBreak && result.EndsWith("\n"))
            {
                // 마지막 문자가 줄바꿈인지 확인 후 제거
                return result.TrimEnd('\n');
            }

            return result;
        }



        /// <summary>
        /// StringBuilder에서 모든 HTML 태그를 제거합니다.
        /// </summary>
        /// <param name="stringBuilder">대상 StringBuilder</param>
        /// <returns>HTML 태그가 제거된 StringBuilder</returns>
        public static StringBuilder RemoveHtmlTags(this StringBuilder stringBuilder)
        {
            if (stringBuilder == null)
                throw new ArgumentNullException(nameof(stringBuilder));

            // HTML 태그를 감지하는 정규식
            string htmlTagPattern = @"<[^>]+>";

            // StringBuilder를 문자열로 변환하고 정규식으로 HTML 태그 제거
            string cleanText = Regex.Replace(stringBuilder.ToString(), htmlTagPattern, string.Empty);

            // 기존 StringBuilder 내용을 클리어하고 정제된 문자열 추가
            stringBuilder.Clear();
            stringBuilder.Append(cleanText);

            return stringBuilder;
        }



        /// <summary>
        /// 일시적으로 StringBuilder를 사용하여 작업을 수행한 후 초기 상태로 되돌립니다.
        /// </summary>
        /// <param name="sb">작업에 사용할 StringBuilder 인스턴스</param>
        /// <param name="action">수행할 작업 델리게이트</param>
        /// <param name="clearBeforeAction">작업 전에 StringBuilder를 초기화할지 여부 (기본값: true)</param>
        public static void UseTemporaryStringBuilder(this StringBuilder sb, Action<StringBuilder> action, bool clearBeforeAction = true)
        {
            //! 필요 시 StringBuilder 초기화
            if (clearBeforeAction) sb.Clear();

            action?.Invoke(sb);

            sb.Clear(); //! 작업 후 다시 초기화
        }



        /// <summary>
        /// 일시적으로 StringBuilder를 사용하여 문자열을 생성한 후, 해당 문자열을 반환합니다.
        /// </summary>
        /// <param name="sb">작업에 사용할 StringBuilder 인스턴스</param>
        /// <param name="removeLastLineBreak">마지막 줄바꿈을 제거할지 여부를 결정합니다.</param>
        /// <returns>생성된 문자열</returns>
        public static string GetTemporaryString(this StringBuilder sb, bool removeLastLineBreak = false)
        {
            string result = null;
            UseTemporaryStringBuilder(sb, x => result = x.ToString(removeLastLineBreak), false);
            return result;
        }



        ///======================================================================================================================================================



        //? StringBuilder 심화



        /// <summary>
        /// 대상 객체의 모든 필드와 해당 값을 문자열로 변환하여 지정된 <see cref="StringBuilder"/>에 추가합니다.
        /// </summary>
        /// <typeparam name="T">처리할 객체의 타입</typeparam>
        /// <param name="obj">대상 객체</param>
        /// <param name="stringBuilder">결과를 추가할 <see cref="StringBuilder"/> 인스턴스</param>
        /// <param name="separator">컬렉션 요소를 구분할 문자열 (기본값: ", ")</param>
        /// <param name="indentLevel">현재 들여쓰기 수준 (기본값: 0)</param>
        /// <remarks>
        /// 이 메서드는 객체의 모든 필드를 반영(reflection)을 통해 탐색하여,
        /// 각 필드의 이름과 값을 지정된 <see cref="StringBuilder"/>에 추가합니다.
        /// 특히 Unity에서 [SerializeField] 속성이 부여된 비공개 필드도 포함하여 처리합니다.
        /// 주로 디버깅이나 로깅 시 객체의 상태를 문자열로 표현하고자 할 때 유용합니다.
        /// </remarks>
        /// <example>
        /// 다음은 이 메서드를 사용하는 예시입니다:
        /// <code>
        /// var myObject = new MyClass();
        /// var sb = new StringBuilder();
        /// ObjectInspector.AppendObjectFields(myObject, sb);
        /// Console.WriteLine(sb.ToString());
        /// </code>
        /// </example>
        public static void AppendFieldsAsString<T>(T obj, StringBuilder stringBuilder, string separator = ", ", int indentLevel = 0)
        {
            if (obj == null)
            {
                stringBuilder.Append("null");
                return;
            }

            Type type = typeof(T);

            // 모든 필드를 가져옵니다 (Public, Non-Public, Instance)
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // Unity의 [SerializeField] 속성이 있는 필드만 처리
                if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)))
                {
                    // 필드 이름
                    string fieldName = field.Name;

                    // 필드 값 가져오기
                    object fieldValue = field.GetValue(obj);

                    // 필드 값을 문자열로 변환
                    string valueString = ConvertFieldValueToString(fieldValue, separator, indentLevel + 1);

                    // 들여쓰기 적용
                    string indent = new string('\t', indentLevel);
                    stringBuilder.AppendLine($"{indent}{fieldName}: {valueString}");
                }
            }
        }



        /// <summary>
        /// 필드 값을 문자열로 변환합니다.
        /// </summary>
        /// <param name="value">필드 값</param>
        /// <param name="separator">컬렉션 요소 구분자</param>
        /// <param name="indentLevel">현재 들여쓰기 수준</param>
        /// <returns>문자열로 변환된 값</returns>
        private static string ConvertFieldValueToString(object value, string separator, int indentLevel)
        {
            if (value == null)
            {
                return "null";
            }

            Type valueType = value.GetType();

            // 컬렉션(리스트, 배열 등)을 처리
            if (typeof(IEnumerable).IsAssignableFrom(valueType) && value is IEnumerable enumerable)
            {
                StringBuilder collectionBuilder = new StringBuilder();
                collectionBuilder.AppendLine("[");
                string indent = new string('\t', indentLevel);

                foreach (var item in enumerable)
                {
                    collectionBuilder.Append(indent);
                    collectionBuilder.Append(ConvertFieldValueToString(item, separator, indentLevel));
                    collectionBuilder.AppendLine(separator.Trim());
                }

                collectionBuilder.Append(new string('\t', indentLevel - 1));
                collectionBuilder.Append("]");
                return collectionBuilder.ToString();
            }

            // 객체 타입이라면 필드 내용 재귀적으로 탐색
            if (!valueType.IsPrimitive && !valueType.IsEnum && valueType != typeof(string))
            {
                StringBuilder objectBuilder = new StringBuilder();
                objectBuilder.AppendLine("{");
                string indent = new string('\t', indentLevel);

                FieldInfo[] subFields = valueType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var subField in subFields)
                {
                    if (subField.IsPublic || Attribute.IsDefined(subField, typeof(SerializeField)))
                    {
                        object subValue = subField.GetValue(value);
                        objectBuilder.Append(indent);
                        objectBuilder.AppendLine($"{subField.Name}: {ConvertFieldValueToString(subValue, separator, indentLevel + 1)},");
                    }
                }

                objectBuilder.Append(new string('\t', indentLevel - 1));
                objectBuilder.Append("}");
                return objectBuilder.ToString();
            }

            // 기본값은 ToString 호출
            return value.ToString();
        }



        ///======================================================================================================================================================



        //? 값 단위 문자열 변환



        /// <summary>
        /// long(바이트 단위) 값을 KB, MB, GB 등으로
        /// 사람이 읽기 쉬운 용량 문자열로 변환합니다. (1000 배수)
        /// </summary>
        /// <param name="byteSize">바이트 단위의 long 값</param>
        /// <returns>"4B", "4KB", "3.95MB" 처럼 사람이 읽기 쉬운 문자열</returns>
        public static string ToByteSizeString(this long byteSize)
        {
            // 0보다 작은 값이면 에러 or 예외처리
            if (byteSize < 0)
                return "Invalid size";

            // 단위 배열 (필요하면 PB, EB 등 더 추가 가능)
            string[] units = { "B", "KB", "MB", "GB", "TB" };

            // double로 변환해 계산
            double size = byteSize;
            int unitIndex = 0;

            // 1000이 넘으면 다음 단위로 나누어가며 이동
            while (size >= 1000 && unitIndex < units.Length - 1)
            {
                size /= 1000;
                unitIndex++;
            }

            // 정수 B 단위(0~999)에서는 소수점 없이, 그 이상 단위는 2자리 정도 소수점 표현
            if (unitIndex == 0)
            {
                // byte라면 정수만 표기
                return $"{size:0}{units[unitIndex]}";
            }
            else
            {
                // KB 이상이면 소수점 2자리 (필요하면 조절)
                return $"{size:0.00}{units[unitIndex]}";
            }
        }



        ///======================================================================================================================================================



        //? System.Type 확장



        /// <summary>
        /// 제네릭 인자 수를 나타내는 문자를 제거한 순수한 클래스 이름을 반환합니다.
        /// </summary>
        /// <param name="type">확장할 Type 객체</param>
        /// <returns>순수한 클래스 이름</returns>
        public static string GetPureClassName(this Type type)
        {
            //? Type.Name 속성은 클래스의 이름을 반환하지만, 제네릭 타입의 경우 'ClassName`1'과 같이 반환됩니다.
            string typeName = type.Name;
            int backtickIndex = typeName.IndexOf('`');
            //. 백틱(`) 문자가 없다면 제네릭 타입이 아니므로, 전체 이름을 반환합니다.
            if (backtickIndex == -1)
            {
                return typeName;
            }
            //! 백틱(`) 문자가 있다면, 해당 위치 이전의 문자열만을 추출하여 반환합니다.
            return typeName.Substring(0, backtickIndex);
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}
