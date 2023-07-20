/////////////////////////////////////////////////////////////////////////////////////
//  File:   Qos.cs                                                  19 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Channels;

using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

/// <summary>
/// This class handles setting the Differentiated Services Code Point (DSCP) for both IPv4 and IPv6
/// for both Windows and Linux. The Windows terminology is Quality of Service (QOS).
/// </summary>
public class Qos
{
    private bool m_IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private WindowsQos m_Qos = null;
    private int m_FlowId = 0;
    private IntPtr m_SockHandle = IntPtr.Zero;

    /// <summary>
    /// Constructor
    /// </summary>
    public Qos()
    {
        if (m_IsWindows == true)
            m_Qos = new WindowsQos();
    }

    /// <summary>
    /// When running under Windows, this method must be called before the UDP/TCP connection is
    /// closed to remove the socket handle from the QOS flow and to release the handle to the Windows
    /// QOS subsystem.
    /// It is not necessary to call this method when running under Linux. Calling it performs no
    /// action.
    /// </summary>
    public void Shutdown()
    {
        if (m_Qos != null)
        {
            if (m_FlowId != 0 && m_SockHandle != IntPtr.Zero)
                m_Qos.RemoveQos(m_SockHandle, m_FlowId);

            m_Qos.Shutdown();
        }

        m_Qos = null;
        m_FlowId = 0;
        m_SockHandle = IntPtr.Zero;
    }

    /// <summary>
    /// Sets the DSCP bits in the TOS field of the IP packets sent by the UdpClient. In Windows this
    /// is done by adding the UDP socket handle to a QOS flow. In Linux this is done by setting the
    /// socket options.
    /// </summary>
    /// <param name="udpClient">UdpClient to set the DSCP value for.</param>
    /// <param name="DscpValue">DSCP value to set. Must be between 0x00 and 0x3f inclusive.</param>
    public void SetUdpDscp(UdpClient udpClient, uint DscpValue)
    {
        if (m_IsWindows == false)
        {
            int TOS = (int)DscpValue << 2;
            SocketOptionLevel level = udpClient.Client.AddressFamily == AddressFamily.InterNetwork ?
                SocketOptionLevel.IP : SocketOptionLevel.IPv6;
            udpClient.Client.SetSocketOption(level, SocketOptionName.TypeOfService, TOS);
        }
        else
        {
            m_SockHandle = udpClient.Client.Handle;
            m_FlowId = m_Qos.AddUdpQos(udpClient, DscpValue);
        }
    }

    /// <summary>
    /// Sets the DSCP bits in the TOS field of the IP packets sent by the UdpClient. In Windows this
    /// is done by adding the UDP socket handle to a QOS flow. In Linux this is done by setting the
    /// socket options.
    /// </summary>
    /// <param name="tcpClient">TcpClient object to the DSCP value for. The socket must be in the
    /// connected state.</param>
    /// <param name="DscpValue">DSCP value to set. Must be between 0x00 and 0x3f inclusive.</param>
    /// <param name="RemIpe">Remote IP endpoint of the socket.</param>
    public void SetTcpDscp(TcpClient tcpClient, uint DscpValue, IPEndPoint RemIpe)
    {
        if (m_IsWindows == false)
        {
            int TOS = (int)DscpValue << 2;
            SocketOptionLevel level = RemIpe.AddressFamily == AddressFamily.InterNetwork ?
                SocketOptionLevel.IP : SocketOptionLevel.IPv6;
            tcpClient.Client.SetSocketOption(level, SocketOptionName.TypeOfService, TOS);
        }
        else
        {
            m_SockHandle = tcpClient.Client.Handle;
            m_FlowId = m_Qos.AddTcpQos(tcpClient, DscpValue, RemIpe);
        }

    }
}



