using System;
using System.Text;

/// <summary>
/// ThaiFontAdjuster — ระบบแก้ไขปัญหาสระลอย และวรรณยุกต์ซ้อนทับ/หายไป (Thai Vowel & Tone Mark Corrector)
/// แปลงสตริงภาษาไทยมาตรฐานให้ใช้รหัส Unicode PUA (Private Use Area: 0xF700 - 0xF71A)
/// เพื่อจัดระดับวรรณยุกต์และสระบน/ล่างใน TextMeshPro ได้อย่างถูกต้องตามหลักสรีระตัวอักษรไทย:
/// 1. วรรณยุกต์บนพยัญชนะปกติที่ไม่มีสระบน -> ใช้ระดับปกติที่ปรับความสูงให้เตี้ยลง (แก้ปัญหาสระลอย)
/// 2. วรรณยุกต์ที่ซ้อนบนสระบน (ิ ี ึ ื ั ็ ํ) -> เลื่อนขึ้นไประดับ Tier 3 (แก้ปัญหาวรรณยุกต์จม/หาย/ซ้อนทับ)
/// 3. สระบน/วรรณยุกต์บนพยัญชนะหางยาว (ป ฝ ฟ ฬ) -> เลื่อนหลบหางไปทางซ้าย (แก้ปัญหาชนหาง)
/// 4. สระอำผสมวรรณยุกต์ (เช่น น้ำ, ช้ำ) -> แยก นิคหิต + วรรณยุกต์บน + สระอา
/// 5. สระล่าง (ุ ู) บนพยัญชนะหางยาวล่าง (ฎ ฏ) -> ปรับเลื่อนลงหลบหาง
/// </summary>
public static class ThaiFontAdjuster
{
    // ─── Unicode Constants ───────────────────────────────────────────────────
    // Base vowels & diacritics
    public const char SARA_AM          = '\u0E33'; // ำ
    public const char SARA_AA          = '\u0E32'; // า
    public const char MAI_HAN_AKAT     = '\u0E31'; // ั
    public const char SARA_I           = '\u0E34'; // ิ
    public const char SARA_II          = '\u0E35'; // ี
    public const char SARA_UE          = '\u0E36'; // ึ
    public const char SARA_UEE         = '\u0E37'; // ื
    public const char SARA_U           = '\u0E38'; // ุ
    public const char SARA_UU          = '\u0E39'; // ู
    public const char PHINTHU          = '\u0E3A'; // ฺ
    public const char MAITAIKHU        = '\u0E47'; // ็
    public const char MAI_EK           = '\u0E48'; // ่
    public const char MAI_THO          = '\u0E49'; // ้
    public const char MAI_TRI          = '\u0E4A'; // ๊
    public const char MAI_CHATTAWA     = '\u0E4B'; // ๋
    public const char THANTHAKHAT      = '\u0E4C'; // ์ (การันต์)
    public const char NIKHAHIT         = '\u0E4D'; // ํ
    public const char YAMAKKAN         = '\u0E4E'; // ๎

    // Tall consonants (ป ฝ ฟ ฬ)
    public const char PO_PLA           = '\u0E1B'; // ป
    public const char FO_FA            = '\u0E1D'; // ฝ
    public const char FO_FAN           = '\u0E1F'; // ฟ
    public const char LO_CHULA         = '\u0E2C'; // ฬ

    // Descender consonants (ฎ ฏ)
    public const char DO_CHADA         = '\u0E0E'; // ฎ
    public const char TO_PATAK         = '\u0E0F'; // ฏ

    // Tailless consonants (ญ ฐ)
    public const char YO_YING          = '\u0E0D'; // ญ
    public const char THO_THAN         = '\u0E10'; // ฐ

    // ─── PUA Unicode Mapping (MacThai / Windows / NECTEC Standard) ───────────
    // Tier 3: Upper Level Tone Marks (วางซ้อนเหนือสระบน)
    public const char PUA_MAI_EK_UPPER       = '\uF70A';
    public const char PUA_MAI_THO_UPPER      = '\uF70B';
    public const char PUA_MAI_TRI_UPPER      = '\uF70C';
    public const char PUA_MAI_CHATTAWA_UPPER = '\uF70D';
    public const char PUA_THANTHAKHAT_UPPER  = '\uF70E';

    // Left-shifted Upper Vowels (หลบหาง ป ฝ ฟ ฬ)
    public const char PUA_MAI_HAN_AKAT_LEFT  = '\uF710';
    public const char PUA_SARA_I_LEFT        = '\uF701';
    public const char PUA_SARA_II_LEFT       = '\uF702';
    public const char PUA_SARA_UE_LEFT       = '\uF703';
    public const char PUA_SARA_UEE_LEFT      = '\uF704';
    public const char PUA_MAITAIKHU_LEFT     = '\uF711';
    public const char PUA_NIKHAHIT_LEFT      = '\uF712';

    // Left-shifted Tone Marks (วางเหนือ ป ฝ ฟ ฬ แบบไม่มีสระบน)
    public const char PUA_MAI_EK_LEFT        = '\uF713';
    public const char PUA_MAI_THO_LEFT       = '\uF714';
    public const char PUA_MAI_TRI_LEFT       = '\uF715';
    public const char PUA_MAI_CHATTAWA_LEFT  = '\uF716';
    public const char PUA_THANTHAKHAT_LEFT   = '\uF717';

    // Tier 3 Left-shifted Tone Marks (วางซ้อนเหนือสระบนที่อยู่บน ป ฝ ฟ ฬ)
    public const char PUA_MAI_EK_UPPER_LEFT       = '\uF705';
    public const char PUA_MAI_THO_UPPER_LEFT      = '\uF706';
    public const char PUA_MAI_TRI_UPPER_LEFT      = '\uF707';
    public const char PUA_MAI_CHATTAWA_UPPER_LEFT = '\uF708';
    public const char PUA_THANTHAKHAT_UPPER_LEFT  = '\uF709';

    // Lower Vowels Shifted Down (หลบหาง ฎ ฏ)
    public const char PUA_SARA_U_LOWER       = '\uF718';
    public const char PUA_SARA_UU_LOWER      = '\uF719';
    public const char PUA_PHINTHU_LOWER      = '\uF71A';

    // Tailless consonants (ตัดเชิงล่างเมื่อใส่สระล่าง ญ ฐ)
    public const char PUA_YO_YING_TAILLESS   = '\uF700';
    public const char PUA_THO_THAN_TAILLESS  = '\uF70F';

    // ─────────────────────────────────────────────────────────────────────────
    #region Classification Helpers

    public static bool IsUpperVowel(char c)
    {
        return c == MAI_HAN_AKAT || c == SARA_I || c == SARA_II ||
               c == SARA_UE || c == SARA_UEE || c == MAITAIKHU ||
               c == NIKHAHIT || c == YAMAKKAN ||
               c == PUA_MAI_HAN_AKAT_LEFT || c == PUA_SARA_I_LEFT ||
               c == PUA_SARA_II_LEFT || c == PUA_SARA_UE_LEFT ||
               c == PUA_SARA_UEE_LEFT || c == PUA_MAITAIKHU_LEFT ||
               c == PUA_NIKHAHIT_LEFT;
    }

    public static bool IsToneMark(char c)
    {
        return c == MAI_EK || c == MAI_THO || c == MAI_TRI ||
               c == MAI_CHATTAWA || c == THANTHAKHAT ||
               (c >= '\uF705' && c <= '\uF70E') ||
               (c >= '\uF713' && c <= '\uF717');
    }

    public static bool IsLowerVowel(char c)
    {
        return c == SARA_U || c == SARA_UU || c == PHINTHU ||
               c == PUA_SARA_U_LOWER || c == PUA_SARA_UU_LOWER ||
               c == PUA_PHINTHU_LOWER;
    }

    public static bool IsTallConsonant(char c)
    {
        return c == PO_PLA || c == FO_FA || c == FO_FAN || c == LO_CHULA;
    }

    public static bool IsDescenderConsonant(char c)
    {
        return c == DO_CHADA || c == TO_PATAK;
    }

    public static bool IsTaillessCandidate(char c)
    {
        return c == YO_YING || c == THO_THAN;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Transformation

    /// <summary>
    /// ปรับข้อความภาษาไทยให้สระและวรรณยุกต์ตรงตำแหน่ง ไม่ลอย ไม่ชน และไม่หาย
    /// ปลอดภัยสำหรับข้อความที่มี Rich Text Tag เช่น &lt;color&gt;, &lt;size&gt;, &lt;b&gt;
    /// </summary>
    public static string Adjust(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // ขั้นที่ 1: จัดการกรณีพิเศษของ สระอำ ผสมวรรณยุกต์ (เช่น น้ำ, ย้ำ, ค่ำ)
        // บางครั้งพิมพ์: [พยัญชนะ] + [วรรณยุกต์] + [ำ] หรือ [พยัญชนะ] + [ำ] + [วรรณยุกต์]
        // ต้องแยกเป็น [พยัญชนะ] + [นิคหิต ํ] + [วรรณยุกต์ Tier 3] + [สระอา า]
        string normalized = NormalizeSaraAm(input);

        // ขั้นที่ 2: วนลูปปรับแต่งระดับของตัวอักษรทีละตัว
        StringBuilder sb = new StringBuilder(normalized.Length);
        int len = normalized.Length;
        bool inTag = false;

        for (int i = 0; i < len; i++)
        {
            char current = normalized[i];

            // ข้ามการประมวลผลภายใน Rich Text Tags (เช่น <color=#FFE8A3>)
            if (current == '<')
            {
                inTag = true;
                sb.Append(current);
                continue;
            }
            if (current == '>')
            {
                inTag = false;
                sb.Append(current);
                continue;
            }
            if (inTag)
            {
                sb.Append(current);
                continue;
            }

            // หาตัวอักษรก่อนหน้า (ข้าม tag ถ้ามี)
            char prevChar = GetPrevValidChar(normalized, i);
            char prevPrevChar = GetPrevPrevValidChar(normalized, i);

            // ── Case 1: วรรณยุกต์ (่ ้ ๊ ๋ ์) ──
            if (IsToneMark(current))
            {
                bool prevIsUpper = IsUpperVowel(prevChar);
                bool baseIsTall = prevIsUpper ? IsTallConsonant(prevPrevChar) : IsTallConsonant(prevChar);

                if (prevIsUpper)
                {
                    // วรรณยุกต์ซ้อนบนสระบน -> เลื่อนขึ้น Tier 3
                    if (baseIsTall)
                    {
                        sb.Append(GetUpperLeftToneMark(current));
                    }
                    else
                    {
                        sb.Append(GetUpperToneMark(current));
                    }
                }
                else if (baseIsTall)
                {
                    // วรรณยุกต์อยู่บนพยัญชนะหางยาว (ไม่มีสระบน) -> เลื่อนหลบหางไปทางซ้าย
                    sb.Append(GetLeftToneMark(current));
                }
                else
                {
                    // วรรณยุกต์บนพยัญชนะปกติ -> ใช้วรรณยุกต์ระดับปกติ (ฐานที่ปรับ BearingY เรียบร้อย)
                    sb.Append(GetNormalToneMark(current));
                }
                continue;
            }

            // ── Case 2: สระบน (ั ิ ี ึ ื ็ ํ) ──
            if (IsUpperVowel(current))
            {
                if (IsTallConsonant(prevChar))
                {
                    // สระบนอยู่บนพยัญชนะหางยาว -> เลื่อนหลบหางไปทางซ้าย
                    sb.Append(GetLeftUpperVowel(current));
                }
                else
                {
                    sb.Append(current);
                }
                continue;
            }

            // ── Case 3: สระล่าง (ุ ู ฺ) ──
            if (IsLowerVowel(current))
            {
                if (IsDescenderConsonant(prevChar))
                {
                    // สระล่างอยู่ใต้ ฎ ฏ -> เลื่อนลงหลบหาง
                    sb.Append(GetLowerVowelShifted(current));
                }
                else
                {
                    sb.Append(current);
                }
                continue;
            }

            // ── Case 4: พยัญชนะ ญ ฐ ที่มีสระล่างตามมา ──
            if (IsTaillessCandidate(current))
            {
                char nextChar = GetNextValidChar(normalized, i);
                if (IsLowerVowel(nextChar))
                {
                    // ตัดเชิงล่าง
                    sb.Append(current == YO_YING ? PUA_YO_YING_TAILLESS : PUA_THO_THAN_TAILLESS);
                    continue;
                }
            }

            // ตัวอักษรทั่วไป
            sb.Append(current);
        }

        return sb.ToString();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Sara Am Normalization

    private static string NormalizeSaraAm(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf(SARA_AM) < 0) return text;

        StringBuilder sb = new StringBuilder(text.Length + 16);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == SARA_AM)
            {
                // ตรวจดูตัวก่อนหน้าว่ามีวรรณยุกต์หรือไม่
                if (sb.Length > 0 && IsToneMark(sb[sb.Length - 1]))
                {
                    char tone = sb[sb.Length - 1];
                    sb.Remove(sb.Length - 1, 1); // ลบวรรณยุกต์ออกชั่วคราว
                    sb.Append(NIKHAHIT);
                    sb.Append(tone);
                    sb.Append(SARA_AA);
                }
                else
                {
                    // สระอำเดี่ยวๆ
                    sb.Append(c);
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Lookup Mappings

    private static char GetUpperToneMark(char tone)
    {
        switch (tone)
        {
            case MAI_EK:       return PUA_MAI_EK_UPPER;
            case MAI_THO:      return PUA_MAI_THO_UPPER;
            case MAI_TRI:      return PUA_MAI_TRI_UPPER;
            case MAI_CHATTAWA: return PUA_MAI_CHATTAWA_UPPER;
            case THANTHAKHAT:  return PUA_THANTHAKHAT_UPPER;
            default:           return tone;
        }
    }

    private static char GetUpperLeftToneMark(char tone)
    {
        switch (tone)
        {
            case MAI_EK:       return PUA_MAI_EK_UPPER_LEFT;
            case MAI_THO:      return PUA_MAI_THO_UPPER_LEFT;
            case MAI_TRI:      return PUA_MAI_TRI_UPPER_LEFT;
            case MAI_CHATTAWA: return PUA_MAI_CHATTAWA_UPPER_LEFT;
            case THANTHAKHAT:  return PUA_THANTHAKHAT_UPPER_LEFT;
            default:           return tone;
        }
    }

    private static char GetLeftToneMark(char tone)
    {
        switch (tone)
        {
            case MAI_EK:       return PUA_MAI_EK_LEFT;
            case MAI_THO:      return PUA_MAI_THO_LEFT;
            case MAI_TRI:      return PUA_MAI_TRI_LEFT;
            case MAI_CHATTAWA: return PUA_MAI_CHATTAWA_LEFT;
            case THANTHAKHAT:  return PUA_THANTHAKHAT_LEFT;
            default:           return tone;
        }
    }

    private static char GetNormalToneMark(char tone)
    {
        // คืนค่า code point มาตรฐาน (0x0E48 - 0x0E4C)
        switch (tone)
        {
            case PUA_MAI_EK_UPPER:
            case PUA_MAI_EK_LEFT:
            case PUA_MAI_EK_UPPER_LEFT:       return MAI_EK;
            case PUA_MAI_THO_UPPER:
            case PUA_MAI_THO_LEFT:
            case PUA_MAI_THO_UPPER_LEFT:      return MAI_THO;
            case PUA_MAI_TRI_UPPER:
            case PUA_MAI_TRI_LEFT:
            case PUA_MAI_TRI_UPPER_LEFT:      return MAI_TRI;
            case PUA_MAI_CHATTAWA_UPPER:
            case PUA_MAI_CHATTAWA_LEFT:
            case PUA_MAI_CHATTAWA_UPPER_LEFT: return MAI_CHATTAWA;
            case PUA_THANTHAKHAT_UPPER:
            case PUA_THANTHAKHAT_LEFT:
            case PUA_THANTHAKHAT_UPPER_LEFT:  return THANTHAKHAT;
            default:                          return tone;
        }
    }

    private static char GetLeftUpperVowel(char vowel)
    {
        switch (vowel)
        {
            case MAI_HAN_AKAT: return PUA_MAI_HAN_AKAT_LEFT;
            case SARA_I:       return PUA_SARA_I_LEFT;
            case SARA_II:      return PUA_SARA_II_LEFT;
            case SARA_UE:      return PUA_SARA_UE_LEFT;
            case SARA_UEE:     return PUA_SARA_UEE_LEFT;
            case MAITAIKHU:    return PUA_MAITAIKHU_LEFT;
            case NIKHAHIT:     return PUA_NIKHAHIT_LEFT;
            default:           return vowel;
        }
    }

    private static char GetLowerVowelShifted(char vowel)
    {
        switch (vowel)
        {
            case SARA_U:   return PUA_SARA_U_LOWER;
            case SARA_UU:  return PUA_SARA_UU_LOWER;
            case PHINTHU:  return PUA_PHINTHU_LOWER;
            default:       return vowel;
        }
    }

    private static char GetPrevValidChar(string s, int index)
    {
        for (int i = index - 1; i >= 0; i--)
        {
            if (s[i] == '>')
            {
                int open = s.LastIndexOf('<', i);
                if (open >= 0) { i = open; continue; }
            }
            return s[i];
        }
        return '\0';
    }

    private static char GetPrevPrevValidChar(string s, int index)
    {
        int count = 0;
        for (int i = index - 1; i >= 0; i--)
        {
            if (s[i] == '>')
            {
                int open = s.LastIndexOf('<', i);
                if (open >= 0) { i = open; continue; }
            }
            count++;
            if (count == 2) return s[i];
        }
        return '\0';
    }

    private static char GetNextValidChar(string s, int index)
    {
        for (int i = index + 1; i < s.Length; i++)
        {
            if (s[i] == '<')
            {
                int close = s.IndexOf('>', i);
                if (close >= 0) { i = close; continue; }
            }
            return s[i];
        }
        return '\0';
    }

    #endregion
}

