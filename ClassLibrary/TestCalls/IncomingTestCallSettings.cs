/////////////////////////////////////////////////////////////////////////////////////
//  File:   IncomingTestCallSettings.cs                             25 Mar 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.TestCalls;
using System.Text.Json.Serialization;

/// <summary>
/// The settings in this class determine how to handle an incoming test call request. See Section 9 of NENA-STA-010.3b.
/// PsapSimulatorSrs.docx.
/// </summary>
public class IncomingTestCallSettings
{
    /// <summary>
    /// If true, then test calls are enabled, else they are disabled
    /// </summary>
    public bool Enable { get; set; } = true;

    /// <summary>
    /// Specifies the maximum number concurrent test calls. The minimum number is 1 and there is no upper limit.
    /// </summary>
    public int MaxTestCalls { get; set; } = 1;

    /// <summary>
    /// Specifies which duration units to use. Must be either DurationUnitsPackets or DurationUnitsMinutes. The default
    /// setting is DurationUnitsPackets (0).
    /// </summary>
    public TestCallDurationUnitsEnum DurationUnits { get; set; } = TestCallDurationUnitsEnum.DurationUnitsPackets;

    /// <summary>
    /// Specifies the number of RTP packets to receive before terminating the test call. The default is 3.
    /// </summary>
    public int DurationPackets { get; set; } = 3;

    /// <summary>
    /// Specifies the number of minutes to wait before terminating the call. The default is 1.
    /// </summary>
    public int DurationMinutes { get; set; } = 1;

    /// <summary>
    /// Constructor
    /// </summary>
    [JsonConstructor]
    public IncomingTestCallSettings()
    {
    }
}

/// <summary>
/// Enumeration for the DurationUnits property of the IncomingTestCallSettings class.
/// </summary>
public enum TestCallDurationUnitsEnum
{
    /// <summary>
    /// The call duration is determined by the number of RTP packets that are received.
    /// </summary>
    DurationUnitsPackets,

    /// <summary>
    /// The call duration is determined by the number of minutes since the call was started.
    /// </summary>
    DurationUnitsMinutes,
}