/////////////////////////////////////////////////////////////////////////////////////
//	File:	WindowsQos.cs                                           19 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SipLib.Channels;

/// <summary>
/// Class for setting the Quality of Service (QOS) for UDP or TCP sockets used for transporting media 
/// or call signaling. This class is for the Windows operating systems only. It will not work when 
/// running on the Linux operating system.
/// This class handles setting the Differentiated Services Code Point (DSCP) for both IPv4 and IPv6.
/// </summary>
public class WindowsQos
{
    private IntPtr m_QHandle = IntPtr.Zero;
    private const int QOS_NON_ADAPTIVE_FLOW = 2;	// Defined in qos2.h

    /// <summary>
    /// Constructs a new Qos object. This constructor creates a Win32 handle to the Windows QOS 
    /// subsystem and maintains it until the Shutdown method is called. Therefore, the Shutdown method
    /// must be called before this object is disposed of.
    /// </summary>
    public WindowsQos()
    {
        QOS_VERSION QosVer = new QOS_VERSION();
        QosVer.MajorVersion = 1;
        QosVer.MinorVersion = 0;
        bool Success = false;

        try
        { 
            Success = WindowsQos.QOSCreateHandle(ref QosVer, out m_QHandle);
            if (Success == false)
                Debug.WriteLine("QOSCreateHandle returned false.");
        }
        catch (FileNotFoundException)
        {   // Will happen if qwave.dll is not available
            m_QHandle = IntPtr.Zero;	// Prevents further function calls
        }
        catch (DllNotFoundException)
        {   // Will happen if qwave.dll is not available.
            m_QHandle = IntPtr.Zero;	// Prevents further function calls
        }
    }

    /// <summary>
    /// Frees the handle to the underlying handle to the Windows QOS subsystem. This method must be
    /// called before this object is disposed of. Do not call any other methods of this object after
    /// this method is called.
    /// </summary>
    public void Shutdown()
    {
        if (m_QHandle != IntPtr.Zero)
        {
            bool Success = WindowsQos.QOSCloseHandle(m_QHandle);
            m_QHandle = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Adds the specified Socket object to the QOS subsystem and sets the Differentiated Servies Code
    /// Point (DSCP) value for the IP layer that will handle transport for the socket For UDP.
    /// </summary>
    /// <param name="udpClient">UdpClient to set the DSCP value for.</param>
    /// <param name="DscpValue">DSCP value to set. Must be between 0x00 and 0x3f inclusive.</param>
    /// <returns>Returns a Flow Identifier (FlowID) that the Windows QOS subsystem assigned to the
    /// socket. The caller must retain this value and use it in the call to the RemoveQos() method.
    /// This method returns a value of 0 if it was not able to setup the QOS for the socket. In this 
    /// case, the socket is still usable but the DSCP field for the IP layer will not be set.</returns>
    public int AddUdpQos(UdpClient udpClient, uint DscpValue)
    {
        int FlowId = 0;

        if (m_QHandle == IntPtr.Zero || DscpValue > 0x3f)
            return FlowId;

        IntPtr SockHandle = udpClient.Client.Handle;

        uint Dscp = DscpValue;
        SockAddr Sa = new SockAddr();
        Sa.Family = (ushort)udpClient.Client.AddressFamily;
        // Not neccessary to populate for UDP
        Sa.Data = new byte[SockAddrStructLength];

        bool Success = WindowsQos.QOSAddSocketToFlow(m_QHandle, SockHandle, ref Sa, QOS_TRAFFIC_TYPE.
            QOSTrafficTypeBestEffort, QOS_NON_ADAPTIVE_FLOW, ref FlowId);

        if (Success == false)
            return 0;

        Success = WindowsQos.QOSSetFlow(m_QHandle, FlowId, QOS_SET_FLOW.QOSSetOutgoingDSCPValue, 4, 
            ref Dscp, 0, IntPtr.Zero);

        if (Success == false)
            Debug.WriteLine("QOSSetFlow() returned false.");    

        return FlowId;
    }

    /// <summary>
    /// Adds the specified TcpClient object to the QOS subsystem and sets the Differentiated Services 
    /// Code Point (DSCP) value for the IP layer that will handle transport for the socket.
    /// Use this method for TCP sockets.
    /// </summary>
    /// <param name="TcpCli">TcpClient object to add QOS to. The socket must be in the connected state.
    /// </param>
    /// <param name="DscpValue">DSCP value to set. Must be between 0x00 and 0x3f inclusive.</param>
    /// <param name="RemIpe">Remote IP endpoint of the socket.</param>
    /// <returns>Returns a Flow Identifier (FlowID) that the Windows QOS subsystem assigned to the
    /// socket. The caller must retain this value and use it in the call to the RemoveQos() method. This
    /// method returns a value of 0 if it was not able to setup the QOS for the socket. In this case,
    /// the socket is still usable but the DSCP field for the IP layer will not be set.</returns>
    public int AddTcpQos(TcpClient TcpCli, uint DscpValue, IPEndPoint RemIpe)
    {
        int FlowId = 0;

        if (m_QHandle == IntPtr.Zero || DscpValue > 0x3f)
            return FlowId;

        uint Dscp = DscpValue;

        byte[] AddrBytes = RemIpe.Address.GetAddressBytes();

        SockAddr Sa = new SockAddr();
        Sa.Family = (ushort) RemIpe.AddressFamily;
        // The Data array must contain the port number and the IP address of the remote end point. The
        // port number is in the first 2 bytes and the IP address is in the next 4 bytes. The byte order
        // must be Big-Endian (network order).
        Sa.Data = new byte[SockAddrStructLength];
        Sa.Data[0] = (Convert.ToByte(RemIpe.Port >> 8));
        Sa.Data[1] = (Convert.ToByte(RemIpe.Port & 0xff));
        int Addr_Index = RemIpe.AddressFamily == AddressFamily.InterNetwork ? SockAddr_IPv4_Addr_Offset :
            SockAddr_IPv6_Addr_Offset;
        //Array.ConstrainedCopy(AddrBytes, 0, Sa.Data, 2, AddrBytes.Length);
        Array.ConstrainedCopy(AddrBytes, 0, Sa.Data, Addr_Index, AddrBytes.Length);

        bool Success = WindowsQos.QOSAddSocketToFlow(m_QHandle, TcpCli.Client.Handle, ref Sa, 
            QOS_TRAFFIC_TYPE.QOSTrafficTypeBestEffort, QOS_NON_ADAPTIVE_FLOW, ref FlowId);

        if (Success == false)
            return 0;

        Success = WindowsQos.QOSSetFlow(m_QHandle, FlowId, QOS_SET_FLOW.QOSSetOutgoingDSCPValue, 4, 
            ref Dscp, 0, IntPtr.Zero);
        if (Success == false)
            Debug.WriteLine("QOSSetFlow() returned false.");

        return FlowId;

    }

    /// <summary>
    /// Removes the socket from the Windows QOS subsystem. This method must be called before the socket
    /// is closed.
    /// </summary>
    /// <param name="SockHandle">Underlying Socket client object of a UDP or a TCP client socket.</param>
    /// <param name="FlowId">Flow ID of the QOS flow that was returned by the AddQos() method.</param>
    public void RemoveQos(IntPtr SockHandle, int FlowId)
    {
        if (FlowId == 0 || m_QHandle == IntPtr.Zero)
            return;

        bool Success = WindowsQos.QOSRemoveSocketFromFlow(m_QHandle, SockHandle, FlowId, 0);
        if (Success == false)
            Debug.WriteLine("QOSRemoveSocketFromFlow() returned false.");
    }

    /////////////////////////////////////////////////////////////////////////////
    // QWave DLL definitions. /PInvoke definitions for functions used from the
    // Windows qwave.dll DLL.
    /////////////////////////////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential)]
    private struct QOS_VERSION
    {
        public ushort MajorVersion;
        public ushort MinorVersion;

        public QOS_VERSION(ushort MajorVer, ushort MinorVer)
        {
            MajorVersion = MajorVer;
            MinorVersion = MinorVer;
        }
    }

    private const int SockAddrStructLength = 26;
    /// <summary>
    /// Index of the IPv4 address in SockAddr.Data
    /// </summary>
    private const int SockAddr_IPv4_Addr_Offset = 2;
    /// <summary>
    /// Index of the IPv6 address in SockAddr.Data
    /// </summary>
    private const int SockAddr_IPv6_Addr_Offset = 6;

    /// <summary>
    /// This structure is used for both the sockaddr_in (for IPv4) and the sockaddr_in6 (for IPv6)
    /// in Windows. It size is set to accomodate the larger of the two structures (sockaddr_in6).
    /// See: https://learn.microsoft.com/en-us/windows/win32/winsock/sockaddr-2
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SockAddr
    {
        public ushort Family;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SockAddrStructLength)]
        public byte[] Data;
    }

    private enum QOS_TRAFFIC_TYPE : int
    {
        QOSTrafficTypeBestEffort,
        QOSTrafficTypeBackground,
        QOSTrafficTypeExcellentEffort,
        QOSTrafficTypeAudioVideo,
        QOSTrafficTypeVoice,
        QOSTrafficTypeControl
    }

    private enum QOS_SET_FLOW : int
    {
        QOSSetTrafficType = 0,
        QOSSetOutgoingRate = 1,
        QOSSetOutgoingDSCPValue = 2
    }

    [DllImport("qwave.dll", CallingConvention=CallingConvention.Winapi, 
        SetLastError=true)]
    private static extern bool QOSCreateHandle(ref QOS_VERSION pQOSVersion, 
        out IntPtr QHandle);

    [DllImport("qwave.dll", CallingConvention = CallingConvention.Winapi, 
        SetLastError = true)]
    private static extern bool QOSAddSocketToFlow(IntPtr QHandle, IntPtr aSocket, 
        ref SockAddr DestAddr, QOS_TRAFFIC_TYPE TrafficType, int Flags, 
        ref int FlowID);

    [DllImport("qwave.dll", CallingConvention = CallingConvention.Winapi, 
        SetLastError = true)]
    private static extern bool QOSSetFlow(IntPtr QHandle, int FlowID, 
        QOS_SET_FLOW Operation, int size, ref UInt32 Buffer, 
        UInt32 Flags, IntPtr Overlapped);

    [DllImport("qwave.dll", CallingConvention = CallingConvention.Winapi, 
        SetLastError = true)]
    private static extern bool QOSRemoveSocketFromFlow(IntPtr QHandle, 
        IntPtr Socket, int FlowID, UInt32 Flags);

    [DllImport("qwave.dll", CallingConvention = CallingConvention.Winapi, 
        SetLastError = true)]
    private static extern bool QOSCloseHandle(IntPtr QHandle);
}

