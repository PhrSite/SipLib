/////////////////////////////////////////////////////////////////////////////////////
//  File:   RocValues.cs                                            25 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.RtpCrypto;

/// <summary>
/// Class for storing the Roll Over Counter (ROC) for SRTP encryption. The ROC counts the number of times
/// that the RTP packet sequence number has wrapped around in a RTP media session. 
/// See Section 3.3.1 of RFC 3711.
/// </summary>
public class RocValues
{
    /// <summary>
    /// Current ROC value.
    /// </summary>
    /// <value></value>
    public uint Roc = 0;
    /// <summary>
    /// Stores the value of ROC - 1
    /// </summary>
    /// <value></value>
    public uint RocMinus1 = uint.MaxValue;
    /// <summary>
    /// Stores the value of ROC + 1
    /// </summary>
    /// <value></value>
    public uint RocPlus1 = 1;

    /// <summary>
    /// Increments the Roll Over Counter (ROC)
    /// </summary>
    public void IncrementRoc()
    {
        Roc += 1;
        RocMinus1 = Roc - 1;
        RocPlus1 = Roc + 1;
    }
}
