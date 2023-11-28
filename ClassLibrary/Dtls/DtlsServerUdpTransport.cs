/////////////////////////////////////////////////////////////////////////////////////
//  File:   DtlsServerUdpTransport.cs                               14 Nov 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Net.Sockets;
using System.Net;

namespace SipLib.Dtls;

/// <summary>
/// <para>
/// Class for performing the server side of the DTLS handshake for DTLS-SRTP. This class uses a .NET
/// UdpClient to communicate with the DTLS-SRTP client.
/// </para>
/// <para>
/// This class should be used to perform the DTLS-SRTP handshake only. At the end of the DTLS-SRTP handshake
/// both the client and the server will have exchanged the SRTP keying material. Call the Close() method
/// when the handshake is completed. The Close() method closes the UdpClient object so it is necessary to
/// create a new UdpClient object to communicate with the client for transporting SRTP media packets.
/// </para>
/// <para>
/// After calling the constructor, call the DoHandshake() method of the DtlsSrtpTransport object to start
/// the DTLS-SRTP handshake process.
/// </para>
/// </summary>
public class DtlsServerUdpTransport
{
    private UdpClient m_UdpClient;
    private IPEndPoint m_RemIpEndPoint;
    private DtlsSrtpTransport m_dtlsSrtpServerTransport;
    private Thread m_Thread;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="udpClient">UdpClient object to use for sending data to and receiving data from the DTLS-SRTP
    /// client</param>
    /// <param name="remIpEndPoint">Remote endpoint of the DTLS-SRTP client</param>
    /// <param name="dtlsSrtpServerTransport">Transport to use for managing the DTLS-SRTP server handshake</param>
    public DtlsServerUdpTransport(UdpClient udpClient, IPEndPoint remIpEndPoint, DtlsSrtpTransport 
        dtlsSrtpServerTransport)
    {
        m_UdpClient = udpClient;
        m_RemIpEndPoint = remIpEndPoint;
        m_dtlsSrtpServerTransport = dtlsSrtpServerTransport;
        m_dtlsSrtpServerTransport.OnDataReady += DataReady;

        m_Thread = new Thread(ReceiveThread);
        m_Thread.IsBackground = true;
        m_Thread.Priority = ThreadPriority.Highest;
        m_Thread.Start();
    }

    /// <summary>
    /// Event handler for the DtlsSrtpTransport's OnDataReady event. The DtlsSrtpTransport object fires this
    /// event when it needs to send data to the DTLS-SRTP client.
    /// </summary>
    /// <param name="buf">DTLS-SRTP handshake message to send to the client</param>
    private void DataReady(byte[] buf)
    {
        Console.WriteLine($"DTLS server transport sending {buf.Length} bytes to client.");
        m_UdpClient.Send(buf, buf.Length, m_RemIpEndPoint);
    }

    private bool m_IsEnding = false;

    private void ReceiveThread()
    {
        IPEndPoint Ipe = null;
        try
        {
            while (m_IsEnding == false)
            {
                byte[] buf = m_UdpClient.Receive(ref Ipe);
                if (buf != null && buf.Length > 0)
                {
                    m_dtlsSrtpServerTransport.WriteToRecvStream(buf);
                }
            }
        }
        catch (SocketException) { }
        catch (InvalidOperationException) { }
        catch (Exception) { }
    }

    /// <summary>
    /// Closes the UdpClient which forces the receive thread to terminate.
    /// </summary>
    public void Close()
    {
        m_IsEnding = true;
        m_UdpClient.Close();
        m_Thread.Join();
    }

}
