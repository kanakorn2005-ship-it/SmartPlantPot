using System.Text;

namespace SmartPlantPot
{
    /// <summary>
    /// Unity's legacy Text has no Thai shaping, so a tone mark stacked on an upper vowel
    /// (กิ่ง, ที่, เรื่อง, น้ำ) draws on top of it. This swaps those marks for the font's own raised /
    /// left-shifted variants, which the bundled Prompt fonts expose at private-use code points.
    /// Only use with the patched fonts in Assets/SmartPlantPot/Fonts.
    /// </summary>
    public static class ThaiShaper
    {
        /// <summary>Turn off if you swap in a font without the PUA mapping.</summary>
        public static bool Enabled = true;

        const char SaraAm = 'ำ';

        static bool IsTone(char c)
        {
            return c >= '่' && c <= '์';
        }

        static bool IsUpperVowel(char c)
        {
            return c == 'ั' || (c >= 'ิ' && c <= 'ื') || c == '็' || c == 'ํ' ||
                   c == '' || (c >= '' && c <= '') || c == '' || c == '';
        }

        static bool IsTallConsonant(char c)
        {
            return c == 'ป' || c == 'ฝ' || c == 'ฟ' || c == 'ฬ';
        }

        static char RaisedTone(char c)
        {
            return (char)('' + (c - '่'));
        }

        static char LeftTone(char c)
        {
            return (char)('' + (c - '่'));
        }

        static char LeftUpperVowel(char c)
        {
            switch (c)
            {
                case 'ั': return '';
                case 'ิ': return '';
                case 'ี': return '';
                case 'ึ': return '';
                case 'ื': return '';
                case '็': return '';
                case 'ํ': return '';
                default: return c;
            }
        }

        public static string Fix(string s)
        {
            if (!Enabled || string.IsNullOrEmpty(s)) return s;

            bool hasThaiMark = false;
            foreach (char c in s)
            {
                if (c >= 'ั' && c <= 'ํ')
                {
                    hasThaiMark = true;
                    break;
                }
            }
            if (!hasThaiMark) return s;

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                char prev = i > 0 ? sb[sb.Length - 1] : '\0';
                char next = i + 1 < s.Length ? s[i + 1] : '\0';

                if (IsTone(c))
                {
                    if (IsUpperVowel(prev) || next == SaraAm) c = RaisedTone(c);
                    else if (IsTallConsonant(prev)) c = LeftTone(c);
                }
                else if (IsTallConsonant(prev) && LeftUpperVowel(c) != c)
                {
                    c = LeftUpperVowel(c);
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
