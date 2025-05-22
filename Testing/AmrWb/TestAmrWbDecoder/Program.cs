/////////////////////////////////////////////////////////////////////////////////////
//  File:   Program.cs                                              21 May 25 PHR
//
//  Description:    Test program for testing the AmrWb decoder using the test
//                  files provided by the 3GPP organization.
/////////////////////////////////////////////////////////////////////////////////////

namespace TestAmrWbDecoder;
using AmrWbLib;

internal class Program
{
    //private const string InputCodFile = "./testv/tst_m0.cod";
    //private const string OutFile = "./testv/tst_m0.out";
    private const int ErrorThreshold = 0;
    private const int MaxDisplayErrors = 1000;

    static void Main(string[] args)
    {
        //TestFile(InputCodFile, OutFile);
        for (int i=0; i <= 8; i++)
        {
            TestFile($"./testv/tst_m{i}.cod", $"./testv/tst_m{i}.out");
        }
    }

    private static void TestFile(string InputCodFile, string CorrectResultsFile)
    {
        Console.WriteLine($"Input File: {InputCodFile}");
        if (File.Exists(InputCodFile) == false)
        {
            Console.WriteLine($"{InputCodFile} does not exist");
            return;
        }    

        if (File.Exists(CorrectResultsFile) == false)
        {
            Console.WriteLine($"The correct results file: {CorrectResultsFile} does not exist");
            return;
        }

        AmrWb amrWb = AmrWb.CreateAsDecoder();
        List<short> SamplesList = amrWb.DecodeCodFile(InputCodFile);

        if (SamplesList.Count == 0)
        {
            Console.WriteLine("No samples decoded");
            return;
        }

        byte[] OutFileBytes = File.ReadAllBytes(CorrectResultsFile);
        MemoryStream outStream = new MemoryStream(OutFileBytes);
        BinaryReader outReader = new BinaryReader(outStream);
        List<short> outSamplesList = new List<short>();

        bool Done = false;
        while (Done == false)
        {
            try
            {
                outSamplesList.Add(outReader.ReadInt16());
            }
            catch (EndOfStreamException)
            {
                Done = true;
            }
        }

        outStream.Dispose();
        outReader.Dispose();

        if (SamplesList.Count != outSamplesList.Count)
        {
            Console.WriteLine($"Error: Results length mismatch: Samples count = {SamplesList.Count}, Out samples count = {outSamplesList.Count}");
            return;
        }

        int TotalErrors = 0;
        int AbsError = 0;
        Dictionary<int, int> ErrorDistribution = new Dictionary<int, int>();
        for (int i = 0; i < SamplesList.Count; i++)
        {
            if (SamplesList[i] != outSamplesList[i])
            {
                AbsError = Math.Abs(SamplesList[i] - outSamplesList[i]);
                if (ErrorDistribution.ContainsKey(AbsError) == false)
                    ErrorDistribution.Add(AbsError, 1);
                else
                    ErrorDistribution[AbsError] = ErrorDistribution[AbsError] + 1;

                if (AbsError > ErrorThreshold)
                {
                    TotalErrors += 1;
                    if (TotalErrors < MaxDisplayErrors)
                    {
                        Console.WriteLine($"i = {i} Samples = {SamplesList[i].ToString()}, Out = {outSamplesList[i].ToString()}");
                    }
                }
            }
        }

        Console.WriteLine($"Total Errors = {TotalErrors}");
        Console.WriteLine();
        foreach (KeyValuePair<int, int> kvp in ErrorDistribution)
        {
            Console.WriteLine($"AbsError: {kvp.Key}, Count = {kvp.Value}");
        }

    }
}
