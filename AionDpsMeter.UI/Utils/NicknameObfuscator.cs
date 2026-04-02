namespace AionDpsMeter.UI.Utils
{
    public static class NicknameObfuscator
    {
        public static string Mask(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            int len = name.Length;

            if (len <= 2)
                return new string('*', len);

            if (len <= 4)
                return name[0] + new string('*', len - 1);

            int discriminator = Math.Abs(GetStableHash(name)) % 10;
            int starCount = len - 2 - 1;         
            int digitPos = starCount / 2;
            var result = new char[len];
            result[0] = name[0];
            result[1] = name[1];
            int starIdx = 0;
            for (int i = 2; i < len; i++)
            {
                if (starIdx == digitPos)
                    result[i] = (char)('0' + discriminator);
                else
                    result[i] = '*';
                starIdx++;
            }
            return new string(result);
        }

        private static int GetStableHash(string s)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char c in s)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
                return (int)hash;
            }
        }
    }
}
