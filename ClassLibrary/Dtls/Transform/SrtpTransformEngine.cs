//-----------------------------------------------------------------------------
// Filename: SrtpTransformEngine.cs
//
// Description: SRTPTransformEngine class implements TransformEngine interface. 
// It stores important information / objects regarding SRTP processing.Through
// SRTPTransformEngine, we can get the needed PacketTransformer, which will be
// used by abstract TransformConnector classes.
//
// Derived From:
// https://github.com/RestComm/media-core/blob/master/rtp/src/main/java/org/restcomm/media/core/rtp/crypto/SRTPTransformEngine.java
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
/// Implementation of the SRTP Transform Engine
/// </summary>
public class SrtpTransformEngine : ITransformEngine
{
    /// <summary>
    /// The default SRTPCryptoContext, which will be used to derivate other contexts.
    /// </summary>
    private SrtpCryptoContext defaultContext;

    /// <summary>
    /// The default SRTPCryptoContext, which will be used to derive other contexts.
    /// </summary>
    private SrtcpCryptoContext defaultContextControl;

    /// <summary>
    /// Constructs a SRTPTransformEngine based on given master encryption key, master salt key and SRTP/SRTCP policy.
    /// </summary>
    /// <param name="masterKey">The master encryption key</param>
    /// <param name="masterSalt">The master salt key</param>
    /// <param name="srtpPolicy">SRTP policy</param>
    /// <param name="srtcpPolicy">SRTCP policy</param>
    public SrtpTransformEngine(byte[] masterKey, byte[] masterSalt, SrtpPolicy srtpPolicy, SrtpPolicy srtcpPolicy)
    {
        defaultContext = new SrtpCryptoContext(0, 0, 0, masterKey, masterSalt, srtpPolicy);
        defaultContextControl = new SrtcpCryptoContext(0, masterKey, masterSalt, srtcpPolicy);
    }

    /// <summary>
    /// Closes the transformer engine.
    /// </summary>
    public void Close()
    {
        if (defaultContext != null)
        {
            defaultContext.Close();
            defaultContext = null;
        }
        if (defaultContextControl != null)
        {
            defaultContextControl.Close();
            defaultContextControl = null;
        }
    }

    /// <summary>
    /// Gets the IPacketTransformer for RTCP packets.
    /// </summary>
    /// <returns></returns>
    public IPacketTransformer GetRTCPTransformer()
    {
        return new SrtcpTransformer(this);
    }

    /// <summary>
    /// Gets the IPacketTransformer for RTP packets.
    /// </summary>
    /// <returns></returns>
    public IPacketTransformer GetRTPTransformer()
    {
        return new SrtpTransformer(this);
    }

    /// <summary>
    /// Gets the default SRTPCryptoContext
    /// </summary>
    /// <returns></returns>
    public SrtpCryptoContext GetDefaultContext()
    {
        return this.defaultContext;
    }

    /// <summary>
    /// Get the default SRTPCryptoContext
    /// </summary>
    /// <returns></returns>
    public SrtcpCryptoContext GetDefaultContextControl()
    {
        return this.defaultContextControl;
    }
}
