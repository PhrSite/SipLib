//-----------------------------------------------------------------------------
// Filename: IPacketTransformer.cs
//
// Description: Encapsulate the concept of packet transformation. Given a packet,
// PacketTransformer can either transform it or reverse the
// transformation.
//
// Derived From:
// https://github.com/RestComm/media-core/blob/master/rtp/src/main/java/org/restcomm/media/core/rtp/crypto/PacketTransformer.java
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

//  Revised: 26 Nov 23 PHR
//      -- Changed namespace to SipLib.Dtls from SIPSorcery.Net
//      -- Added documentation comments and code cleanup

namespace SipLib.Dtls;

/// <summary>
/// Interface for transforming a non-secure packet.
/// </summary>
public interface IPacketTransformer
{
    /// <summary>
    /// Transforms a non-secure packet.
    /// </summary>
    /// <param name="pkt">The packet to be transformed</param>
    /// <returns>Returns the transformed packet. Returns null if the packet cannot be transformed.</returns>
    byte[] Transform(byte[] pkt);

    /// <summary>
    /// Transforms a specific non-secure packet.
    /// </summary>
    /// <param name="pkt">The packet to be secured</param>
    /// <param name="offset">The offset of the packet data</param>
    /// <param name="length">The length of the packet data</param>
    /// <returns>Returns the transformed packet. Returns null if the packet cannot be transformed.</returns>
    byte[] Transform(byte[] pkt, int offset, int length);

    /// <summary>
    /// Reverse-transforms a specific packet (i.e. transforms a transformed packet back).
    /// </summary>
    /// <param name="pkt">The transformed packet to be restored</param>
    /// <returns>The restored packet</returns>
    byte[] ReverseTransform(byte[] pkt);

    /// <summary>
    /// Reverse-transforms a specific packet (i.e. transforms a transformed packet back).
    /// </summary>
    /// <param name="pkt">The packet to be restored</param>
    /// <param name="offset">The offset to the packet data</param>
    /// <param name="length">The length of data in the packet</param>
    /// <returns>The restored packet</returns>
    byte[] ReverseTransform(byte[] pkt, int offset, int length);

    /// <summary>
    /// Closes the transformer and underlying transform engine.
    /// The close functions closes all stored crypto contexts. This deletes key data and forces a cleanup of the
    /// crypto contexts.
    /// </summary>
    void Close();
}