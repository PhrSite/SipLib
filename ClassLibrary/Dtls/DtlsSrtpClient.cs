//-----------------------------------------------------------------------------
// Filename: DtlsSrtpClient.cs
//
// Description: This class represents the DTLS SRTP client connection handler.
//
// Author(s):
// Rafael Soares (raf.csoares@kyubinteractive.com)
//
// History:
// 01 Jul 2020	Rafael Soares   Created.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

//  Revised: 17 Nov 23 PHR
//      -- Changed namespace to SipLib.Dtls from SIPSorcery.Net
//      -- Added documentation comments and code cleanup

using System.Collections;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace SipLib.Dtls
{
    /// <summary>
    /// Class for authentication information for DTLS-SRTP
    /// </summary>
    internal class DtlsSrtpTlsAuthentication : TlsAuthentication
    {
        private readonly DtlsSrtpClient mClient;
        private readonly TlsContext mContext;

        internal DtlsSrtpTlsAuthentication(DtlsSrtpClient client)
        {
            this.mClient = client;
            this.mContext = client.TlsContext;
        }

        public virtual void NotifyServerCertificate(Certificate serverCertificate)
        {
            //Console.WriteLine("DTLS client received server certificate chain of length " + chain.Length);
            mClient.ServerCertificate = serverCertificate;
        }

        public virtual TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
        {
            byte[] certificateTypes = certificateRequest.CertificateTypes;
            if (certificateTypes == null || !Arrays.Contains(certificateTypes, ClientCertificateType.rsa_sign))
            {
                return null;
            }

            return DtlsUtils.LoadSignerCredentials(mContext,
                certificateRequest.SupportedSignatureAlgorithms,
                SignatureAlgorithm.rsa,
                mClient.mCertificateChain,
                mClient.mPrivateKey);
        }

        public TlsCredentials? GetClientCredentials(TlsContext context, CertificateRequest certificateRequest)
        {
            return GetClientCredentials(certificateRequest);
        }
    };

    /// <summary>
    /// Class for a DTLS-SRTP handshake client.
    /// </summary>
    public class DtlsSrtpClient : DefaultTlsClient, IDtlsSrtpPeer
    {

        internal Certificate? mCertificateChain = null;
        internal AsymmetricKeyParameter? mPrivateKey = null;

        internal TlsClientContext TlsContext
        {
            get { return mContext; }
        }

        /// <summary>
        /// Contains the Org.BouncyCastle.Crypto.Tls.TlsSession
        /// </summary>
        /// <value></value>
        protected internal TlsSession? mSession;

        /// <summary>
        /// Gets or sets a flag to indicate whether or not to force the use of the extended MasterSecret.
        /// Defaults to true.
        /// </summary>
        /// <value></value>
        public bool ForceUseExtendedMasterSecret { get; set; } = true;

        /// <summary>
        /// Gets the Certificate received from the server.
        /// </summary>
        /// <value></value>
        public Certificate ServerCertificate { get; internal set; }

        /// <summary>
        /// Gets the fingerprint of the X.509 certificate used by this client
        /// </summary>
        /// <value></value>
        public RTCDtlsFingerprint? Fingerprint { get; private set; } = null;

        private UseSrtpData clientSrtpData;

        // Asymmetric shared keys derived from the DTLS handshake and used for the SRTP encryption/
        private byte[] srtpMasterClientKey;
        private byte[] srtpMasterServerKey;
        private byte[] srtpMasterClientSalt;
        private byte[] srtpMasterServerSalt;
        private byte[]? masterSecret = null;

        // Policies
        private SrtpPolicy srtpPolicy;
        private SrtpPolicy srtcpPolicy;

        /// <summary>
        /// Event that is fired when an Alert is received from the server during the DTLS handshake
        /// </summary>
        /// <value></value>
        public event Action<AlertLevelsEnum, AlertTypesEnum, string>? OnAlert;

        /// <summary>
        /// Constructor. Creates a self-signed certificate.
        /// </summary>
        public DtlsSrtpClient() : this(null!, null!, null!)
        {
        }

        /// <summary>
        /// Constructor. Creates a self-signed certificate from a .NET X509Certificate2
        /// </summary>
        /// <param name="certificate">Input certificate</param>
        public DtlsSrtpClient(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) :
            this(DtlsUtils.LoadCertificateChain(certificate), DtlsUtils.LoadPrivateKeyResource(certificate))
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="certificatePath">File path to the certificate file</param>
        /// <param name="keyPath">File path to the private key file</param>
        public DtlsSrtpClient(string certificatePath, string keyPath) : this(new string[] { certificatePath }, 
            keyPath)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="certificatesPath"></param>
        /// <param name="keyPath"></param>
        public DtlsSrtpClient(string[] certificatesPath, string keyPath) :
            this(DtlsUtils.LoadCertificateChain(certificatesPath), DtlsUtils.LoadPrivateKeyResource(keyPath))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="certificateChain"></param>
        /// <param name="privateKey"></param>
        public DtlsSrtpClient(Certificate certificateChain, AsymmetricKeyParameter privateKey) :
            this(certificateChain, privateKey, null!)
        {
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="certificateChain">Contains at least one X.509 certificate. If null, then a self-signed
        /// certificate will be automatically created.</param>
        /// <param name="privateKey">Private key for the certificate</param>
        /// <param name="clientSrtpData">BouncyCastle UseSrtpData object to use. May be null. If null the
        /// a UseSrtpData object will be created. The UseSrtpData class contains the SRTP protection
        /// profiles and the Master Key Index that will be negotiated during the DTLS handshake process.</param>
        public DtlsSrtpClient(Certificate certificateChain, AsymmetricKeyParameter privateKey, UseSrtpData clientSrtpData)
        {
            if (certificateChain == null && privateKey == null)
            {
                (certificateChain, privateKey) = DtlsUtils.CreateSelfSignedTlsCert();
            }

            if (clientSrtpData == null)
            {
                SecureRandom random = new SecureRandom();
                int[] protectionProfiles = { SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_80 };
                byte[] mki = new byte[(SrtpParameters.SRTP_AES128_CM_HMAC_SHA1_80.GetCipherKeyLength() + SrtpParameters.SRTP_AES128_CM_HMAC_SHA1_80.GetCipherSaltLength()) / 8];
                random.NextBytes(mki); // Reusing our secure random for generating the key.
                this.clientSrtpData = new UseSrtpData(protectionProfiles, mki);
            }
            else
            {
                this.clientSrtpData = clientSrtpData;
            }

            this.mPrivateKey = privateKey;
            mCertificateChain = certificateChain;

            //Generate FingerPrint
            var certificate = mCertificateChain!.GetCertificateAt(0);
            Fingerprint = certificate != null ? DtlsUtils.Fingerprint(certificate) : null;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientSrtpData"></param>
        public DtlsSrtpClient(UseSrtpData clientSrtpData) : this(null!, null!, clientSrtpData)
        { }

        /// <summary>
        /// Gets the DTLS extensions for this DTLS-SRTP client.
        /// </summary>
        /// <returns>Returns a dictionary containing the client DTLS extensions</returns>
        public override IDictionary GetClientExtensions()
        {
            var clientExtensions = base.GetClientExtensions();
            if (TlsSRTPUtils.GetUseSrtpExtension(clientExtensions) == null)
            {
                if (clientExtensions == null)
                {
                    clientExtensions = new Hashtable();
                }

                TlsSRTPUtils.AddUseSrtpExtension(clientExtensions, clientSrtpData);
            }
            return clientExtensions;
        }

        /// <summary>
        /// Processes the DTLS handshake extensions received from the DTLS server
        /// </summary>
        /// <param name="clientExtensions"></param>
        public override void ProcessServerExtensions(IDictionary clientExtensions)
        {
            base.ProcessServerExtensions(clientExtensions);

            // set to some reasonable default value
            int chosenProfile = SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_80;
            UseSrtpData clientSrtpData = TlsSRTPUtils.GetUseSrtpExtension(clientExtensions);

            foreach (int profile in clientSrtpData.ProtectionProfiles)
            {
                switch (profile)
                {
                    case SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_32:
                    case SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_80:
                    case SrtpProtectionProfile.SRTP_NULL_HMAC_SHA1_32:
                    case SrtpProtectionProfile.SRTP_NULL_HMAC_SHA1_80:
                        chosenProfile = profile;
                        break;
                }
            }

            // server chooses a mutually supported SRTP protection profile
            // http://tools.ietf.org/html/draft-ietf-avt-dtls-srtp-07#section-4.1.2
            int[] protectionProfiles = { chosenProfile };

            // server agrees to use the MKI offered by the client
            clientSrtpData = new UseSrtpData(protectionProfiles, clientSrtpData.Mki);
        }

        /// <summary>
        /// Gets the SRTP encryption and authentication policy information for the DTLS-SRTP session
        /// </summary>
        /// <returns></returns>
        public virtual SrtpPolicy GetSrtpPolicy()
        {
            return srtpPolicy;
        }

        /// <summary>
        /// Gets the SRTCP encryption and authenticaion policy information for the DTLS-SRTP session
        /// </summary>
        /// <returns></returns>
        public virtual SrtpPolicy GetSrtcpPolicy()
        {
            return srtcpPolicy;
        }


        /// <summary>
        /// Gets the server's master key for SRTP
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetSrtpMasterServerKey()
        {
            return srtpMasterServerKey;
        }

        /// <summary>
        /// Gets the server's master salt
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetSrtpMasterServerSalt()
        {
            return srtpMasterServerSalt;
        }

        /// <summary>
        /// Gets the client's master key
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetSrtpMasterClientKey()
        {
            return srtpMasterClientKey;
        }

        /// <summary>
        /// Gets the client's master salt
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetSrtpMasterClientSalt()
        {
            return srtpMasterClientSalt;
        }

        /// <summary>
        /// Gets the authentication information
        /// </summary>
        /// <returns></returns>
        public override TlsAuthentication GetAuthentication()
        {
            return new DtlsSrtpTlsAuthentication(this);
        }

        /// <summary>
        /// Called when the DTLS handshake is completed
        /// </summary>
        public override void NotifyHandshakeComplete()
        {
            base.NotifyHandshakeComplete();

            //Copy master Secret (will be inaccessible after this call)
            masterSecret = new byte[mContext.SecurityParameters.MasterSecret != null ?
                mContext.SecurityParameters.MasterSecret.Length : 0];
            Buffer.BlockCopy(mContext.SecurityParameters.MasterSecret, 0, masterSecret, 0, masterSecret.Length);

            //Prepare Srtp Keys (we must to it here because master key will be cleared after that)
            PrepareSrtpSharedSecret();
        }

        /// <summary>
        /// Always returns true because this object is the DTLS client
        /// </summary>
        /// <returns></returns>
        public bool IsClient()
        {
            return true;
        }

        /// <summary>
        /// Gets the keying material (master keys and master salts)
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected byte[] GetKeyingMaterial(int length)
        {
            return GetKeyingMaterial(ExporterLabel.dtls_srtp, null!, length);
        }

        /// <summary>
        /// Gets the keying material (master keys and master salts)
        /// </summary>
        /// <param name="asciiLabel"></param>
        /// <param name="context_value"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        // <exception cref="ArgumentException"></exception>
        // <exception cref="InvalidOperationException"></exception>
        protected virtual byte[] GetKeyingMaterial(string asciiLabel, byte[] context_value, int length)
        {
            if (context_value != null && !TlsUtilities.IsValidUint16(context_value.Length))
            {
                throw new ArgumentException("must have length less than 2^16 (or be null)", "context_value");
            }

            SecurityParameters sp = mContext.SecurityParameters;
            if (!sp.IsExtendedMasterSecret && RequiresExtendedMasterSecret())
            {
                /*
                 * RFC 7627 5.4. If a client or server chooses to continue with a full handshake without
                 * the extended master secret extension, [..] the client or server MUST NOT export any
                 * key material based on the new master secret for any subsequent application-level
                 * authentication. In particular, it MUST disable [RFC5705] [..].
                 */
                throw new InvalidOperationException("cannot export keying material without extended_master_secret");
            }

            byte[] cr = sp.ClientRandom, sr = sp.ServerRandom;

            int seedLength = cr.Length + sr.Length;
            if (context_value != null)
            {
                seedLength += (2 + context_value.Length);
            }

            byte[] seed = new byte[seedLength];
            int seedPos = 0;

            Array.Copy(cr, 0, seed, seedPos, cr.Length);
            seedPos += cr.Length;
            Array.Copy(sr, 0, seed, seedPos, sr.Length);
            seedPos += sr.Length;
            if (context_value != null)
            {
                TlsUtilities.WriteUint16(context_value.Length, seed, seedPos);
                seedPos += 2;
                Array.Copy(context_value, 0, seed, seedPos, context_value.Length);
                seedPos += context_value.Length;
            }

            if (seedPos != seedLength)
            {
                throw new InvalidOperationException("error in calculation of seed for export");
            }

            return TlsUtilities.PRF(mContext, sp.MasterSecret, asciiLabel, seed, length);
        }

        /// <summary>
        /// Returns true if an extended master secret is required.
        /// </summary>
        /// <returns></returns>
        public override bool RequiresExtendedMasterSecret()
        {
            return ForceUseExtendedMasterSecret;
        }

        /// <summary>
        /// Prepares the SRTP-DTLS shared secret for the DTLS-SRTP handshake
        /// </summary>
        protected virtual void PrepareSrtpSharedSecret()
        {
            //Set master secret back to security parameters (only works in old bouncy castle versions)
            //mContext.SecurityParameters.MasterSecret = masterSecret;

            SrtpParameters srtpParams = SrtpParameters.GetSrtpParametersForProfile(clientSrtpData.
                ProtectionProfiles[0]);
            int keyLen = srtpParams.GetCipherKeyLength();
            int saltLen = srtpParams.GetCipherSaltLength();

            srtpPolicy = srtpParams.GetSrtpPolicy();
            srtcpPolicy = srtpParams.GetSrtcpPolicy();

            srtpMasterClientKey = new byte[keyLen];
            srtpMasterServerKey = new byte[keyLen];
            srtpMasterClientSalt = new byte[saltLen];
            srtpMasterServerSalt = new byte[saltLen];

            // 2* (key + salt length) / 8. From http://tools.ietf.org/html/rfc5764#section-4-2
            // No need to divide by 8 here since lengths are already in bits
            byte[] sharedSecret = GetKeyingMaterial(2 * (keyLen + saltLen));

            /*
             * 
             * See: http://tools.ietf.org/html/rfc5764#section-4.2
             * 
             * sharedSecret is an equivalent of :
             * 
             * struct {
             *     client_write_SRTP_master_key[SRTPSecurityParams.master_key_len];
             *     server_write_SRTP_master_key[SRTPSecurityParams.master_key_len];
             *     client_write_SRTP_master_salt[SRTPSecurityParams.master_salt_len];
             *     server_write_SRTP_master_salt[SRTPSecurityParams.master_salt_len];
             *  } ;
             *
             * Here, client = local configuration, server = remote.
             * NOTE [ivelin]: 'local' makes sense if this code is used from a DTLS SRTP client. 
             *                Here we run as a server, so 'local' referring to the client is actually confusing. 
             * 
             * l(k) = KEY length
             * s(k) = salt lenght
             * 
             * So we have the following repartition :
             *                           l(k)                                 2*l(k)+s(k)   
             *                                                   2*l(k)                       2*(l(k)+s(k))
             * +------------------------+------------------------+---------------+-------------------+
             * + local key           |    remote key    | local salt   | remote salt   |
             * +------------------------+------------------------+---------------+-------------------+
             */
            Buffer.BlockCopy(sharedSecret, 0, srtpMasterClientKey, 0, keyLen);
            Buffer.BlockCopy(sharedSecret, keyLen, srtpMasterServerKey, 0, keyLen);
            Buffer.BlockCopy(sharedSecret, 2 * keyLen, srtpMasterClientSalt, 0, saltLen);
            Buffer.BlockCopy(sharedSecret, (2 * keyLen + saltLen), srtpMasterServerSalt, 0, saltLen);
        }

        /// <summary>
        /// Gets the protcol version for this client
        /// </summary>
        /// <value></value>
        public override ProtocolVersion ClientVersion
        {
            get { return ProtocolVersion.DTLSv12; }
        }

        /// <summary>
        /// Gets the minimum version support by this client
        /// </summary>
        /// <value></value>
        public override ProtocolVersion MinimumVersion
        {
            get { return ProtocolVersion.DTLSv10; }
        }

        /// <summary>
        /// Gets the DTLS-SRTP session to resume
        /// </summary>
        /// <returns></returns>
        public override TlsSession GetSessionToResume()
        {
            return this.mSession!;
        }

        /// <summary>
        /// Called by the transport if a DTLS-SRTP protocol alert has been raised.
        /// </summary>
        /// <param name="alertLevel"></param>
        /// <param name="alertDescription"></param>
        /// <param name="message"></param>
        /// <param name="cause"></param>
        public override void NotifyAlertRaised(byte alertLevel, byte alertDescription, string message, 
            Exception cause)
        {
            string description = null;
            if (message != null)
            {
                description += message;
            }
            if (cause != null)
            {
                description += cause;
            }

            string alertMessage = $"{AlertLevel.GetText(alertLevel)}, {AlertDescription.GetText(alertDescription)}";
            alertMessage += !string.IsNullOrEmpty(description) ? $", {description}." : ".";
        }

        /// <summary>
        /// Called during the protocol handshake to set the protocol version of the server
        /// </summary>
        /// <param name="serverVersion"></param>
        public override void NotifyServerVersion(ProtocolVersion serverVersion)
        {
            base.NotifyServerVersion(serverVersion);
        }

        /// <summary>
        /// Gets the Certificate of the server
        /// </summary>
        /// <returns></returns>
        public Certificate GetRemoteCertificate()
        {
            return ServerCertificate;
        }

        /// <summary>
        /// Called if a protocol Alert was received
        /// </summary>
        /// <param name="alertLevel"></param>
        /// <param name="alertDescription"></param>
        public override void NotifyAlertReceived(byte alertLevel, byte alertDescription)
        {
            string description = AlertDescription.GetText(alertDescription);

            AlertLevelsEnum level = AlertLevelsEnum.Warning;
            AlertTypesEnum alertType = AlertTypesEnum.unknown;

            if (Enum.IsDefined(typeof(AlertLevelsEnum), alertLevel))
            {
                level = (AlertLevelsEnum)alertLevel;
            }

            if (Enum.IsDefined(typeof(AlertTypesEnum), alertDescription))
            {
                alertType = (AlertTypesEnum)alertDescription;
            }

            OnAlert?.Invoke(level, alertType, description);
        }
    }
}
