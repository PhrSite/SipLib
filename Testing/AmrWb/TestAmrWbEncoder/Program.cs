/////////////////////////////////////////////////////////////////////////////////////
//  File:   Program.cs                                              21 May 25 PHR
//
//  Description:    Test program for testing the AmrWb encoder using the test 
//                  vectors provided by the 3GPP organization.
/////////////////////////////////////////////////////////////////////////////////////

namespace TestAmrWbEncoder;
using AmrWbLib;

internal class Program
{
    private const short TX_FRAME_TYPE = 0x6b21;     // From bits.cs

    static void Main(string[] args)
    {
        //TestFile(1, 8, @"./testv/tst.inp", @"./testv/tst_m8.cod");
        //TestFile(1, 2, @"./testv/tst.inp", @"./testv/tst_m2.cod");

        for (short i=0; i <= 8; i++)
        {
            TestFile(1, i, "./testv/tst.inp", $"./testv/tst_m{i}.cod");
            Console.WriteLine();
        }
    }

    static void TestFile(short AllowDtx, short mode, string strInputFile, string strCorrectResultsCodingFile)
    {
        Console.WriteLine($"Mode: {mode}");
        if (File.Exists(strInputFile) == false)
        {
            Console.WriteLine($"Error: The input file {strInputFile} does not exist");
            return;
        }

        if (File.Exists(strCorrectResultsCodingFile) == false)
        {
            Console.WriteLine($"The results file: {strCorrectResultsCodingFile} does not exist.");
            return;
        }

        AmrWb amrWb = AmrWb.CreateAsEncoder(mode, AllowDtx);

        byte[] inputBytes = File.ReadAllBytes(strInputFile);
        MemoryStream inputMemoryStream = new MemoryStream(inputBytes);
        BinaryReader inputReader = new BinaryReader(inputMemoryStream);
        bool Done = false;
        short[]? SampleFrame = null;
        short[]? FrameParams = null;

        MemoryStream outputStream = new MemoryStream();
        BinaryWriter outputWriter = new BinaryWriter(outputStream);

        while (Done == false)
        {
            SampleFrame = ReadNextFrame(inputReader);
            if (SampleFrame == null)
                Done = true;
            else
            {
                FrameParams = amrWb.EncodeToPacketParams(SampleFrame);

                if (FrameParams != null)
                {
                    outputWriter.Write((short)TX_FRAME_TYPE);
                    outputWriter.Write((short)0);
                    outputWriter.Write(mode);

                    foreach (short prm in FrameParams)
                    {
                        outputWriter.Write(prm);
                    }
                }
                else
                {

                }
            }
        }

        outputWriter.Close();
        byte[] outputBytes = outputStream.ToArray();
        int MismatchCount = 0;
        int MaxErrors = 100;

        byte[] codingFileBytes = File.ReadAllBytes(strCorrectResultsCodingFile);
        if (outputBytes.Length != codingFileBytes.Length)
        {
            Console.WriteLine($"mode = {mode} -- outputBytes.Length = {outputBytes.Length}, codingFileBytes.Length = {codingFileBytes.Length}");
        }
        else
        {
            for (int i=0; i < outputBytes.Length; i++)
            {
                if (outputBytes[i] != codingFileBytes[i])
                {
                    MismatchCount += 1;
                    if (MismatchCount <= MaxErrors)
                    {
                        Console.WriteLine($"i = {i}, out = {outputBytes[i].ToString("X02")}, coding = {codingFileBytes[i].ToString("X02")}");
                    }
                }
            }
        }

        Console.WriteLine($"Total Errors = {MismatchCount}");

        outputStream.Close();
        inputMemoryStream.Close();
    }

    private const int FRAME_LENGTH = 320;

    private static short[]? ReadNextFrame(BinaryReader reader)
    {
        short[] inputFrame = new short[FRAME_LENGTH];
        int i = 0;
        try
        {
            for (i=0; i < FRAME_LENGTH; i++)
            {
                inputFrame[i] = reader.ReadInt16();
            }
        }
        catch (EndOfStreamException)
        {
            return null;
        }

        return inputFrame;
    }
}
