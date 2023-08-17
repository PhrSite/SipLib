/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpStreamParserUnitTests.cs                            28 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;

using SipLib.Msrp;

[Trait("Category", "unit")]
public class MsrpStreamParserUnitTests
{
    /// <summary>
    /// Specifies the path to the files containing the test SIP messages. Change this if the project
    /// location or the location of the test files change.
    /// </summary>
    private const string Path = @"..\..\..\MsrpMessages\";

    [Fact]
    public void MsrpRequestMessage1()
    {
        byte[] MsgBytes = GetTestFile("MsrpRequestMessage1.txt");
        MsrpStreamParser parser = new MsrpStreamParser(10000);
        int i;
        bool FullMessage = false;
        for (i = 0; i < MsgBytes.Length; i++)
        {
            FullMessage = parser.ProcessByte(MsgBytes[i]);
            if (FullMessage == true)
                break;
        }

        Assert.True(FullMessage == true, "FullMessage is false");

        byte[] Message = parser.GetMessageBytes();
        Assert.True(MsgBytes.Length == Message.Length, "The Length is wrong");

        bool Match = true;
        for (i = 0; (i < Message.Length && Match == true); i++)
        {
            if (MsgBytes[i] != Message[i])
                Match = false;
        }

        Assert.True(Match == true, "Match is false");
    }

    [Fact]
    public void TwoMessagesBackToBack()
    {
        MsrpStreamParser parser = new MsrpStreamParser(10000);
        byte[] MsgBytes1 = GetTestFile("MsrpRequestMessage1.txt");
        byte[] MsgBytes2 = GetTestFile("MsrpPositiveReport1.txt");
        MemoryStream Ms = new MemoryStream();
        Ms.Write(MsgBytes1, 0, MsgBytes1.Length);
        Ms.Write(MsgBytes2 , 0, MsgBytes2.Length);
        List<byte[]> Messages = new List<byte[]>();
        byte[] streamBytes = Ms.ToArray();

        int i;
        bool FullMessage = false;
        for (i = 0; i < streamBytes.Length; i++)
        {
            FullMessage = parser.ProcessByte(streamBytes[i]);
            if (FullMessage == true)
                Messages.Add(parser.GetMessageBytes());
        }

        Assert.True(Messages.Count == 2, "Messages.Count is wrong");
        Assert.True(Messages[0].Length == MsgBytes1.Length, "The first message length is wrong");
        Assert.True(Messages[1].Length == MsgBytes2.Length, "The second message length is wrong");
    }

    [Fact]
    public void TwoMessagesWithRandomCharactersInStream()
    {
        MsrpStreamParser parser = new MsrpStreamParser(10000);
        byte[] MsgBytes1 = GetTestFile("MsrpRequestMessage1.txt");
        byte[] MsgBytes2 = GetTestFile("MsrpPositiveReport1.txt");
        MemoryStream Ms = new MemoryStream();
        Ms.Write(MsgBytes1, 0, MsgBytes1.Length);

        // Insert some random bytes between the messages
        Random Rnd = new Random();
        byte[] rndBytes = new byte[256];
        Rnd.NextBytes(rndBytes);
        Ms.Write(rndBytes, 0, rndBytes.Length);

        Ms.Write(MsgBytes2, 0, MsgBytes2.Length);
        List<byte[]> Messages = new List<byte[]>();
        byte[] streamBytes = Ms.ToArray();

        int i;
        bool FullMessage = false;
        for (i = 0; i < streamBytes.Length; i++)
        {
            FullMessage = parser.ProcessByte(streamBytes[i]);
            if (FullMessage == true)
                Messages.Add(parser.GetMessageBytes());
        }

        Assert.True(Messages.Count == 2, "Messages.Count is wrong");
        Assert.True(Messages[0].Length == MsgBytes1.Length, "The first message length is wrong");
        Assert.True(Messages[1].Length == MsgBytes2.Length, "The second message length is wrong");
    }

    private byte[] GetTestFile(string FileName)
    {
        byte[] FileBytes = null;
        string FilePath = $"{Path}{FileName}";
        Assert.True(File.Exists(FilePath), $"The {FileName} test input file was missing.");
        FileBytes = File.ReadAllBytes(FilePath);
        return FileBytes;
    }
}
