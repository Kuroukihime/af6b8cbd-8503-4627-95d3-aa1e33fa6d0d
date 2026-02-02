using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AionDpsMeter.Services.Extensions
{
    public static class BinaryExtensions
    {
        extension(byte[] bytes)
        {
            public int ReadUInt32Le(int offset)
            {
                if (offset + 4 > bytes.Length) return 0;
                return bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
            }


            public (int Value, int Length) ReadVarInt(int offset = 0)
            {
                if (offset >= bytes.Length) return (-1, -1);

                int value = 0;
                int shift = 0;
                int count = 0;

                while (offset + count < bytes.Length)
                {
                    int b = bytes[offset + count];
                    count++;

                    value |= (b & 0x7F) << shift;

                    if ((b & 0x80) == 0)
                        return (value, count);

                    shift += 7;

                    if (shift >= 32)
                        return (-1, -1);
                }

                return (-1, -1);
            }

            public int IndexOfArray( byte[] bytesToFind, int offset = 0)
            {
                if (bytesToFind.Length == 0 || bytes.Length < bytesToFind.Length || offset >= bytes.Length)
                {
                    return -1; 
                }
                for (int i = offset; i <= bytes.Length - bytesToFind.Length; i++)
                {
                    bool found = true;
                    for (int j = 0; j < bytesToFind.Length; j++)
                    {
                        if (bytes[i + j] != bytesToFind[j])
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found)
                    {
                        return i; 
                    }
                }
                return -1; 
            }
        }
    }
}
