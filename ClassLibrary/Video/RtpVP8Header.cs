#region License
//-----------------------------------------------------------------------------
// Filename: RtpVP8Header.cs
//
// Description: Represents the RTP header to use for a VP8 encoded payload as per
// https://tools.ietf.org/html/rfc7741.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 11 Nov 2014	Aaron Clauson	Created, Hobart, Australia.
// 11 Aug 2019  Aaron Clauson   Added full license header.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------
#endregion

//  Revised:  19 Oct 23 PHR
//    -- Added documentation comments
//    -- General code cleanup

namespace SipLib.Video;

/// <summary>
/// Representation of the VP8 RTP header as specified in RFC7741. See https://tools.ietf.org/html/rfc7741.
/// </summary>
public class RtpVP8Header
{
    // Payload Descriptor Fields.
    /// <summary>
    /// Indicates whether extended control bits are present.
    /// </summary>
    public bool ExtendedControlBitsPresent;

    /// <summary>
    /// When set indicates the frame can be discarded without affecting any other frames.
    /// </summary>
    public bool NonReferenceFrame;

    /// <summary>
    /// Should be set when the first payload octet is the start of a new VP8 partition.
    /// </summary>
    public bool StartOfVP8Partition;

    /// <summary>
    /// Denotes the VP8 partition index that the first payload octet of the packet belongs to.
    /// </summary>
    public byte PartitionIndex;

    /// <summary>
    /// If true then the PictureID field is present in the VP8 header
    /// </summary>
    public bool IsPictureIDPresent;

    /// <summary>
    /// Contains the PictureID field. Valid only if IsPictureIDPresent is true.
    /// </summary>
    public ushort PictureID;

    // Payload Header Fields.
    /// <summary>
    /// The size of the first partition in bytes is calculated from the 19 bits in Size0, Size1 and Size2 as:
    /// size = Size0 + (8 x Size1) + (2048 8 Size2).
    /// </summary>
    public int FirstPartitionSize;

    private int _length = 0;

    /// <summary>
    /// Gets the length of the VP8 header.
    /// </summary>
    public int Length
    {
        get { return _length; }
    }

    private int _payloadDescriptorLength;

    /// <summary>
    /// Gets the length of the Payload Descriptor in the VP8 header.
    /// </summary>
    public int PayloadDescriptorLength
    {
        get { return _payloadDescriptorLength; }
    }

    private RtpVP8Header()
    { }

    /// <summary>
    /// Gets the RtpVP8Header from the payload of an RTP packet
    /// </summary>
    /// <param name="rtpPayload">Payload of the received RTP packet.</param>
    /// <returns>Returns a new RtpVP8header object.</returns>
    public static RtpVP8Header GetVP8Header(byte[] rtpPayload)
    {
        RtpVP8Header vp8Header = new RtpVP8Header();
        int payloadHeaderStartIndex = 1;

        // First byte of payload descriptor.
        vp8Header.ExtendedControlBitsPresent = ((rtpPayload[0] >> 7) & 0x01) == 1;
        vp8Header.StartOfVP8Partition = ((rtpPayload[0] >> 4) & 0x01) == 1;
        vp8Header._length = 1;

        // Is second byte being used.
        if (vp8Header.ExtendedControlBitsPresent)
        {
            vp8Header.IsPictureIDPresent = ((rtpPayload[1] >> 7) & 0x01) == 1;
            vp8Header._length = 2;
            payloadHeaderStartIndex = 2;
        }

        // Is the picture ID being used.
        if (vp8Header.IsPictureIDPresent)
        {
            if (((rtpPayload[2] >> 7) & 0x01) == 1)
            {
                // The Picture ID is using two bytes.
                vp8Header._length = 4;
                payloadHeaderStartIndex = 4;
                vp8Header.PictureID = BitConverter.ToUInt16(rtpPayload, 2);
            }
            else
            {
                // The picture ID is using one byte.
                vp8Header.PictureID = rtpPayload[2];
                vp8Header._length = 3;
                payloadHeaderStartIndex = 3;
            }
        }

        vp8Header._payloadDescriptorLength = payloadHeaderStartIndex;

        return vp8Header;
    }
}
