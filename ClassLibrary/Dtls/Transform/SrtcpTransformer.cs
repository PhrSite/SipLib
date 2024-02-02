//-----------------------------------------------------------------------------
// Filename: SrtcpTransformer.cs
//
// Description: Encapsulates the encryption/decryption logic for SRTCP packets.
//
// Derived From:
// https://github.com/RestComm/media-core/blob/master/rtp/src/main/java/org/restcomm/media/core/rtp/crypto/SRTCPTransformer.java
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

using System.Collections.Concurrent;

namespace SipLib.Dtls;

/// <summary>
/// SRTCPTransformer implements PacketTransformer.
/// It encapsulate the encryption / decryption logic for SRTCP packets
///
/// author Bing SU (nova.su@gmail.com)
/// author Werner Werner.Dittmann@t-online.de
/// </summary>
internal class SrtcpTransformer : IPacketTransformer
{
    private int _isLocked = 0;
    private RawPacket packet;

    private SrtpTransformEngine forwardEngine;
    private SrtpTransformEngine reverseEngine;

    /** All the known SSRC's corresponding SRTCPCryptoContexts */
    private ConcurrentDictionary<long, SrtcpCryptoContext> contexts;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="engine"></param>
    public SrtcpTransformer(SrtpTransformEngine engine) : this(engine, engine)
    {

    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="forwardEngine"></param>
    /// <param name="reverseEngine"></param>
    public SrtcpTransformer(SrtpTransformEngine forwardEngine, SrtpTransformEngine reverseEngine)
    {
        this.packet = new RawPacket();
        this.forwardEngine = forwardEngine;
        this.reverseEngine = reverseEngine;
        this.contexts = new ConcurrentDictionary<long, SrtcpCryptoContext>();
    }

    /// <summary>
    /// Encrypts a SRTCP packet
    /// </summary>
    /// <param name="pkt">plain SRTCP packet to be encrypted.</param>
    /// <returns>encrypted SRTCP packet.</returns>
    public byte[] Transform(byte[] pkt)
    {
        return Transform(pkt, 0, pkt.Length);
    }

    /// <summary>
    /// Encrypts a SRTCP packet
    /// </summary>
    /// <param name="pkt"></param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public byte[] Transform(byte[] pkt, int offset, int length)
    {
        var isLocked = Interlocked.CompareExchange(ref _isLocked, 1, 0) != 0;
        try
        {
            // Wrap the data into raw packet for readable format
            var packet = !isLocked ? this.packet : new RawPacket();
            packet.Wrap(pkt, offset, length);

            // Associate the packet with its encryption context
            long ssrc = packet.GetRTCPSSRC();
            SrtcpCryptoContext context = null;
            contexts.TryGetValue(ssrc, out context);

            if (context == null)
            {
                context = forwardEngine.GetDefaultContextControl().DeriveContext(ssrc);
                context.DeriveSrtcpKeys();
                contexts.AddOrUpdate(ssrc, context, (a, b) => context);
            }

            // Secure packet into SRTCP format
            context.TransformPacket(packet);
            byte[] result = packet.GetData();

            return result;
        }
        finally
        {
            //Unlock
            if (!isLocked)
                Interlocked.CompareExchange(ref _isLocked, 0, 1);
        }
    }

    /// <summary>
    /// Reverse-transforms a specific packet (i.e. transforms a transformed packet back).
    /// </summary>
    /// <param name="pkt"></param>
    /// <returns></returns>
    public byte[] ReverseTransform(byte[] pkt)
    {
        return ReverseTransform(pkt, 0, pkt.Length);
    }

    /// <summary>
    /// Reverse-transforms a specific packet (i.e. transforms a transformed packet back).
    /// </summary>
    /// <param name="pkt"></param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public byte[] ReverseTransform(byte[] pkt, int offset, int length)
    {
        var isLocked = Interlocked.CompareExchange(ref _isLocked, 1, 0) != 0;
        try
        {
            // wrap data into raw packet for readable format
            var packet = !isLocked ? this.packet : new RawPacket();
            packet.Wrap(pkt, offset, length);

            // Associate the packet with its encryption context
            long ssrc = packet.GetRTCPSSRC();
            SrtcpCryptoContext context = null;
            contexts.TryGetValue(ssrc, out context);

            if (context == null)
            {
                context = reverseEngine.GetDefaultContextControl().DeriveContext(ssrc);
                context.DeriveSrtcpKeys();
                contexts.AddOrUpdate(ssrc, context, (a, b) => context);
            }

            // Decode packet to RTCP format
            byte[] result = null;
            bool reversed = context.ReverseTransformPacket(packet);
            if (reversed)
            {
                result = packet.GetData();
            }
            return result;
        }
        finally
        {
            //Unlock
            if (!isLocked)
                Interlocked.CompareExchange(ref _isLocked, 0, 1);
        }
    }

    /// <summary>
    /// Close the transformer and underlying transform engine.
    /// The close functions closes all stored crypto contexts. This deletes key data
    /// and forces a cleanup of the crypto contexts.
    /// </summary>
    public void Close()
    {
        forwardEngine.Close();
        if (forwardEngine != reverseEngine)
        {
            reverseEngine.Close();
        }

        var keys = new List<long>(contexts.Keys);
        foreach (var ssrc in keys)
        {
            if (contexts.TryRemove(ssrc, out var context))
            {
                context.Close();
            }
        }
    }
}
