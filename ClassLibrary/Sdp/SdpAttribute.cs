//////////////////////////////////////////////////////////////////////////////////////
//	File:	SdpAttribute.cs                                          20 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.Sdp;

/// <summary>
/// Class for processing the Attributes "a=" type of the SDP contents. See Section 5.13 of RFC 4566.
/// </summary>
public class SdpAttribute
{
    /// <summary>
    /// Contains the attribute name.
    /// </summary>
    /// <value></value>
    public string Attribute = "";
    /// <summary>
    /// Contains the attribute value. This string may be empty or null if the attribute does not have a value.
    /// </summary>
    /// <value></value>
    public string? Value = "";

    /// <summary>
    /// Contains the parameters for the SDP a= attribute. This dictionary will be empty if there are no
    /// parameters. The key is the name of the parameter and the value is the parameter value. The parameter
    /// value may be null or empty if there is no associated value for the parameter name.
    /// </summary>
    /// <value></value>
    public Dictionary<string, string> Params = new Dictionary<string, string>();

    /// <summary>
    /// Constructs a new, empty SdpAttribute object. Use this constructor when for attribute objects for
    /// SDP contents of a new SIP message.
    /// </summary>
    public SdpAttribute()
    {
    }

    /// <summary>
    /// Parses the input string and Constructs a new SdpAttribute object from the attribute information
    /// received in the SDP contents of a SIP message.
    /// </summary>
    /// <param name="strAttributeIn">The format must be either "Attribute" or "Attribute:Value".</param>
    /// <returns>A new SdpAttribute object.</returns>
    public static SdpAttribute ParseSdpAttribute(string strAttributeIn)
    {
        SdpAttribute RetVal = new SdpAttribute();
        if (strAttributeIn.Contains(":") == true)
        {   // The format is <attribute>:<value>.

            int Idx = strAttributeIn.IndexOf(':');
            RetVal.Attribute = strAttributeIn.Remove(Idx);

            char[] Delim = { ':' };
            string[] Fields = strAttributeIn.Split(Delim);
            RetVal.Value = strAttributeIn.Substring(Idx + 1).TrimStart(' ');

            // For the crypto attribute, treat the value as the rest of the entire line.
            if (RetVal.Attribute == "crypto")
                return RetVal;

            // The accept-types and path attributes are used by MSRP and the value may be a list of values
            // that are separated by spaces, so take the value as is for these attributes.
            if (RetVal.Attribute == "accept-types" || RetVal.Attribute == "path")
                return RetVal;

            Idx = RetVal.Value.IndexOf(" ");

            if (Idx >= 0)
            {   // The attribute value may contain parameters.
                string strTemp = RetVal.Value.ToString();
                RetVal.Value = RetVal.Value.Remove(Idx);

                int StartIdx = Idx + 1;
                if (StartIdx >= strTemp.Length)
                    return RetVal;     // No parameters

                strTemp = strTemp.Substring(StartIdx);
                char[] Delim2 = { ' ' };

                string[] ParamStrs = strTemp.Split(Delim2, StringSplitOptions.
                    RemoveEmptyEntries);
                string strParamName, strParamValue;
                Delim[0] = '=';
                int SubLen = 0;
                foreach (string strParam in ParamStrs)
                {
                    Idx = strParam.IndexOf("=");
                    if (Idx > 0)
                    {
                        Fields = strParam.Split(Delim, StringSplitOptions.
                            RemoveEmptyEntries);
                        if (Fields.Length == 2)
                        {
                            strParamName = Fields[0];
                            SubLen = strParam.Length - (Idx + 1);
                            if (SubLen > 0)
                                strParamValue = strParam.Substring(Idx + 1,
                                    SubLen);
                            else
                                strParamValue = Fields[1];
                        }
                        else
                            continue;   // Skip this one
                    }
                    else
                    {
                        strParamName = strParam;
                        strParamValue = null;
                    }

                    if (RetVal.Params.ContainsKey(strParamName) == true)
                        RetVal.Params[strParamName] = strParamValue!;
                    else
                        RetVal.Params.Add(strParamName, strParamValue);
                } // end foreach
            }
        }
        else
        {
            RetVal.Attribute = strAttributeIn;
            RetVal.Value = "";
        }

        return RetVal;
    }

    /// <summary>
    /// Constructs a new SdpAttribute given the attribute name and the value.
    /// </summary>
    /// <param name="AttrName">Name of the attribute.</param>
    /// <param name="AttrValue">Value of the attribute</param>
    public SdpAttribute(string AttrName, string? AttrValue)
    {
        Attribute = AttrName;
        Value = AttrValue;
    }

    /// <summary>
    /// Creates a copy of this object.
    /// </summary>
    /// <returns>A new object with a copy of each member variable.</returns>
    public SdpAttribute CreateCopy()
    {
        SdpAttribute RetVal = new SdpAttribute();
        RetVal.Attribute = Attribute;
        RetVal.Value = Value;
        foreach (KeyValuePair<String, String> Kvp in Params)
            RetVal.Params.Add(Kvp.Key, Kvp.Value);

        return RetVal;
    }

    /// <summary>
    /// Converts the SdpAttribute object to a string.
    /// </summary>
    /// <returns>The format is either "a=Attribute:Value\r\n" or
    /// "a=Attribute\r\n".</returns>
    public override string ToString()
    {
        string strReturnValue;
        if (string.IsNullOrEmpty(Value) == false)
        {	// There is a value, there may also be parameters so add them.
            if (Params.Count == 0)
            {	// No parameters
                strReturnValue = string.Format("a={0}:{1}\r\n", Attribute,
                    Value);
            }
            else
            {
                StringBuilder Sb = new StringBuilder(1024);
                Sb.AppendFormat("a={0}:{1} ", Attribute, Value);
                int Cnt = 1;
                foreach (KeyValuePair<string, string> Kvp in Params)
                {
                    if (Kvp.Value == null || Kvp.Value.Length == 0)
                        Sb.Append(Kvp.Key);
                    else
                    {
                        if (Cnt == 1)
                            Sb.AppendFormat("{0}={1}", Kvp.Key, Kvp.Value);
                        else
                            Sb.AppendFormat(" {0}={1}", Kvp.Key, Kvp.Value);
                    }

                    Cnt += 1;
                } // end foreach

                Sb.Append("\r\n");
                strReturnValue = Sb.ToString();
            }
        }
        else
            strReturnValue = string.Format("a={0}\r\n", Attribute);

        return strReturnValue;
    }

    /// <summary>
    /// Gets the parameter value for a named parameter for this SDP attribute.
    /// </summary>
    /// <param name="strParamName">Name of the SDP attribute parameter to search for.
    /// </param>
    /// <param name="strValue">Output. Value of the parameter. This may be null or empty if the named
    /// parameter is not found or if the parameter has no associated value.</param>
    /// <returns>Returns true if the parameter named in strParameter is found or false if it is not.</returns>
    public bool GetAttributeParameter(string strParamName, ref string? strValue)
    {
        bool Success = false;
        strValue = null;

        foreach (KeyValuePair<string, string> Kvp in Params)
        {
            if (Kvp.Key == strParamName)
            {
                Success = true;
                strValue = Kvp.Value;
                break;
            }
        } // end for each

        return Success;
    }
}
