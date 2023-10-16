/////////////////////////////////////////////////////////////////////////////////////
//  File:   RttParameters.cs                                        12 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Sdp;
using System.Text;
using System.Text.RegularExpressions;

namespace SipLib.RealTimeText;

/// <summary>
/// Holds the RTT protocol parameters for a RTT session
/// </summary>
public class RttParameters
{
    /// <summary>
    /// Specifies the number of redundancy levels. A value of 0 specifies that redundancy is not being used.
    /// </summary>
    public int RedundancyLevel = RttUtils.DefaultRedundancyLevel;

    /// <summary>
    /// Specifies the t140 payload type in the RTP packets.
    /// </summary>
    public int T140PayloadType = RttUtils.DefaultT140PayloadType;

    /// <summary>
    /// Specifies the redundant packet payload type in the RTP packets. A value of 0 indicates that redundancy
    /// is not being used.
    /// </summary>
    public int RedundancyPayloadType = RttUtils.DefaultRedundantPayloadType;

    /// <summary>
    /// Specifies the maximum number of characters per second that may be sent. A value of 0 indicates that
    /// the data rate is not limited.
    /// </summary>
    public int Cps = RttUtils.DefaultCps;

    /// <summary>
    /// If true then the UA of the RTT RTP channel is RTT mixer aware as specified in RFC 9071.
    /// </summary>
    public bool RttMixerAware = false;

    /// <summary>
    /// Constructor
    /// </summary>
    public RttParameters()
    {
    }

    /// <summary>
    /// Parses an SDP media description object and creates an RttParameters object.
    /// </summary>
    /// <param name="mediaDescription">Input SDP media description. The media type must be "text"
    /// for RTT.</param>
    /// <returns>Returns a new RttParameters object. Returns null if a parsing error occurred.</returns>
    public static RttParameters FromMediaDescription(MediaDescription mediaDescription)
    {
        if (mediaDescription.MediaType != "text")
            return null;

        RttParameters rttParams = new RttParameters();
        SdpAttribute T140RtpMap = mediaDescription.GetRtpmapForCodecType("t140/1000");
        rttParams.T140PayloadType = T140RtpMap != null ? int.Parse(T140RtpMap.Value) : 0;
        SdpAttribute RedRtpMap = mediaDescription.GetRtpmapForCodecType("red/1000");
        rttParams.RedundancyPayloadType = RedRtpMap != null ? int.Parse(RedRtpMap.Value) : 0;

        SdpAttribute Fmtp;
        if (rttParams.RedundancyPayloadType != 0)
        {
            Fmtp = mediaDescription.GetFmtpForFormatNumber(RedRtpMap.Value);
            if (Fmtp != null)
            {   // Determine the number of redundancy levels
                foreach (string strName in Fmtp.Params.Keys)
                {
                    if (strName.IndexOf(T140RtpMap.Value) >= 0)
                        // Note: The number of occurrances of the T140 codec number in the fmtp attribute
                        // minus 1 defines the redundancy level. For example 98/98/98 defines a
                        // redundancy level of 2.
                        rttParams.RedundancyLevel = Regex.Matches(strName, T140RtpMap.Value).Count - 1;
                }
            }
            else
                rttParams.RedundancyLevel = 0;    // This is an error.
        }
        else
            rttParams.RedundancyLevel = 0;

        // Get the cps= parameter from the fmtp attribute that is for text (i.e. t140)
        Fmtp = mediaDescription.GetFmtpForFormatNumber(T140RtpMap?.Value);
        if (Fmtp != null)
        {   // Determine the cps parameter
            rttParams.Cps = 0;
            string strMaxCps = null;
            if (Fmtp.GetAttributeParameter("cps", ref strMaxCps) == true && strMaxCps != null)
                int.TryParse(strMaxCps, out rttParams.Cps);
        }

        if (mediaDescription.GetNamedAttribute("rtt-mixer") != null)
            rttParams.RttMixerAware = true;

        return rttParams;
    }

    /// <summary>
    /// Creates an SDP media description object from this RttParameters object.
    /// </summary>
    /// <param name="Port">UDP port number to use.</param>
    /// <returns>Returns a new SDP MediaDescription object.</returns>
    public MediaDescription ToMediaDescription(int Port)
    {
        MediaDescription mediaDescription = new MediaDescription();
        mediaDescription.MediaType = "text";
        mediaDescription.Port = Port;
        mediaDescription.Transport = "RTP/AVP";
        string strT140Pt = T140PayloadType.ToString();
        string strRedPt = RedundancyPayloadType.ToString();

        mediaDescription.MediaFormatNumbers.Add(strT140Pt);
        if (RedundancyPayloadType != 0)
            mediaDescription.MediaFormatNumbers.Add(strRedPt);

        SdpAttribute T140Attribute = new SdpAttribute("rtpmap", strT140Pt);
        T140Attribute.Params.Add("t140/1000", null);
        mediaDescription.Attributes.Add(T140Attribute);

        if (RedundancyPayloadType != 0)
        {   // Redundancy is being used.
            SdpAttribute RedAttribute = new SdpAttribute("rtpmap", strRedPt);
            RedAttribute.Params.Add("red/1000", null);
            mediaDescription.Attributes.Add(RedAttribute);
            StringBuilder Sb = new StringBuilder();
            int Cnt = RedundancyLevel + 1;
            for (int i = 0; i < Cnt; i++)
            {
                Sb.Append(strT140Pt);
                if (i < Cnt - 1)
                    Sb.Append("/");
            }

            SdpAttribute FmtpAttribute = new SdpAttribute("fmtp", strRedPt);
            FmtpAttribute.Params.Add(Sb.ToString(), null);
            mediaDescription.Attributes.Add (FmtpAttribute);
        }

        if (Cps != 0)
        {   // Use the format number for text (i.e. t140) for the cps= parameter
            SdpAttribute CpsAttribute = new SdpAttribute("fmtp", strT140Pt);
            CpsAttribute.Params.Add("cps", Cps.ToString());
            mediaDescription.Attributes.Add(CpsAttribute);
        }

        if (RttMixerAware == true)
            mediaDescription.Attributes.Add(new SdpAttribute("rtt-mixer", null));

        return mediaDescription;
    }
}
