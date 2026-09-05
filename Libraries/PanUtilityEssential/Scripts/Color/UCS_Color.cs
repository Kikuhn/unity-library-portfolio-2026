using Sirenix.OdinInspector;
using System;
using UnityEngine;



//? 색깔 관련들이 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_Color
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 알파 값으로 변경한 새로운 색상을 반환합니다.
        /// </summary>
        /// <param name="color">원본 색상</param>
        /// <param name="alpha">적용할 알파 값 (0.0f ~ 1.0f 범위)</param>
        /// <returns>알파 값이 변경된 새로운 색상</returns>
        public static Color WithAlpha(this Color color, float alpha)
        {
            //. 원본 색상의 RGB 채널은 그대로 유지하고, 알파 채널만 새 값으로 대체합니다.
            return new Color(color.r, color.g, color.b, alpha);
        }



        /// <summary>
        /// 지정된 알파 값으로 원본 색상의 알파 채널을 변경합니다.
        /// </summary>
        /// <param name="color">변경할 색상 (참조로 전달되어 직접 수정됩니다)</param>
        /// <param name="alpha">적용할 알파 값 (0.0f ~ 1.0f 범위)</param>
        /// <returns>알파 채널이 변경된 색상</returns>
        public static Color SetAlpha(this ref Color color, float alpha)
        {
            //. 원본 색상의 RGB 채널은 그대로 유지하며, 알파 채널만 새 값으로 변경합니다.
            color = new Color(color.r, color.g, color.b, alpha);
            return color;
        }



        /// <summary>
        /// 알파 채널에 지정된 값을 곱한 결과로 변경된 새로운 색상을 반환합니다.
        /// </summary>
        /// <param name="color">원본 색상</param>
        /// <param name="multiplier">알파 채널에 곱할 값</param>
        /// <returns>알파 채널에 곱셈이 적용된 새로운 색상</returns>
        public static Color WithMultipliedAlpha(this Color color, float multiplier)
        {
            //. 원본 색상의 RGB 채널은 그대로 유지하고, 알파 채널에 multiplier를 곱한 값을 적용합니다.
            return new Color(color.r, color.g, color.b, color.a * multiplier);
        }



        /// <summary>
        /// 알파 채널에 지정된 값을 곱하여 원본 색상의 알파 채널을 변경합니다.
        /// </summary>
        /// <param name="color">변경할 색상 (참조로 전달되어 직접 수정됩니다)</param>
        /// <param name="multiplier">알파 채널에 곱할 값</param>
        /// <returns>알파 채널에 곱셈이 적용된 색상</returns>
        public static Color MultiplyAlpha(this ref Color color, float multiplier)
        {
            //. 원본 색상의 RGB 채널은 그대로 유지하며, 알파 채널에 multiplier를 곱한 값을 적용합니다.
            color = new Color(color.r, color.g, color.b, color.a * multiplier);
            return color;
        }



        /// <summary>
        /// 보색(반전 색상)을 계산하여 반환합니다.
        /// </summary>
        /// <param name="color">보색을 계산할 원본 색상</param>
        /// <returns>계산된 보색</returns>
        public static Color GetComplementaryColor(this Color color)
        {
            //. 각 RGB 채널의 값을 반전하여 보색을 계산합니다. 알파 채널은 그대로 유지됩니다.
            float r = 1.0f - color.r;
            float g = 1.0f - color.g;
            float b = 1.0f - color.b;
            return new Color(r, g, b, color.a);
        }



        /// <summary>
        /// 원본 색상을 보색(반전 색상)으로 변경합니다.
        /// </summary>
        /// <param name="color">보색으로 변경할 색상 (참조로 전달되어 직접 수정됩니다)</param>
        /// <remarks>
        /// 이 메서드는 RGB 채널의 값을 반전시켜 보색으로 변경하며, 알파 채널은 변경하지 않습니다.
        /// </remarks>
        public static void ApplyComplementaryColor(this ref Color color)
        {
            //. 원본 색상을 보색으로 변경합니다.
            color = GetComplementaryColor(color);
        }



        ///======================================================================================================================================================
        


        ///======================================================================================================================================================
    }



    public static class SU_ColorPresetRGB
    {
        public static Color Red_GrapeFruit1()
        {
            return new Color(0.929f, 0.333f, 0.396f);
        }

        public static Color Red_GrapeFruit2()
        {
            return new Color(0.855f, 0.267f, 0.325f);
        }

        public static Color Green_Emerald()
        {
            return new Color(0.1803921568627451f, 0.8f, 0.44313725490196076f);
        }

        public static Color Green_Emerald2()
        {
            return new Color(0.15294117647058825f, 0.6823529411764706f, 0.3764705882352941f);
        }

        public static Color Blue_Aqua1()
        {
            return new Color(0.31f, 0.757f, 0.914f);
        }

        public static Color Blue_Aqua2()
        {
            return new Color(0.231f, 0.686f, 0.855f);
        }
    }



    /// <summary>
    /// 토글이 켜졌을 때만 Color 편집이 가능한 재사용 구조체.
    /// </summary>
    [Serializable, InlineProperty, HideLabel]
    public struct ToggleColor
    {
        [HorizontalGroup("Row", Width = 20)]
        [LabelText(""), ToggleLeft]
        public bool Use;



        [HorizontalGroup("Row")]
        [HideLabel, EnableIf(nameof(Use))]
        public Color Color;



        public ToggleColor(Color defaultColor)
        {
            Use = false;
            Color = defaultColor;
        }

        public ToggleColor(float r, float g, float b, float a = 1f)
        {
            Use = false;
            Color = new Color(r, g, b, a);
        }



        //! 토글이 켜졌을 때 Color 반환
        public readonly bool TryGetColor(out Color color)
        {
            if (Use)
            {
                color = Color;
                return true;
            }

            color = default;   //. 무의미
            return false;
        }
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 16진수 문자열과 <see cref="Color"/> 간 변환 유틸리티.
    /// <br/>지원 형식:
    /// <list type="bullet">
    /// <item><description>#RGB, #RGBA (축약 표기)</description></item>
    /// <item><description>#RRGGBB, #RRGGBBAA (일반 표기)</description></item>
    /// <item><description>0xAARRGGBB (ARGB, 흔한 Win32/ARGB 표기)</description></item>
    /// <item><description>접두사 미사용도 허용: "FFF", "FFFFFFFF", "FF00FF" 등</description></item>
    /// </list>
    /// 불량 입력 시 기본값(흰색) 반환. 필요 시 <see cref="TryHexToColor(string,out Color)"/> 사용 권장.
    /// </summary>
    public static class HexColorExtensions
    {
        /// <summary>
        /// 16진수 문자열을 <see cref="Color"/>로 변환합니다. (RGBA 해석)
        /// </summary>
        /// <param name="hex">16진수 색 문자열(#, 0x 접두사 허용)</param>
        /// <param name="defaultAlpha">알파가 미포함일 때 사용할 알파(0~1)</param>
        public static Color HexToColor(this string hex, float defaultAlpha = 1f)
        {
            if (!TryHexToColor(hex, out var c, defaultAlpha)) return Color.white;
            return c;
        }

        /// <summary>
        /// 16진수 문자열을 <see cref="Color32"/>로 변환합니다. (RGBA 해석)
        /// </summary>
        /// <param name="hex">16진수 색 문자열(#, 0x 접두사 허용)</param>
        /// <param name="defaultAlpha">알파가 미포함일 때 사용할 알파(0~255)</param>
        public static Color32 HexToColor32(this string hex, byte defaultAlpha = 255)
        {
            if (!TryHexToColor32(hex, out var c32, defaultAlpha)) return new Color32(255, 255, 255, 255);
            return c32;
        }

        /// <summary>
        /// <see cref="Color"/>를 16진수 문자열로 변환합니다.
        /// </summary>
        /// <param name="color">원본 색상</param>
        /// <param name="includeAlpha">알파 포함 여부 (true: RRGGBBAA, false: RRGGBB)</param>
        /// <param name="withHash">앞에 '#' 접두사를 붙일지 여부</param>
        public static string ToHex(this Color color, bool includeAlpha = false, bool withHash = true)
        {
            Color32 c32 = color;
            return ToHex(c32, includeAlpha, withHash);
        }

        /// <summary>
        /// <see cref="Color32"/>를 16진수 문자열로 변환합니다.
        /// </summary>
        /// <param name="color">원본 색상</param>
        /// <param name="includeAlpha">알파 포함 여부 (true: RRGGBBAA, false: RRGGBB)</param>
        /// <param name="withHash">앞에 '#' 접두사를 붙일지 여부</param>
        public static string ToHex(this Color32 color, bool includeAlpha = false, bool withHash = true)
        {
            //. 버퍼 준비
            int len = includeAlpha ? 8 : 6;
            char[] chars = new char[withHash ? len + 1 : len];

            if (withHash) chars[0] = '#';

            //? R, G, B 채널부터 기록 (RRGGBB[AA])
            int i = withHash ? 1 : 0;
            WriteByteHex(color.r, chars, ref i);
            WriteByteHex(color.g, chars, ref i);
            WriteByteHex(color.b, chars, ref i);
            if (includeAlpha) WriteByteHex(color.a, chars, ref i);

            return new string(chars);
        }

        /// <summary>
        /// 16진수 문자열을 <see cref="Color"/>로 변환하려 시도합니다. 실패 시 false.
        /// </summary>
        public static bool TryHexToColor(string hex, out Color color, float defaultAlpha = 1f)
        {
            defaultAlpha = Mathf.Clamp01(defaultAlpha);
            if (!TryHexToColor32(hex, out var c32, (byte)Mathf.RoundToInt(defaultAlpha * 255f)))
            {
                color = Color.white;
                return false;
            }
            color = c32;
            return true;
        }

        /// <summary>
        /// 16진수 문자열을 <see cref="Color32"/>로 변환하려 시도합니다. 실패 시 false.
        /// </summary>
        public static bool TryHexToColor32(string hex, out Color32 color32, byte defaultAlpha = 255)
        {
            color32 = new Color32(255, 255, 255, 255);

            if (string.IsNullOrEmpty(hex))
            {
                //! 빈 문자열이면 실패
                return false;
            }

            //. 전처리: 공백 제거, 접두사 제거, 대문자화
            string s = hex.Trim();
            if (s.StartsWith("#")) s = s.Substring(1);
            else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            s = s.ToUpperInvariant();

            //? 길이에 따른 분기
            //? 3  : RGB (각 4비트 → 8비트 확장)
            //? 4  : RGBA
            //? 6  : RRGGBB
            //? 8  : RRGGBBAA (일반) / AARRGGBB(과거 표기) 둘 다 허용
            switch (s.Length)
            {
                case 3:
                {
                    byte r = Expand4Bit(ToHexNibble(s[0]));
                    byte g = Expand4Bit(ToHexNibble(s[1]));
                    byte b = Expand4Bit(ToHexNibble(s[2]));
                    color32 = new Color32(r, g, b, defaultAlpha);
                    return true;
                }
                case 4:
                {
                    byte r = Expand4Bit(ToHexNibble(s[0]));
                    byte g = Expand4Bit(ToHexNibble(s[1]));
                    byte b = Expand4Bit(ToHexNibble(s[2]));
                    byte a = Expand4Bit(ToHexNibble(s[3]));
                    color32 = new Color32(r, g, b, a);
                    return true;
                }
                case 6:
                {
                    byte r = ToByte(s, 0);
                    byte g = ToByte(s, 2);
                    byte b = ToByte(s, 4);
                    color32 = new Color32(r, g, b, defaultAlpha);
                    return true;
                }
                case 8:
                {
                    //? 우선 RRGGBBAA로 시도
                    byte r1 = ToByte(s, 0);
                    byte g1 = ToByte(s, 2);
                    byte b1 = ToByte(s, 4);
                    byte a1 = ToByte(s, 6);

                    //? 동시에 AARRGGBB 가능성도 고려: AARRGGBB → (r2,g2,b2,a2)
                    byte a2 = r1; // 기존 r1 위치값이 사실 alpha일 수 있음
                    byte r2 = g1;
                    byte g2 = b1;
                    byte b2 = a1;

                    //? 휴리스틱:
                    //? - RRGGBBAA가 정상일 확률이 높음
                    //? - 단, 입력이 "FF112233"처럼 ARGB로 보이는 전형적 패턴이면 AARRGGBB로 해석
                    bool looksArgb =
                        // 알파가 0x00~0xFF 아무 값이 올 수 있으나, 관습적으로 불투명(FF)이 자주 등장
                        // R/G/B가 0일 확률보다 Alpha가 FF일 확률이 더 높다고 보고 체크
                        (a2 == 0xFF && a1 != 0xFF) ||
                        // 또는 "0x" 접두사가 있었던 케이스(이미 제거됨)를 염두에 두고,
                        // 상단에서 0x를 제거했다면 RRGGBBAA 시도가 실패하는 경우가 적으므로 여기선 간단히 휴리스틱만
                        false;

                    if (looksArgb) color32 = new Color32(r2, g2, b2, a2);
                    else color32 = new Color32(r1, g1, b1, a1);
                    return true;
                }
                default:
                //! 지원하지 않는 길이
                return false;
            }
        }

        //======================================================================

        //? 0~15(4비트)를 두 배 확장하여 8비트로 변환 (예: 0xA → 0xAA)
        private static byte Expand4Bit(int nibble)
        {
            //. n * 16 + n == (n << 4) | n
            int v = (nibble << 4) | nibble;
            return (byte)v;
        }

        //? 단일 헥사 문자 → 0~15
        private static int ToHexNibble(char c)
        {
            //. '0'~'9'
            if (c >= '0' && c <= '9') return c - '0';
            //. 'A'~'F'
            if (c >= 'A' && c <= 'F') return 10 + (c - 'A');
            //! 범위 밖: 0 처리
            return 0;
        }

        //? 문자열 s의 지정 오프셋부터 2글자를 1바이트로 변환 (예: "FF" → 255)
        private static byte ToByte(string s, int offset)
        {
            int hi = ToHexNibble(s[offset]);
            int lo = ToHexNibble(s[offset + 1]);
            return (byte)((hi << 4) | lo);
        }

        //? 1바이트를 2글자 16진수로 chars에 기록
        private static void WriteByteHex(byte value, char[] chars, ref int index)
        {
            int hi = (value >> 4) & 0xF;
            int lo = value & 0xF;
            chars[index++] = NibbleToHexChar(hi);
            chars[index++] = NibbleToHexChar(lo);
        }

        //. 0~15 → '0'~'9','A'~'F'
        private static char NibbleToHexChar(int nibble)
        {
            return (char)(nibble < 10 ? ('0' + nibble) : ('A' + (nibble - 10)));
        }
    }



    ///======================================================================================================================================================
}
