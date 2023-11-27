//-----------------------------------------------------------------------------
// Filename: RawPacket.cs
//
// TODO: This class should be replaced by the existing RTP packet implementation
// in src/net/RTP.
//
// Description: See below.
//
// Derived From:
// https://github.com/RestComm/media-core/blob/master/rtp/src/main/java/org/restcomm/media/core/rtp/crypto/RawPacket.java
//
// Author(s):
// Rafael Soares (raf.csoares@kyubinteractive.com)
//
// History:
// 01 Jul 2020	Rafael Soares   Created.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
// Original Source: AGPL-3.0 License
//-----------------------------------------------------------------------------

//  Revised: 27 Nov 23 PHR
//      -- Changed namespace to SipLib.Dtls from SIPSorcery.Net
//      -- Added documentation comments and code cleanup

/*
* @author Werner Dittmann (Werner.Dittmann@t-online.de)
* @author Bing SU (nova.su@gmail.com)
* @author Emil Ivov
* @author Damian Minkov
* @author Boris Grozev
* @author Lyubomir Marinov
*/

namespace SipLib.Dtls;

/// <summary>
/// When using a TransformConnector, a RTP/RTCP packet is represented using RawPacket. RawPacket stores the buffer 
/// holding the RTP/RTCP packet, as well as the inner offset and length of RTP/RTCP packet data.
/// 
/// After transformation, data is also stored in RawPacket objects, either the original RawPacket (in place
/// transformation), or a newly created RawPacket.
/// </summary>
public class RawPacket
{
    /// <summary>
    /// Maximum RTP packet size
    /// </summary>
    public const int RTP_PACKET_MAX_SIZE = 8192;

    /// <summary>
    /// The size of the extension header as defined by RFC 3550.
    /// </summary>
    public const int EXT_HEADER_SIZE = 4;

    /// <summary>
    /// The size of the fixed part of the RTP header as defined by RFC 3550.
    /// </summary>
    public const int FIXED_HEADER_SIZE = 12;

    /// <summary>
    /// Byte array storing the content of this Packet
    /// </summary>
    private MemoryStream buffer;

    /// <summary>
    /// Initializes a new empty RawPacket instance.
    /// </summary>
    public RawPacket()
    {
        this.buffer = new MemoryStream(RTP_PACKET_MAX_SIZE);
    }

    /// <summary>
    /// Initializes a new RawPacket instance with a specific byte array buffer.
    /// </summary>
    /// <param name="data">The byte array to be the buffer of the new instance</param>
    /// <param name="offset">The offset in <tt>buffer</tt> at which the actual data to be represented by the new
    /// instance starts</param>
    /// <param name="length">The number of bytes in buffer which constitute the actual data to
    /// be represented by the new instance</param>
    public RawPacket(byte[] data, int offset, int length)
    {
        this.buffer = new MemoryStream(RTP_PACKET_MAX_SIZE);
        Wrap(data, offset, length);
    }

    /// <summary>
    /// Wraps the data into raw packet for readable format
    /// </summary>
    /// <param name="data">Data to write into the raw packet</param>
    /// <param name="offset">Offset into the raw packet</param>
    /// <param name="length">Number of bytes to write</param>
    public void Wrap(byte[] data, int offset, int length)
    {
        this.buffer.Position = 0;
        this.buffer.Write(data, offset, length);
        this.buffer.SetLength(length - offset);
        this.buffer.Position = 0;
    }

    /// <summary>
    /// Gets the data in the RawPacket
    /// </summary>
    /// <returns>Returns a byte array containing the data.</returns>
    public byte[] GetData()
    {
        this.buffer.Position = 0;
        byte[] data = new byte[this.buffer.Length];
        this.buffer.Read(data, 0, data.Length);
        return data;
    }

    /// <summary>
    /// Append a byte array to the end of the packet. This may change the data buffer of this packet.
    /// </summary>
    /// <param name="data">Data byte array to append</param>
    /// <param name="len">The number of bytes to append</param>
    // <exception cref="System.Exception">Thrown if the input parameters a invalid</exception>
    public void Append(byte[] data, int len)
    {
        if (data == null || len <= 0 || len > data.Length)
        {
            throw new System.Exception("Invalid combination of parameters data and length to append()");
        }

        long oldLimit = buffer.Length;
        // grow buffer if necessary
        Grow(len);
        // set positing to begin writing immediately after the last byte of the current buffer
        buffer.Position = oldLimit;
        // set the buffer limit to exactly the old size plus the new appendix length
        buffer.SetLength(oldLimit + len);
        // append data
        buffer.Write(data, 0, len);
    }

    /// <summary>
    /// Gets the buffer containing the content of this packet
    /// </summary>
    /// <returns></returns>
    public MemoryStream GetBuffer()
    {
        return this.buffer;
    }

    /// <summary>
    /// Returns true if the extension bit of this packet has been set and false otherwise.
    /// </summary>
    /// <returns></returns>
    public bool GetExtensionBit()
    {
        buffer.Position = 0;
        return (buffer.ReadByte() & 0x10) == 0x10;
    }

    /// <summary>
    /// Returns the length of the extensions currently added to this packet.
    /// </summary>
    /// <returns></returns>
    public int GetExtensionLength()
    {
        int length = 0;
        if (GetExtensionBit())
        {
            // the extension length comes after the RTP header, the CSRC list,
            // and after two bytes in the extension header called "defined by profile"
            int extLenIndex = FIXED_HEADER_SIZE + GetCsrcCount() * 4 + 2;
            buffer.Position = extLenIndex;
            int byteLength = (buffer.ReadByte() << 8);
            int byteLength2 = buffer.ReadByte();

            length = (byteLength | byteLength2 * 4);
        }
        return length;
    }

    /// <summary>
    /// Returns the number of CSRC identifiers currently included in this packet.
    /// </summary>
    /// <returns></returns>
    public int GetCsrcCount()
    {
        this.buffer.Position = 0;
        return (this.buffer.ReadByte() & 0x0f);
    }

    /// <summary>
    /// Gets RTP header length from a RTP packet
    /// </summary>
    /// <returns></returns>
    public int GetHeaderLength()
    {
        int length = FIXED_HEADER_SIZE + 4 * GetCsrcCount();
        if (GetExtensionBit())
        {
            length += EXT_HEADER_SIZE + GetExtensionLength();
        }
        return length;
    }

    /// <summary>
    /// Get the length of this packet's data
    /// </summary>
    /// <returns></returns>
    public int GetLength()
    {
        return (int)this.buffer.Length;
    }

    /// <summary>
    /// Get RTP padding size from a RTP packet
    /// </summary>
    /// <returns></returns>
    public int GetPaddingSize()
    {
        buffer.Position = 0;
        if ((this.buffer.ReadByte() & 0x20) == 0)
        {
            return 0;
        }
        buffer.Position = this.buffer.Length - 1;
        return this.buffer.ReadByte();
    }

    /// <summary>
    /// Gets the RTP payload (bytes) of this RTP packet.
    /// </summary>
    /// <returns></returns>
    public byte[] GetPayload()
    {
        return ReadRegion(GetHeaderLength(), GetPayloadLength());
    }

    /// <summary>
    /// Gets the RTP payload length from a RTP packet
    /// </summary>
    /// <returns></returns>
    public int GetPayloadLength()
    {
        return GetLength() - GetHeaderLength();
    }

    /// <summary>
    /// Gets the RTP payload type from a RTP packet
    /// </summary>
    /// <returns></returns>
    public byte GetPayloadType()
    {
        buffer.Position = 1;
        return (byte)(this.buffer.ReadByte() & (byte)0x7F);
    }

    /// <summary>
    /// Gets the RTCP SSRC from a RTCP packet
    /// </summary>
    /// <returns></returns>
    public int GetRTCPSSRC()
    {
        return ReadInt(4);
    }

    /// <summary>
    /// Gets the RTP sequence number from a RTP packet
    /// </summary>
    /// <returns></returns>
    public int GetSequenceNumber()
    {
        return ReadUnsignedShortAsInt(2);
    }

    /// <summary>
    /// Gets the SRTCP sequence number from a SRTCP packet
    /// </summary>
    /// <param name="authTagLen"></param>
    /// <returns></returns>
    public int GetSRTCPIndex(int authTagLen)
    {
        int offset = GetLength() - (4 + authTagLen);
        return ReadInt(offset);
    }

    /// <summary>
    /// Get RTP SSRC from a RTP packet
    /// </summary>
    /// <returns></returns>
    public int GetSSRC()
    {
        return ReadInt(8);
    }

    /// <summary>
    /// Returns the timestamp for this RTP RawPacket.
    /// </summary>
    /// <returns></returns>
    public long GetTimestamp()
    {
        return ReadInt(4);
    }

    /// <summary>
    /// Grow the internal packet buffer.
    /// 
    /// This will change the data buffer of this packet but not the length of the valid data.Use this to grow
    /// the internal buffer to avoid buffer re-allocations when appending data.
    /// </summary>
    /// <param name="delta">Number of bytes to grow</param>
    public void Grow(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        long newLen = buffer.Length + delta;
        if (newLen <= buffer.Capacity)
        {
            // there is more room in the underlying reserved buffer memory
            buffer.SetLength(newLen);
            return;
        }
        else
        {
            // create a new bigger buffer
            MemoryStream newBuffer = new MemoryStream();
            buffer.Position = 0;
            newBuffer.Write(buffer.GetBuffer(), 0, (int)buffer.Length);
            newBuffer.SetLength(newLen);
            // switch to new buffer
            buffer = newBuffer;
        }
    }
 
    /// <summary>
    /// Reads an integer from this packet at specified offset
    /// </summary>
    /// <param name="off">Offset to the integer to be read</param>
    /// <returns></returns>
    public int ReadInt(int off)
    {
        buffer.Position = off;
        return ((buffer.ReadByte() & 0xff) << 24) |
                ((buffer.ReadByte() & 0xff) << 16) |
                ((buffer.ReadByte() & 0xff) << 8) |
                ((buffer.ReadByte() & 0xff));
    }

    /// <summary>
    /// Reads a byte region from a specified offset with a specified length
    /// </summary>
    /// <param name="off">Offset to the region to be read</param>
    /// <param name="len">Length of the region to be read</param>
    /// <returns></returns>
    public byte[] ReadRegion(int off, int len)
    {
        this.buffer.Position = 0;
        if (off < 0 || len <= 0 || off + len > this.buffer.Length)
        {
            return null;
        }

        byte[] region = new byte[len];
        this.buffer.Read(region, off, len);
        return region;
    }

    /// <summary>
    /// Reads a byte region from a specified offset in the RTP packet and with a specified length into a given buffer
    /// </summary>
    /// <param name="off">Offset to the RTP packet of the region to be read</param>
    /// <param name="len">Length of the region to be read</param>
    /// <param name="outBuff">Output buffer</param>
    public void ReadRegionToBuff(int off, int len, byte[] outBuff)
    {
        buffer.Position = off;
        buffer.Read(outBuff, 0, len);
    }

    /// <summary>
    /// Reads an unsigned short at a specified offset as an int
    /// </summary>
    /// <param name="off">Offset to the unsigned short</param>
    /// <returns></returns>
    public int ReadUnsignedShortAsInt(int off)
    {
        this.buffer.Position = off;
        int b1 = (0x000000FF & (this.buffer.ReadByte()));
        int b2 = (0x000000FF & (this.buffer.ReadByte()));
        int val = b1 << 8 | b2;
        return val;
    }

    /// <summary>
    /// Reads an unsigned integer as a long at a specified offset
    /// </summary>
    /// <param name="off">Offset to the unsigned integer</param>
    /// <returns></returns>
    public long ReadUnsignedIntAsLong(int off)
    {
        buffer.Position = off;
        return (((long)(buffer.ReadByte() & 0xff) << 24) |
                ((long)(buffer.ReadByte() & 0xff) << 16) |
                ((long)(buffer.ReadByte() & 0xff) << 8) |
                ((long)(buffer.ReadByte() & 0xff))) & 0xFFFFFFFFL;
    }

    /// <summary>
    /// Shrinks the buffer of this packet by specified length
    /// </summary>
    /// <param name="delta">Length to shrink</param>
    public void shrink(int delta)
    {
        if (delta <= 0)
        {
            return;
        }

        long newLimit = buffer.Length - delta;
        if (newLimit <= 0)
        {
            newLimit = 0;
        }
        this.buffer.SetLength(newLimit);
    }
}
