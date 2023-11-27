//-----------------------------------------------------------------------------
// Filename: ITransformEngine.cs
//
// Description: Defines how to get PacketTransformers for RTP and RTCP packets.
// A single PacketTransformer can be used for both RTP and RTCP packets
// or there can be two separate PacketTransformers.
//
// Derived From:
// https://github.com/RestComm/media-core/blob/master/rtp/src/main/java/org/restcomm/media/core/rtp/crypto/TransformEngine.java
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

namespace SipLib.Dtls;

/// <summary>
/// Interface for the transform engine.
/// </summary>
public interface ITransformEngine
{
    /// <summary>
    /// Gets the IPacketTransformer interface for RTP packets
    /// </summary>
    /// <returns></returns>
    IPacketTransformer GetRTPTransformer();

    /// <summary>
    /// Gets the IPacketTransformer interface for RTCP packets
    /// </summary>
    /// <returns></returns>
    IPacketTransformer GetRTCPTransformer();
}
