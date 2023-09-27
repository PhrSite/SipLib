/////////////////////////////////////////////////////////////////////////////////////
//  File:   InlineParameters.cs                                         21 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.RtpCrypto;

/// <summary>
/// Class for storing and managing the parameter of the "inline" SRTP key-param of an SDP crypto media attribute.
/// See Section 9 of RFC 4568.
/// </summary>
public class InlineParams
{
    /// <summary>
    /// Contains the master key byte array. Must be a valid length for the AES algorithm (16 bytes for AES-128),
    /// (24 bytes for AES-192 or 32 bytes for AES-256).
    /// </summary>
    public byte[] MasterKey = null;
    /// <summary>
    /// Contains the master salt byte array. Must be 14 bytes in length for SRTP.
    /// </summary>
    public byte[] MasterSalt = null;
    /// <summary>
    /// Specifies the lifetime of the master key in RTP packets. A value of 0 indicates that the default
    /// lifetime is to be used. Must be an integral power of 2. A value of 0 indicates that the default
    /// lifetime of 2^48 should be used.
    /// </summary>
    public ulong Lifetime = 0;
    /// <summary>
    /// Specifies the Master Key Identifier (MKI) for the master key. A value of 0 indicates that master key
    /// identifiers are not being used.
    /// </summary>
    public int MKI = 0;
    /// <summary>
    /// Specifies length in bytes of the MKI in the SRTP or SRTCP packet.
    /// </summary>
    public int MKI_Length = 0;

    /// <summary>
    /// Parses the "inline=" portion of an SDP crypto attribute.
    /// </summary>
    /// <param name="Inline">String containing the "inline=" portion of the crypto attribute. Needs to be in
    /// the form:
    ///     "inline:MTIzNDU2Nzg5QUJDREUwMTIzNDU2Nzg5QUJjZGVm|2^20|1:4".
    /// See Section 9.1 of RFC 4568.
    /// </param>
    /// <param name="KeyLength">Master Key length in bytes. Must be 16, 24 or 32, depending on the crypto
    /// suite.</param>
    /// <returns>Returns a new InlineParameters object if the input is valid or null if the input is not
    /// valid.</returns>
    public static InlineParams Parse(string Inline, int KeyLength)
    {
        bool Success = true;
        InlineParams Ilp = new InlineParams();
        string strKeys = SRtpUtils.ExtractDelimStr(':', '|', Inline);
        if (strKeys == null)
        {   // No lifetime or MKI parameters
            int Idx = Inline.IndexOf(":");
            if (Idx == -1)
                // Error no keys provided or not properly formatted.
                return null;
            else
            {
                strKeys = Inline.Substring(Idx + 1);
                if (string.IsNullOrEmpty(strKeys))
                    // Error no keys provided or not properly formatted.
                    return null;
            }
        }

        byte[] Keys = null;
        try
        {
            Keys = Convert.FromBase64String(strKeys);
        }
        catch (ArgumentNullException) { Keys = null; }
        catch (FormatException) { Keys = null; }
        catch (Exception) { Keys = null; }

        if (Keys == null)
            return null;

        int ExpectedLength = KeyLength + SRtpUtils.n_sB;    // The salt length is always 14 bytes (112 bits)
        if (Keys.Length != ExpectedLength)
            return null;

        Ilp.MasterKey = new byte[KeyLength];
        Ilp.MasterSalt = new byte[SRtpUtils.n_sB];
        Array.ConstrainedCopy(Keys, 0, Ilp.MasterKey, 0, KeyLength);
        Array.ConstrainedCopy(Keys, KeyLength, Ilp.MasterSalt, 0, SRtpUtils.n_sB);

        // Check to see if the lifetime parameter is present.
        string[] Params = Inline.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        if (Params == null || Params.Length < 2)
            return Ilp;    // The lifetime and MKI parameters are not present.

        if (Params[1].IndexOf(":") == -1)
        {   // The first parameter is the lifetime parameter because it will never contain a ":" character
            // and the MKI/Length paramter will.
            string strLifetime = Params[1];
            if (strLifetime.IndexOf("^") > 0)
            {   // The lifetime is expressed as an exponential value in the form of "2^24".
                string[] strLparams = strLifetime.Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);

                int b = 0, exp = 0;
                if (strLparams == null || strLparams.Length != 2 || int.TryParse(strLparams[0], out b) == false ||
                    int.TryParse(strLparams[1], out exp) == false || b != 2 || exp > 48)
                    return null;
                else
                    Ilp.Lifetime = Convert.ToUInt64(Math.Pow(b, exp));
            }
            else
            {   // The lifetime is expressed as a simple integer.
                if (ulong.TryParse(strLifetime, out Ilp.Lifetime) == false || Ilp.Lifetime < 0)
                    return null;
            }
        }
        else
        {   // The first parameter is the MKI and length parameter. The lifetime parameter is not present.
            Ilp.Lifetime = 0;
            Success = ParseMkiParams(Params[1], Ilp);
        }

        if (Params.Length == 3)
            Success = ParseMkiParams(Params[2], Ilp);

        if (Success == true)
            return Ilp;
        else
            return null;
    }

    private static bool ParseMkiParams(string strMki, InlineParams Ilp)
    {
        if (string.IsNullOrEmpty(strMki) == true)
            return false;

        string[] strMkiParams = strMki.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (strMkiParams == null || strMkiParams.Length != 2 || int.TryParse(strMkiParams[0], 
            out Ilp.MKI) == false || int.TryParse(strMkiParams[1], out Ilp.MKI_Length) == false || 
            Ilp.MKI < 0 || Ilp.MKI_Length < 1 || Ilp.MKI_Length > 128)
            return false;

        return true;
    }

    /// <summary>
    /// Converts this object into a formatted string for inclusion as an inline key param. in a crypto SDP
    /// attribute.
    /// </summary>
    /// <returns>Returns a string representation of this object.</returns>
    public new string ToString()
    {
        StringBuilder Sb = new StringBuilder();

        byte[] Keys = new byte[MasterKey.Length + SRtpUtils.n_sB];
        Array.ConstrainedCopy(MasterKey, 0, Keys, 0, MasterKey.Length);
        Array.ConstrainedCopy(MasterSalt, 0, Keys, MasterKey.Length, SRtpUtils.n_sB);
        Sb.AppendFormat("inline:{0}", Convert.ToBase64String(Keys));

        if (Lifetime != 0)
        {
            int exp = (int) Math.Log2(Lifetime);
            Sb.AppendFormat("|2^{0}", exp);
        }

        if (MKI != 0 && MKI_Length >= 1)
            Sb.AppendFormat("|{0}:{1}", MKI, MKI_Length);

        return Sb.ToString();
    }
}
