/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdesPacketUnitTests.cs                                   29 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;
using SipLib.Rtp;

[Trait("Category", "unit")]
public class SdesPacketUnitTests
{
    [Fact]
    public void SdesPacketParsing1()
    {
        SdesPacket Sp1 = BuildSdesPacket();
        byte[] Sp1Bytes = Sp1.ToByteArray();
        SdesPacket Sp2 = SdesPacket.Parse(Sp1Bytes, 0);
        Assert.True(Sp2 != null, "Sp2 is null");
        Assert.True(CompareSdesPackets(Sp1, Sp2) == true, "SdesPackets do not match");
    }

    public static SdesPacket BuildSdesPacket()
    {
        List<SdesItem> SdesItems = new List<SdesItem>();
        SdesItem Si1 = new SdesItem(SdesItemType.CNAME, "bsmith@somecompany.com");
        SdesItems.Add(Si1);
        SdesItem Si2 = new SdesItem(SdesItemType.NAME, "Bill");
        SdesItems.Add(Si2);
        SdesChunk SdesC1 = new SdesChunk(12345, SdesItems);

        List<SdesChunk> Chunks1 = new List<SdesChunk>();
        Chunks1.Add(SdesC1);

        // Add a second SDES chunk
        List<SdesItem> SdesItems2 = new List<SdesItem>();
        SdesItem Si3 = new SdesItem(SdesItemType.CNAME, "jdoe@anothercompany.com");
        SdesItems2.Add(Si3);
        SdesItem Si4 = new SdesItem(SdesItemType.NAME, "John");
        SdesItems2.Add(Si4);
        SdesChunk SdesC2 = new SdesChunk(6789, SdesItems2);
        Chunks1.Add(SdesC2);

        SdesPacket Sp1 = new SdesPacket(Chunks1);
        return Sp1;
    }

    public static bool CompareSdesPackets(SdesPacket Sp2, SdesPacket Sp1)
    {
        bool Success = true;
        Assert.True(Sp2.Header.Count == Sp1.Header.Count, "Header.Count mismatch");
        Assert.True(Sp2.Header.PacketType == Sp1.Header.PacketType, "PacketType mismatch");
        Assert.True(Sp2.Header.Length == Sp1.Header.Length, "Header.Length mismatch");
        Assert.True(Sp2.Chunks.Count == Sp1.Chunks.Count, "Chunks.Count mismatch");

        for (int i = 0; i < Sp2.Chunks.Count; i++)
        {
            Assert.True(Sp2.Chunks[i].Items.Count == Sp1.Chunks[i].Items.Count,
                $"Chunks[{i}].Items.Count mismatch");
            for (int j = 0; j < Sp2.Chunks[i].Items.Count; j++)
            {
                Assert.True(Sp2.Chunks[i].Items[j].ItemType == Sp1.Chunks[i].Items[j].ItemType,
                    $"Chunks[{i}].Items[{j}].ItemType mismatch");

                Assert.True(Sp2.Chunks[i].Items[j].Payload.Length == Sp1.Chunks[i].Items[j].Payload.Length,
                    $"Chunks[{i}].Items[{j}].Payload.Length mismatch");
                Assert.True(Sp2.Chunks[i].Items[j].Payload == Sp1.Chunks[i].Items[j].Payload,
                    $"Sp2.Chunks[{i}].Items[{j}].Payload mismatch");
            } // end for j
        } // end for i

        return Success;
    }
}
