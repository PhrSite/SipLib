﻿#region License
//-----------------------------------------------------------------------------
// Filename: H264Packetiser.cs
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 27 Dec 2020	Aaron Clauson	Created, Dublin, Ireland.
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
/// Contains functions to packetise an H264 Network Abstraction Layer Units (NAL or NALU) into an RTP payload.
/// See "RTP Payload Format for H.264 Video" https://tools.ietf.org/html/rfc6184
/// </summary>
/// <remarks>
/// Packetisation Modes (see https://tools.ietf.org/html/rfc6184#section-6.2):
/// 
/// - Mode 0: Single NAL Unit Mode. This is the default mode used when no 
///   packetization-mode parameter is present or when it is set to 0. Only 
///   single NAL unit packets may be used in this mode. STAPs, MTAPs and FUs
///   must not be used.
///
/// - Mode 1: Non-interleaved mode. This is the mode used when the
///   packetization-mode=1. Only single NAL unit packets, STAP-As and FU-As 
///   may be used in this mode.
///
/// - Mode 2: Interleaved mode. This is the mode used when the
///   packetization-mode=2. This mode is not currently supported.
/// </remarks>
public class H264Packetiser
{
    private const int H264_RTP_HEADER_LENGTH = 2;

    /// <summary>
    /// Structure for representing an H264 NAL
    /// </summary>
    public struct H264Nal
    {
        /// <summary>
        /// Gets the bytes of the NAL
        /// </summary>
        /// <value></value>
        public byte[] NAL { get; }
        /// <summary>
        /// If true, then this is the last NAL
        /// </summary>
        /// <value></value>
        public bool IsLast { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nal"></param>
        /// <param name="isLast"></param>
        public H264Nal(byte[] nal, bool isLast)
        {
            NAL = nal;
            IsLast = isLast;
        }
    }

    /// <summary>
    /// Parses an H264 encoded frame (access unit) into a list of H264Nal structures
    /// </summary>
    /// <param name="accessUnit">Input H264 access unit</param>
    /// <returns></returns>
    public static IEnumerable<H264Nal> ParseNals(byte[] accessUnit)
    {
        int zeroes = 0;

        // Parse NALs from H264 access unit, encoded as an Annex B bitstream.
        // NALs are delimited by 0x000001 or 0x00000001.
        int currPosn = 0;
        for (int i = 0; i < accessUnit.Length; i++)
        {
            if (accessUnit[i] == 0x00)
            {
                zeroes++;
            }
            else if (accessUnit[i] == 0x01 && zeroes >= 2)
            {
                // This is a NAL start sequence.
                int nalStart = i + 1;
                if (nalStart - currPosn > 4)
                {
                    int endPosn = nalStart - ((zeroes == 2) ? 3 : 4);
                    int nalSize = endPosn - currPosn;
                    bool isLast = currPosn + nalSize == accessUnit.Length;

                    yield return new H264Nal(accessUnit.Skip(currPosn).Take(nalSize).ToArray(), isLast);
                }

                currPosn = nalStart;
            }
            else
            {
                zeroes = 0;
            }
        }

        if (currPosn < accessUnit.Length)
        {
            yield return new H264Nal(accessUnit.Skip(currPosn).ToArray(), true);
        }
    }

    /// <summary>
    /// Constructs the RTP header for an H264 NAL. This method does NOT support
    /// aggregation packets where multiple NALs are sent as a single RTP payload.
    /// The supported H264 header type is Single-Time Aggregation Packet type A 
    /// (STAP-A) and Fragmentation Unit A (FU-A). The headers produced correspond
    /// to H264 packetization-mode=1.
    /// </summary>
    /// <param name="nal0">Input H264 NAL</param>
    /// <param name="isFirstPacket">Input. Set to true if this is the first packet</param>
    /// <param name="isFinalPacket">Input. Set to true if this is the final packet</param>
    /// <remarks>
    /// RTP Payload Format for H.264 Video:
    /// https://tools.ietf.org/html/rfc6184
    /// 
    /// FFmpeg H264 RTP packetisation code:
    /// https://github.com/FFmpeg/FFmpeg/blob/master/libavformat/rtpenc_h264_hevc.c
    /// 
    /// When the payload size is less than or equal to max RTP payload, send as 
    /// Single-Time Aggregation Packet (STAP):
    /// https://tools.ietf.org/html/rfc6184#section-5.7.1
    /// <code>
    ///      0                   1                   2                   3
    /// 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                          RTP Header                           |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |STAP-A NAL HDR |         NALU 1 Size           | NALU 1 HDR    |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |F|NRI|  Type   |                                               |
    /// +-+-+-+-+-+-+-+-+
    /// </code>
    /// Type = 24 for STAP-A (NOTE: this is the type of the H264 RTP header 
    /// and NOT the NAL type).
    /// 
    /// When the payload size is greater than max RTP payload, send as 
    /// Fragmentation Unit A (FU-A): https://tools.ietf.org/html/rfc6184#section-5.8
    /// <code>
    ///      0                   1                   2                   3
    /// 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// | FU indicator  |   FU header   |                               |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ 
    /// |   Fragmentation Unit (FU) Payload
    /// |
    /// ...
    /// </code>
    /// The FU indicator octet has the following format:
    /// <code>
    /// +---------------+
    /// |0|1|2|3|4|5|6|7|
    /// +-+-+-+-+-+-+-+-+
    /// |F|NRI|  Type   |
    /// +---------------+
    /// </code>
    /// F and NRI bits come from the NAL being transmitted.
    /// Type = 28 for FU-A (NOTE: this is the type of the H264 RTP header 
    /// and NOT the NAL type).
    /// 
    /// The FU header has the following format:
    /// <code>
    /// +---------------+
    /// |0|1|2|3|4|5|6|7|
    /// +-+-+-+-+-+-+-+-+
    /// |S|E|R|  Type   |
    /// +---------------+
    /// </code>
    /// S: Set to 1 for the start of the NAL FU (i.e. first packet in frame).
    /// E: Set to 1 for the end of the NAL FU (i.e. the last packet in the frame).
    /// R: Reserved bit must be 0.
    /// Type: The NAL unit payload type, comes from NAL packet (NOTE: this IS the type of the NAL message).
    /// </remarks>
    public static byte[] GetH264RtpHeader(byte nal0, bool isFirstPacket, bool isFinalPacket)
    {
        byte nalType = (byte)(nal0 & 0x1F);
        //byte nalNri = (byte)((nal0 >> 5) & 0x03);

        byte firstHdrByte = (byte)(nal0 & 0xE0); // Has either 24 (STAP-A) or 28 (FU-A) added to it.

        byte fuIndicator = (byte)(firstHdrByte + 28);
        byte fuHeader = nalType;
        if (isFirstPacket)
        {
            fuHeader += 0x80;
        }
        else if (isFinalPacket)
        {
            fuHeader += 0x40;
        }

        return new byte[] { fuIndicator, fuHeader };
    }
}
