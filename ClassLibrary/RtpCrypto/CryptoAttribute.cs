/////////////////////////////////////////////////////////////////////////////////////
//  File:   CryptoAttribute.cs                                      21 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.RtpCrypto;

/// <summary>
/// Class for the crypto SDP attribute for SRTP. See Section 9 of RFC 4568.
/// The format is like:
/// <para>
/// a=crypto:1 AES_CM_128_HMAC_SHA1_80 inline:PS1uQCVeeCFCanVmcjkpPywjNWhcYD0mXXtxaVBR|2^20|1:4
/// </para>
/// <para>There may be more than one inline parameter. inline parameters are delimited by a ";".</para>
/// </summary>
public class CryptoAttribute
{
    /// <summary>
    /// Value of the tag parameter.
    /// </summary>
    /// <value></value>
    public int Tag = 1;

    /// <summary>
    /// Specifies the crypto-suite to use. The default is AES_CM_128_HMAC_SHA1_80.
    /// </summary>
    /// <value></value>
    public string CryptoSuite = CryptoSuites.AES_CM_128_HMAC_SHA1_80;

    /// <summary>
    /// Contains a list of key-params. This list must contain at least 1 element.
    /// </summary>
    /// <value></value>
    public List<InlineParams> InlineParameters = new List<InlineParams>();
    /// <summary>
    /// Specifies the Key Derivation Rate. Must be in the range of 0 - 24 and is an integer power of 2. 
    /// Optional.
    /// Set to -1 to indicate not specified. An unspecified KDR means that the default KDR of 0 should be used.
    /// </summary>
    /// <value></value>
    public int KDR = -1;
    /// <summary>
    /// Specifies the fec-order (for forward error correction) session parameter. Must be "FEC_SRTP" or
    /// "SRTP_FEC". Optional. Set to null if it is not specified.
    /// </summary>
    /// <value></value>
    public string? FEC_ORDER = null;
    /// <summary>
    /// Specifies the fec-key session parameter. The value depends upon the FEC scheme being used. Optional.
    /// Set to null if it is not specified.
    /// </summary>
    /// <value></value>
    public string? FEC_KEY = null;
    /// <summary>
    /// Specifies the Window Size Hint used for replay detection. This session parameter is optional. The
    /// minimum value is 64. A value of -1 indicates that the field has not been set.
    /// </summary>
    /// <value></value>
    public int WSH = -1;

    /// <summary>
    /// Parses the value portion of a crypto SDP attribute. See Section 9.1 of RFC 4568. The ABNF for the
    /// value portion of this attribute is:
    ///     tag 1*WSP crypto-suite 1*WSP key-params *(1*WSP session-param)
    /// </summary>
    /// <param name="strCrypto">Input containing the value portion of a crypto SDP attribute.</param>
    /// <returns>Returns a new CryptoAttribute object if successful or null if an error occurred or
    /// the crypto suite is not supported.</returns>
    public static CryptoAttribute? Parse(string strCrypto)
    {
        if (string.IsNullOrEmpty(strCrypto) == true)
            return null;

        string[] Fields = strCrypto.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (Fields == null || Fields.Length < 3)
            return null;

        CryptoAttribute attr = new CryptoAttribute();

        if (int.TryParse(Fields[0], out attr.Tag) == false || attr.Tag < 1)
            return null;

        attr.CryptoSuite = Fields[1];

        if (CryptoSuites.CryptoSuiteIsSupported(attr.CryptoSuite) == false)
            return null;    // The crypto suite is not supported so cannot parse the inline parameters.

        if (string.IsNullOrEmpty(Fields[2]) == true)
            return null;

        string[] Inlines = Fields[2].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (Inlines == null || Inlines.Length < 1)
            return null;

        foreach (string strInline in Inlines)
        {
            InlineParams Srip = InlineParams.Parse(strInline, CryptoSuites.GetKeyLengthBytes(attr.CryptoSuite));
            if (Srip != null)
                attr.InlineParameters.Add(Srip);
            else
                return null;
        }

        string Val = null;
        // Parse the session parameters
        for (int i = 3; i < Fields.Length; i++)
        {
            if (Fields[i].IndexOf("KDR=") >= 0)
            {
                Val = SRtpUtils.GetValueOfNameValuePair(Fields[i], '=');
                if (string.IsNullOrEmpty(Val) == true || int.TryParse(Val, out attr.KDR) == false ||
                    attr.KDR < 0 || attr.KDR > 24)
                    return null;
            }
            else if (Fields[i].IndexOf("FEC_ORDER=") >= 0)
                // Don't care about errors
                attr.FEC_ORDER = SRtpUtils.GetValueOfNameValuePair(Fields[i], '=');
            else if (Fields[i].IndexOf("FEC_KEY=") >= 0)
                // Don't care about errors
                attr.FEC_KEY = SRtpUtils.GetValueOfNameValuePair(Fields[i], '=');
            else if (Fields[i].IndexOf("WSH=") >= 0)
            {   // Don't care about errors
                Val = SRtpUtils.GetValueOfNameValuePair(Fields[i], '=');
                int.TryParse(Fields[i], out attr.WSH);
            }
        }

        return attr;
    }

    /// <summary>
    /// Converts this object to a string.
    /// </summary>
    /// <returns>Returns the string version of this object that can be used for the parameters part of a crypto
    /// SDP attribute.</returns>
    public new string ToString()
    {
        StringBuilder Sb = new StringBuilder();
        Sb.AppendFormat("{0} ", Tag);
        Sb.AppendFormat("{0}", CryptoSuite);
        Sb.Append(" ");

        int i = 0;
        for (i = 0; i < InlineParameters.Count; i++)
        {
            Sb.AppendFormat("{0}", InlineParameters[i].ToString());
            if (InlineParameters.Count > 1 && i < InlineParameters.Count - 1)
                Sb.Append(";");
        }

        if (KDR != -1)
            Sb.AppendFormat(" KDR={0}", KDR);

        if (string.IsNullOrEmpty(FEC_ORDER) == false)
            Sb.AppendFormat(" FEC_ORDER={0}", FEC_ORDER);

        if (string.IsNullOrEmpty(FEC_KEY) == false)
            Sb.AppendFormat(" FEC_KEY={0}", FEC_KEY);

        if (WSH > 0)
            Sb.AppendFormat(" WSH={0}", WSH);

        return Sb.ToString();
    }
}
