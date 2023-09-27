/////////////////////////////////////////////////////////////////////////////////////
//  File:   AesFunctions.cs                                         20 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Security.Cryptography;

namespace SipLib.RtpCrypto
{
    /// <summary>
    /// This class contains static fuctions for performing AES encryption and decryption operations for SRTP
    /// and SRTCP.
    /// </summary>
    public static class AesFunctions
    {
        /// <summary>
        /// Encrypts or decrypts a block of bytes using AES in Counter Mode. RFC 3711 defines the 
        /// AES Counter Mode algorithm for SRTP and SRTCP.
        /// </summary>
        /// <remarks>This function can be used to either encrypt an input array or to produce a key stream
        /// which can then be used to encrypt some data.To encrypt data, pass the data into the Input array.
        /// <para>
        /// To generate a key stream, pass in an array of 0s in the Input array and the Output array will
        /// contain the key stream.. Then XOR the key stream in the Output array with the data to be encrypted.
        /// </para>
        /// <para>
        /// The encryption and decryption operations are symetric. To encrypt a packet, pass in the plain
        /// text as the input. To decrypt a packet, pass in the encrypted packet as the input.
        /// </para>
        /// </remarks>
        /// <param name="key">Encryption key. The array length must be a valid length for the AES algorithm.
        /// Valid lengths are: 16 bytes (AES-128), 24 bytes (AES-192) or 32 bytes (AES-256).
        /// </param>
        /// <param name="salt">Salt value to use. This must be 16 bytes (128 bits) long.</param>
        /// <param name="Input">Input byte array to encrypt or decrypt.</param>
        /// <param name="Output">Output encrypted or decrypted byte array. Must be the same length or longer
        /// than the Input input array.</param>
        // <exception cref="ArgumentException">Thrown if the Output array is shorter than the Input array.
        // </exception>
        public static void AesCounterModeTransform(byte[] key, byte[] salt, byte[] Input, byte[] Output)
        {
            SymmetricAlgorithm aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            int NumInputBytes = Input.Length;
            int blockSizeInBytes = aes.BlockSize / 8;

            if (Output.Length < Input.Length)
                throw new ArgumentException("The Output array is shorter than the Input " +
                    $"array. Input Length = {Input.Length}, Output Length = {Output.Length}");

            if (salt.Length != blockSizeInBytes)
                throw new ArgumentException("The Salt size must be same as the encryption block size " +
                    $"(actual: {salt.Length}, expected: {blockSizeInBytes})");

            byte[] counter = new byte[salt.Length];
            salt.CopyTo(counter, 0);

            byte[] zeroIv = new byte[blockSizeInBytes];
            int xorMaskIdx = blockSizeInBytes;
            // The xorMask is the key stream.
            byte[] xorMask = new byte[blockSizeInBytes];
            ICryptoTransform counterEncryptor = aes.CreateEncryptor(key, zeroIv);

            byte b;
            for (int Idx = 0; Idx < Input.Length; Idx++)
            {
                b = Input[Idx];
                if (xorMaskIdx >= blockSizeInBytes)
                {
                    counterEncryptor.TransformBlock(counter, 0, counter.Length, xorMask, 0);
                    xorMaskIdx = 0;

                    // Increments the salt (IV) -- Counter Mode
                    for (int i2 = counter.Length - 1; i2 >= 0; i2--)
                    {
                        if (++counter[i2] != 0)
                            break;
                    }
                }

                Output[Idx] = (byte)(b ^ xorMask[xorMaskIdx++]);
            } // end for Idx
        }

        /// <summary>
        /// Encrypts or decrypts a block of bytes using AES in Counter Mode. RFC 3711 defines the 
        /// AES Counter Mode algorithm for SRTP and SRTCP.
        /// </summary>
        /// <remarks>This function can be used to either encrypt an input array or to produce a key stream
        /// which can then be used to encrypt some data.To encrypt data, pass the data into the Input array.
        /// <para>
        /// To generate a key stream, pass in an array of 0s in the Input array and the Output array will
        /// contain the key stream.. Then XOR the key stream in the Output array with the data to be encrypted.
        /// </para>
        /// <para>
        /// The encryption and decryption operations are symetric. To encrypt a packet, pass in the plain
        /// text as the input. To decrypt a packet, pass in the encrypted packet as the input.
        /// </para>
        /// </remarks>
        /// <param name="key">Encryption key. The array length must be a valid length for the AES algorithm.
        /// Valid lengths are: 16 bytes (AES-128), 24 bytes (AES-192) or 32 bytes (AES-256).
        /// </param>
        /// <param name="salt">Salt value to use. This must be 16 bytes (128 bits) long.</param>
        /// <param name="Input">Input byte array to encrypt or decrypt.</param>
        /// <param name="StartIdx">Starting index in the Input array.</param>
        /// <param name="NumInputBytes">Number of bytes in the input array to process.</param>
        /// <param name="Output">Output encrypted or decrypted byte array. Must be the same length or longer
        /// than NumInputBytes</param>
        // <exception cref="ArgumentException">Thrown if the Output array is shorter than the number of bytes
        // to process in the Input array.</exception>
        public static void AesCounterModeTransform(byte[] key, byte[] salt, byte[] Input, int StartIdx, 
            int NumInputBytes, byte[] Output)
        {
            SymmetricAlgorithm aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            int blockSizeInBytes = aes.BlockSize / 8;

            if (Output.Length < NumInputBytes)
                throw new ArgumentException("The Output array is shorter than the number of bytes in the " +
                    $"Input array. NumInputBytes = {NumInputBytes}, Output Length = {Output.Length}");

            if (salt.Length != blockSizeInBytes)
            {
                throw new ArgumentException("The Salt size must be same as the encryption block size " +
                    $"(actual: {salt.Length}, expected: {blockSizeInBytes})");
            }

            byte[] counter = new byte[salt.Length];
            salt.CopyTo(counter, 0);

            var zeroIv = new byte[blockSizeInBytes];
            int xorMaskIdx = blockSizeInBytes;
            // The xorMask is the key stream.
            byte[] xorMask = new byte[blockSizeInBytes];
            ICryptoTransform counterEncryptor = aes.CreateEncryptor(key, zeroIv);

            byte b;
            int EndIdx = StartIdx + NumInputBytes;
            int OutIdx = 0;
            for (int Idx = StartIdx; Idx < EndIdx; Idx++)
            {
                b = Input[Idx];
                if (xorMaskIdx >= blockSizeInBytes)
                {
                    counterEncryptor.TransformBlock(counter, 0, counter.Length, xorMask, 0);
                    xorMaskIdx = 0;

                    // Increments the salt (IV) -- Counter Mode
                    for (int i2 = counter.Length - 1; i2 >= 0; i2--)
                    {
                        if (++counter[i2] != 0)
                            break;
                    }
                }

                Output[OutIdx++] = (byte)(b ^ xorMask[xorMaskIdx++]);
            } // end for Idx
        }

        /// <summary>
        /// Encrypts or Decrypts a block of bytes using the AES in F8 mode. Section 4.1.2.1 of RFC 3711
        /// specifies the AES in F8 mode algorithm. This algorithm only supports AES-128.
        /// </summary>
        /// <remarks>This function can be used to either encrypt an input array or to produce a key stream
        /// which can then be used to encrypt some data.To encrypt data, pass the data into the Input array.
        /// <para>
        /// To generate a key stream, pass in an array of 0s in the Input array and the Output array will
        /// contain the key stream.. Then XOR the key stream in the Output array with the data to be encrypted.
        /// </para>
        /// <para>
        /// The encryption and decryption operations are symetric. To encrypt a packet, pass in the plain
        /// text as the input. To decrypt a packet, pass in the encrypted packet as the input.
        /// </para>
        /// </remarks>
        /// <param name="k_e">Encryption key. Must be 16 bytes (128 bits) long.</param>
        /// <param name="k_s">Salt value to use.</param>
        /// <param name="IV">Initialization Vector (IV) to use. Must be the same length as the k_e input 
        /// array.</param>
        /// <param name="Input">Input byte array to encrypt or decrypt.</param>
        /// <param name="Output">Output encrypted or decrypted byte array. Must be the same length or longer
        /// than the Input input array.</param>
        // <exception cref="ArgumentException">Thrown if the Output array is shorter than the Input array.</exception>
        // <exception cref="ArgumentException">Thrown if the input key or the IV is not the same length as the
        // encryption block size.</exception>
        public static void AesF8ModeTransform(byte[] k_e, byte[] k_s, byte[] IV, byte[] Input, byte[] Output)
        {
            if (Output.Length < Input.Length)
                throw new ArgumentException("The Output array is shorter than the Input array. " +
                    $"Input Length = {Input.Length}, Output Length = {Output.Length}");

            SymmetricAlgorithm aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            int blockSizeInBytes = aes.BlockSize / 8;
            if (k_e.Length != blockSizeInBytes)
                throw new ArgumentException("The key size must be same as the encryption block size " +
                    $"(actual: {k_e.Length}, expected: {blockSizeInBytes})");

            if (IV.Length != blockSizeInBytes)
                throw new ArgumentException("The IV size must be same as the encryption block size " +
                    $"(actual: {k_e.Length}, expected: {blockSizeInBytes})");

            byte[] ZeroArray = new byte[k_e.Length];
            int NumInputBytes = Input.Length;
            var zeroIv = new byte[blockSizeInBytes];

            byte[] m = new byte[SRtpUtils.n_eB];
            Array.Copy(k_s, m, k_s.Length);
            int i;
            for (i = k_s.Length; i < m.Length; i++)
                m[i] = 0x55;

            for (i = 0; i < m.Length; i++)
                m[i] = (byte)(m[i] ^ k_e[i]);

            byte[] IV_Prime = new byte[k_e.Length];
            AesCounterModeTransform(m, IV, ZeroArray, IV_Prime);

            byte[] Temp = new byte[k_e.Length];
            byte[] j = new byte[k_e.Length];    // This is the counter array j
            byte[] S = new byte[k_e.Length];    // This is the key stream array
            // Do the first transform to get the initial key stream S.
            AesCounterModeTransform(k_e, IV_Prime, ZeroArray, S);

            int S_Idx = 0;
            for (int Idx = 0; Idx < NumInputBytes; Idx++)
            {
                Output[Idx] = (byte)(Input[Idx] ^ S[S_Idx++]);

                if (S_Idx >= S.Length)
                {   // Generate the next keystream
                    S_Idx = 0;

                    // Increment the j counter array
                    for (i = j.Length - 1; i >= 0; i--)
                    {
                        if (++j[i] != 0)
                            break;
                    }

                    for (i = 0; i < j.Length; i++)
                        Temp[i] = (byte)(j[i] ^ S[i] ^ IV_Prime[i]);

                    AesCounterModeTransform(k_e, Temp, ZeroArray, S);
                }
            } // end for Idx
        }

        /// <summary>
        /// Encrypts or Decrypts a block of bytes using the AES in F8 mode. Section 4.1.2.1 of RFC 3711
        /// specifies the AES in F8 mode algorithm. This algorithm only supports AES-128.
        /// </summary>
        /// <remarks>This function can be used to either encrypt an input array or to produce a key stream
        /// which can then be used to encrypt some data.To encrypt data, pass the data into the Input array.
        /// <para>
        /// To generate a key stream, pass in an array of 0s in the Input array and the Output array will
        /// contain the key stream.. Then XOR the key stream in the Output array with the data to be encrypted.
        /// </para>
        /// <para>
        /// The encryption and decryption operations are symetric. To encrypt a packet, pass in the plain
        /// text as the input. To decrypt a packet, pass in the encrypted packet as the input.
        /// </para>
        /// </remarks>
        /// <param name="k_e">Encryption key. Must be 16 bytes (128 bits) long.</param>
        /// <param name="k_s">Salt value to use.</param>
        /// <param name="IV">Initialization Vector (IV) to use. Must be the same length as the k_e input 
        /// array.</param>
        /// <param name="Input">Input byte array to encrypt or decrypt.</param>
        /// <param name="StartIdx">The index in the Input array to start processing at.</param>
        /// <param name="NumInputBytes">The number of bytes in the Input array to process.</param>
        /// <param name="Output">Output encrypted or decrypted byte array. Must be the same length or longer
        /// than the Input input array.</param>
        // <exception cref="ArgumentException">Thrown if the Output array is shorter than the  number of bytes
        // to process in the Input array.</exception>
        // <exception cref="ArgumentException">Thrown if the input key or the IV is not the same length as the
        // encryption block size.</exception>
        public static void AesF8ModeTransform(byte[] k_e, byte[] k_s, byte[] IV, 
            byte[] Input, int StartIdx, int NumInputBytes, byte[] Output)
        {
            if (Output.Length < NumInputBytes)
                throw new ArgumentException("The Output array is shorter than the Input array. " +
                    $"Input Length = {Input.Length}, Output Length = {Output.Length}");

            SymmetricAlgorithm aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            int blockSizeInBytes = aes.BlockSize / 8;
            if (k_e.Length != blockSizeInBytes)
                throw new ArgumentException("The key size must be same as the encryption block size " +
                    $"(actual: {k_e.Length}, expected: {blockSizeInBytes})");

            if (IV.Length != blockSizeInBytes)
                throw new ArgumentException("The IV size must be same as the encryption block size " +
                    $"(actual: {k_e.Length}, expected: {blockSizeInBytes})");

            byte[] ZeroArray = new byte[k_e.Length];
            byte[] zeroIv = new byte[blockSizeInBytes];

            byte[] m = new byte[SRtpUtils.n_eB];
            Array.Copy(k_s, m, k_s.Length);
            int i;
            for (i = k_s.Length; i < m.Length; i++)
                m[i] = 0x55;

            for (i = 0; i < m.Length; i++)
                m[i] = (byte)(m[i] ^ k_e[i]);

            byte[] IV_Prime = new byte[k_e.Length];
            AesCounterModeTransform(m, IV, ZeroArray, IV_Prime);

            byte[] Temp = new byte[k_e.Length];
            byte[] j = new byte[k_e.Length];    // This is the counter array j
            byte[] S = new byte[k_e.Length];    // This is the key stream array
            // Do the first transform to get the initial key stream S.
            AesCounterModeTransform(k_e, IV_Prime, ZeroArray, S);

            int S_Idx = 0;
            int OutIdx = 0;
            int EndIdx = StartIdx + NumInputBytes;
            for (int Idx = StartIdx; Idx < EndIdx; Idx++)
            {
                Output[OutIdx++] = (byte)(Input[Idx] ^ S[S_Idx++]);

                if (S_Idx >= S.Length)
                {   // Generate the next keystream
                    S_Idx = 0;

                    // Increment the j counter array
                    for (i = j.Length - 1; i >= 0; i--)
                    {
                        if (++j[i] != 0)
                            break;
                    }

                    for (i = 0; i < j.Length; i++)
                        Temp[i] = (byte)(j[i] ^ S[i] ^ IV_Prime[i]);

                    AesCounterModeTransform(k_e, Temp, ZeroArray, S);
                }
            } // end for Idx
        }
    }
}
