using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SipLib.Dtls;

/// <summary>
/// Represents a fingerprint of a certificate used to authenticate WebRTC communications.
/// </summary>
public class RTCDtlsFingerprint
{
    /// <summary>
    /// One of the hash function algorithms defined in the 'Hash function Textual Names' registry.
    /// </summary>
    public string? algorithm;

    /// <summary>
    /// The value of the certificate fingerprint in lower-case hex string as expressed utilising 
    /// the syntax of 'fingerprint' in [RFC4572] Section 5.
    /// </summary>
    public string? value;

    /// <summary>
    /// Converts this object to a string that can be used for the value of the SDP fingerprint attribute
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        // FireFox wasn't happy unless the fingerprint hash was in upper case.
        return $"{algorithm} {value!.ToUpper()}";
    }

    /// <summary>
    /// Attempts to parse the fingerprint fields from a string.
    /// </summary>
    /// <param name="str">The string to parse from.</param>
    /// <param name="fingerprint">If successful a fingerprint object.</param>
    /// <returns>True if a fingerprint was successfully parsed. False if not.</returns>
    public static bool TryParse(string str, out RTCDtlsFingerprint? fingerprint)
    {
        fingerprint = null;

        if (string.IsNullOrEmpty(str))
        {
            return false;
        }
        else
        {
            int spaceIndex = str.IndexOf(' ');
            if (spaceIndex == -1)
            {
                return false;
            }
            else
            {
                string algStr = str.Substring(0, spaceIndex);
                string val = str.Substring(spaceIndex + 1);

                if (!DtlsUtils.IsHashSupported(algStr))
                {
                    return false;
                }
                else
                {
                    fingerprint = new RTCDtlsFingerprint
                    {
                        algorithm = algStr,
                        value = val
                    };
                    return true;
                }
            }
        }
    }
}
