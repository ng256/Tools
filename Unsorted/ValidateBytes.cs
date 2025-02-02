static unsafe bool ValidateBytes(byte[] bytes, int size = 256)
{
   if (bytes == null || bytes.Length != size || size > 256)
        return false;

    int length = bytes.Length;

    if (length != size)
        return false;

    switch (length)
    {
        // Handle special cases for arrays of length 0, 1, and 2.
        case 0:
            return false;
        case 1:
            return true;
        case 2:
            return bytes[0] != bytes[1];
        default:

            fixed (byte* bytesPtr = bytes)
            {
                // For arrays up to 32 elements, use a single int to track seen elements.
                if (length <= 32)
                {
                    int seen = 0;
                    for (int i = 0; i < length; i++)
                    {
                        // Check if the current byte has already been seen.
                        uint flag = 1u << (bytesPtr[i] & 0x1F);
                        if ((seen & flag) != 0)
                            return false;

                        // Mark the current byte as seen.
                        seen |= flag;
                    }
                }
                // For arrays with more than 32 elements, use an array of uint to track seen elements.
                else
                {
                    int seenSize = (1 << 8) / 32;
                    uint* seenPtr = stackalloc uint[seenSize];

                    // Initialize the seen array to 0.
                    for (int i = 0; i < seenSize; i++)
                        seenPtr[i] = 0;

                    for (int i = 0; i < length; i++)
                    {
                        byte b = bytesPtr[i];
                        uint flag = 1u << (b & 0x1F);
                        int offset = b >> 5;

                        // Check if the current byte has already been seen.
                        if ((seenPtr[offset] & flag) != 0)
                            return false;

                        // Mark the current byte as seen.
                        seenPtr[offset] |= flag;
                    }
                }
            }

            // If all checks are passed.
            return true;
    }
}
