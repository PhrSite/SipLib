//-----------------------------------------------------------------------------
// Filename: SrtpTransformer.cs
//
// Description:  SRTPTransformer implements PacketTransformer and provides 
// implementations for RTP packet to SRTP packet transformation and SRTP 
// packet to RTP packet transformation logic.
//
// Derived From:
// https://github.com/RestComm/media-core/blob/master/rtp/src/main/java/org/restcomm/media/core/rtp/crypto/SRTPTransformer.java
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
* @author Bing SU (nova.su@gmail.com)
* @author Rafael Soares (raf.csoares@kyubinteractive.com)
*/

using System.Collections.Concurrent;

namespace SipLib.Dtls;

/// <summary>
/// SRTPTransformer implements PacketTransformer and provides implementations for RTP packet to SRTP packet
/// transformation and SRTP packet to RTP packet transformation logic.
/// 
/// It will first find the corresponding SRTPCryptoContext for each packet based on their SSRC and then invoke
/// the context object to perform the transformation and reverse transformation operation.
/// </summary>
public class SrtpTransformer : IPacketTransformer
{
    private int _isLocked = 0;
    private RawPacket rawPacket;

    private SrtpTransformEngine forwardEngine;
    private SrtpTransformEngine reverseEngine;

    /// <summary>
    /// All the known SSRC's corresponding SRTPCryptoContexts
    /// </summary>
    private ConcurrentDictionary<long, SrtpCryptoContext> contexts;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="engine"></param>
    public SrtpTransformer(SrtpTransformEngine engine) : this(engine, engine)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="forwardEngine"></param>
    /// <param name="reverseEngine"></param>
    public SrtpTransformer(SrtpTransformEngine forwardEngine, SrtpTransformEngine reverseEngine)
    {
        this.forwardEngine = forwardEngine;
        this.reverseEngine = reverseEngine;
        this.contexts = new ConcurrentDictionary<long, SrtpCryptoContext>();
        this.rawPacket = new RawPacket();
    }

    /// <summary>
    /// Transforms a non-secure packet.
    /// </summary>
    /// <param name="pkt">The packet to be transformed</param>
    /// <returns>Returns the transformed packet. Returns null if the packet cannot be transformed.</returns>
    public byte[] Transform(byte[] pkt)
    {
        return Transform(pkt, 0, pkt.Length);
    }

    /// <summary>
    /// Transforms a specific non-secure packet.
    /// </summary>
    /// <param name="pkt">The packet to be secured</param>
    /// <param name="offset">The offset of the packet data</param>
    /// <param name="length">The length of the packet data</param>
    /// <returns>Returns the transformed packet. Returns null if the packet cannot be transformed.</returns>
    public byte[] Transform(byte[] pkt, int offset, int length)
    {
        bool isLocked = Interlocked.CompareExchange(ref _isLocked, 1, 0) != 0;

        try
        {
            // Updates the contents of raw packet with new incoming packet 
            var rawPacket = !isLocked ? this.rawPacket : new RawPacket();
            rawPacket.Wrap(pkt, offset, length);

            // Associate packet to a crypto context
            long ssrc = rawPacket.GetSSRC();
            SrtpCryptoContext context = null;
            contexts.TryGetValue(ssrc, out context);

            if (context == null)
            {
                context = forwardEngine.GetDefaultContext().deriveContext(ssrc, 0, 0);
                context.DeriveSrtpKeys(0);
                contexts.AddOrUpdate(ssrc, context, (a, b) => context);
            }

            // Transform RTP packet into SRTP
            context.TransformPacket(rawPacket);
            byte[] result = rawPacket.GetData();

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
    /// <param name="pkt">The transformed packet to be restored</param>
    /// <returns>The restored packet</returns>
    public byte[] ReverseTransform(byte[] pkt)
    {
        return ReverseTransform(pkt, 0, pkt.Length);
    }

    /// <summary>
    /// Reverse-transforms a specific packet (i.e. transforms a transformed packet back).
    /// </summary>
    /// <param name="pkt">The packet to be restored</param>
    /// <param name="offset">The offset to the packet data</param>
    /// <param name="length">The length of data in the packet</param>
    /// <returns>The restored packet</returns>
    public byte[] ReverseTransform(byte[] pkt, int offset, int length)
    {
        bool isLocked = Interlocked.CompareExchange(ref _isLocked, 1, 0) != 0;
        try
        {
            // Wrap data into the raw packet for readable format
            var rawPacket = !isLocked ? this.rawPacket : new RawPacket();
            rawPacket.Wrap(pkt, offset, length);

            // Associate packet to a crypto context
            long ssrc = rawPacket.GetSSRC();
            SrtpCryptoContext context = null;
            this.contexts.TryGetValue(ssrc, out context);
            if (context == null)
            {
                context = this.reverseEngine.GetDefaultContext().deriveContext(ssrc, 0, 0);
                context.DeriveSrtpKeys(rawPacket.GetSequenceNumber());
                contexts.AddOrUpdate(ssrc, context, (a, b) => context);
            }

            byte[] result = null;
            bool reversed = context.ReverseTransformPacket(rawPacket);
            if (reversed)
            {
                result = rawPacket.GetData();
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
            SrtpCryptoContext context;
            contexts.TryRemove(ssrc, out context);
            if (context != null)
            {
                context.Close();
            }
        }
    }
}