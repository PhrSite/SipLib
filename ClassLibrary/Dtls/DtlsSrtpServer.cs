//-----------------------------------------------------------------------------
// Filename: DtlsSrtpServer.cs
//
// Description: This class represents the DTLS SRTP server connection handler.
//
// Derived From:
// https://github.com/RestComm/media-core/blob/master/rtp/src/main/java/org/restcomm/media/core/rtp/crypto/DtlsSrtpServer.java
//
// Author(s):
// Rafael Soares (raf.csoares@kyubinteractive.com)
//
// History:
// 01 Jul 2020	Rafael Soares   Created.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
// Original Source: AGPL-3.0 License
//-----------------------------------------------------------------------------

//  Revised: 17 Nov 23 PHR
//      -- Changed namespace to SipLib.Dtls from SIPSorcery.Net
//      -- Added documentation comments and code cleanup
//      -- Commented out unused constructors. The constructors that takes a .NET X509Certificate2 object
//         cause an exception to be thrown.
//      -- Commented out GetECDsaSignerCredentials(), GetRSAEncryptionCredentials(), GetRsaSignerCredentials()
//         because they are not used.

using System.Collections;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Utilities;

namespace SipLib.Dtls
{
    /// <summary>
    /// Enumeration of the protocol alert levels
    /// </summary>
    public enum AlertLevelsEnum : byte
    {
        /// <summary>
        /// Only a warning
        /// </summary>
        Warning = 1,
        /// <summary>
        /// Fatal alert level, the handshake failed
        /// </summary>
        Fatal = 2
    }

    /// <summary>
    /// Enumeration of the protocol Alert types
    /// </summary>
    public enum AlertTypesEnum : byte
    {
        /// <summary>
        /// </summary>
        close_notify = 0,
        /// <summary>
        /// </summary>
        unexpected_message = 10,
        /// <summary>
        /// </summary>
        bad_record_mac = 20,
        /// <summary>
        /// </summary>
        decryption_failed = 21,
        /// <summary>
        /// </summary>
        record_overflow = 22,
        /// <summary>
        /// </summary>
        decompression_failure = 30,
        /// <summary>
        /// </summary>
        handshake_failure = 40,
        /// <summary>
        /// </summary>
        no_certificate = 41,
        /// <summary>
        /// </summary>
        bad_certificate = 42,
        /// <summary>
        /// </summary>
        unsupported_certificate = 43,
        /// <summary>
        /// </summary>
        certificate_revoked = 44,
        /// <summary>
        /// </summary>
        certificate_expired = 45,
        /// <summary>
        /// </summary>
        certificate_unknown = 46,
        /// <summary>
        /// </summary>
        illegal_parameter = 47,
        /// <summary>
        /// </summary>
        unknown_ca = 48,
        /// <summary>
        /// </summary>
        access_denied = 49,
        /// <summary>
        /// </summary>
        decode_error = 50,
        /// <summary>
        /// </summary>
        decrypt_error = 51,
        /// <summary>
        /// </summary>
        export_restriction = 60,
        /// <summary>
        /// </summary>
        protocol_version = 70,
        /// <summary>
        /// </summary>
        insufficient_security = 71,
        /// <summary>
        /// </summary>
        internal_error = 80,
        /// <summary>
        /// </summary>
        inappropriate_fallback = 86,
        /// <summary>
        /// </summary>
        user_canceled = 90,
        /// <summary>
        /// </summary>
        no_renegotiation = 100,
        /// <summary>
        /// </summary>
        unsupported_extension = 110,
        /// <summary>
        /// </summary>
        certificate_unobtainable = 111,
        /// <summary>
        /// </summary>
        unrecognized_name = 112,
        /// <summary>
        /// </summary>
        bad_certificate_status_response = 113,
        /// <summary>
        /// </summary>
        bad_certificate_hash_value = 114,
        /// <summary>
        /// </summary>
        unknown_psk_identity = 115,
        /// <summary>
        /// </summary>
        unknown = 255
    }

    /// <summary>
    /// Definition of the interface that must be implement by the DtlsSrtpClient and the DtlsSrtpServer
    /// </summary>
    public interface IDtlsSrtpPeer
    {
        /// <summary>
        /// Event that will be fired when a protocol handshake alert is received or raised
        /// </summary>
        event Action<AlertLevelsEnum, AlertTypesEnum, string> OnAlert;
        /// <summary>
        /// Returns true if use of the extended master secret is to be forced
        /// </summary>
        bool ForceUseExtendedMasterSecret { get; set; }
        /// <summary>
        /// Gets the DTLS-SRTP encryption and authentication policy information
        /// </summary>
        /// <returns></returns>
        SrtpPolicy GetSrtpPolicy();
        /// <summary>
        /// Gets the DTLS-SRTCP encryption and authentication policy information
        /// </summary>
        /// <returns></returns>
        SrtpPolicy GetSrtcpPolicy();
        /// <summary>
        /// Gets the server's master key
        /// </summary>
        /// <returns></returns>
        byte[] GetSrtpMasterServerKey();
        /// <summary>
        /// Gets the server's master salt
        /// </summary>
        /// <returns></returns>
        byte[] GetSrtpMasterServerSalt();
        /// <summary>
        /// Gets the client's master key
        /// </summary>
        /// <returns></returns>
        byte[] GetSrtpMasterClientKey();
        /// <summary>
        /// Gets the client's master salt
        /// </summary>
        /// <returns></returns>
        byte[] GetSrtpMasterClientSalt();
        /// <summary>
        /// Returns true if the implementation is for a client or false if it is for a server
        /// </summary>
        /// <returns></returns>
        bool IsClient();
        /// <summary>
        /// Gets the remote endpoint's X.509 certificate
        /// </summary>
        /// <returns></returns>
        Certificate GetRemoteCertificate();
    }

    /// <summary>
    /// Class for a DTLS-SRTP handshake server
    /// </summary>
    public class DtlsSrtpServer : DefaultTlsServer, IDtlsSrtpPeer
    {
        Certificate mCertificateChain = null;
        AsymmetricKeyParameter mPrivateKey = null;

        private RTCDtlsFingerprint mFingerPrint;

        /// <summary>
        /// Gets or sets a flag to indicate whether or not to force the use of the extended MasterSecret.
        /// Defaults to true.
        /// </summary>
        public bool ForceUseExtendedMasterSecret { get; set; } = true;

        /// <summary>
        /// Gets the client's X.509 certificate
        /// </summary>
        public Certificate ClientCertificate { get; private set; }

        // the server response to the client handshake request
        // http://tools.ietf.org/html/rfc5764#section-4.1.1
        private UseSrtpData serverSrtpData;

        // Asymmetric shared keys derived from the DTLS handshake and used for the SRTP encryption/
        private byte[] srtpMasterClientKey;
        private byte[] srtpMasterServerKey;
        private byte[] srtpMasterClientSalt;
        private byte[] srtpMasterServerSalt;
        byte[] masterSecret = null;

        // Policies
        private SrtpPolicy srtpPolicy;
        private SrtpPolicy srtcpPolicy;

        private int[] cipherSuites;

        /// <summary>
        /// This event is fired if an Alert message was received during the DTLS protocol handshake
        /// </summary>
        public event Action<AlertLevelsEnum, AlertTypesEnum, string> OnAlert;

        /// <summary>
        /// Constructor. Creates a self-signed certificate.
        /// </summary>
        public DtlsSrtpServer() : this((Certificate)null, null)
        {
        }

        //public DtlsSrtpServer(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) : this(DtlsUtils.LoadCertificateChain(certificate), DtlsUtils.LoadPrivateKeyResource(certificate))
        //{
        //}

        //public DtlsSrtpServer(string certificatePath, string keyPath) : this(new string[] { certificatePath }, keyPath)
        //{
        //}

        //public DtlsSrtpServer(string[] certificatesPath, string keyPath) :
        //    this(DtlsUtils.LoadCertificateChain(certificatesPath), DtlsUtils.LoadPrivateKeyResource(keyPath))
        //{
        //}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="certificateChain">Contains at least one X.509 certificate. If null, then a self-signed
        /// certificate will be automatically created.</param>
        /// <param name="privateKey">Private key for the certificate</param>
        public DtlsSrtpServer(Certificate certificateChain, AsymmetricKeyParameter privateKey)
        {
            if (certificateChain == null && privateKey == null)
                (certificateChain, privateKey) = DtlsUtils.CreateSelfSignedTlsCert();

            this.cipherSuites = base.GetCipherSuites();

            this.mPrivateKey = privateKey;
            mCertificateChain = certificateChain;

            //Generate FingerPrint
            var certificate = mCertificateChain.GetCertificateAt(0);

            this.mFingerPrint = certificate != null ? DtlsUtils.Fingerprint(certificate) : null;
        }

        /// <summary>
        /// Gets the fingerprint for the certificate.
        /// </summary>
        public RTCDtlsFingerprint Fingerprint
        {
            get
            {
                return mFingerPrint;
            }
        }

        /// <summary>
        /// Gets the private key for the certificate.
        /// </summary>
        public AsymmetricKeyParameter PrivateKey
        {
            get
            {
                return mPrivateKey;
            }
        }

        /// <summary>
        /// Gets the certificate change containing the certificate
        /// </summary>
        public Certificate CertificateChain
        {
            get
            {
                return mCertificateChain;
            }
        }

        /// <summary>
        /// Gets the maximum supported DTLS protocol version
        /// </summary>
        protected override ProtocolVersion MaximumVersion
        {
            get
            {
                return ProtocolVersion.DTLSv12;
            }
        }

        /// <summary>
        /// Gets the minimum supported DTLS version
        /// </summary>
        protected override ProtocolVersion MinimumVersion
        {
            get
            {
                return ProtocolVersion.DTLSv10;
            }
        }

        /// <summary>
        /// Gets the cipher suite ID that was selected.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="TlsFatalAlert"></exception>
        public override int GetSelectedCipherSuite()
        {
            /*
             * TODO RFC 5246 7.4.3. In order to negotiate correctly, the server MUST check any candidate cipher suites against the
             * "signature_algorithms" extension before selecting them. This is somewhat inelegant but is a compromise designed to
             * minimize changes to the original cipher suite design.
             */

            /*
             * RFC 4429 5.1. A server that receives a ClientHello containing one or both of these extensions MUST use the client's
             * enumerated capabilities to guide its selection of an appropriate cipher suite. One of the proposed ECC cipher suites
             * must be negotiated only if the server can successfully complete the handshake while using the curves and point
             * formats supported by the client [...].
             */
            bool eccCipherSuitesEnabled = SupportsClientEccCapabilities(this.mNamedCurves, this.mClientECPointFormats);

            int[] cipherSuites = GetCipherSuites();
            for (int i = 0; i < cipherSuites.Length; ++i)
            {
                int cipherSuite = cipherSuites[i];

                if (Arrays.Contains(this.mOfferedCipherSuites, cipherSuite)
                        && (eccCipherSuitesEnabled || !TlsEccUtilities.IsEccCipherSuite(cipherSuite))
                        && TlsUtilities.IsValidCipherSuiteForVersion(cipherSuite, mServerVersion))
                    return this.mSelectedCipherSuite = cipherSuite;
            }

            throw new TlsFatalAlert(AlertDescription.handshake_failure);
        }

        /// <summary>
        /// Gets the certificate request
        /// </summary>
        /// <returns></returns>
        public override CertificateRequest GetCertificateRequest()
        {
            List<SignatureAndHashAlgorithm> serverSigAlgs = new List<SignatureAndHashAlgorithm>();

            if (TlsUtilities.IsSignatureAlgorithmsExtensionAllowed(mServerVersion))
            {
                byte[] hashAlgorithms = new byte[] { HashAlgorithm.sha512, HashAlgorithm.sha384, HashAlgorithm.sha256, HashAlgorithm.sha224, HashAlgorithm.sha1 };
                byte[] signatureAlgorithms = new byte[] { SignatureAlgorithm.rsa, SignatureAlgorithm.ecdsa };

                serverSigAlgs = new List<SignatureAndHashAlgorithm>();
                for (int i = 0; i < hashAlgorithms.Length; ++i)
                {
                    for (int j = 0; j < signatureAlgorithms.Length; ++j)
                    {
                        serverSigAlgs.Add(new SignatureAndHashAlgorithm(hashAlgorithms[i], signatureAlgorithms[j]));
                    }
                }
            }
            return new CertificateRequest(new byte[] { ClientCertificateType.rsa_sign }, serverSigAlgs, null);
        }

        /// <summary>
        /// Called when the client certificate has been received during the handshake
        /// </summary>
        /// <param name="clientCertificate"></param>
        public override void NotifyClientCertificate(Certificate clientCertificate)
        {
            ClientCertificate = clientCertificate;
        }

        /// <summary>
        /// Gets the server's DTLS extensions
        /// </summary>
        /// <returns></returns>
        public override IDictionary GetServerExtensions()
        {
            Hashtable serverExtensions = (Hashtable)base.GetServerExtensions();
            if (TlsSRTPUtils.GetUseSrtpExtension(serverExtensions) == null)
            {
                if (serverExtensions == null)
                    serverExtensions = new Hashtable();

                TlsSRTPUtils.AddUseSrtpExtension(serverExtensions, serverSrtpData);
            }

            return serverExtensions;
        }

        /// <summary>
        /// Called to process the client's DTLS protocol externsions when they are received.
        /// </summary>
        /// <param name="clientExtensions">The client's extensions</param>
        public override void ProcessClientExtensions(IDictionary clientExtensions)
        {
            base.ProcessClientExtensions(clientExtensions);

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
            serverSrtpData = new UseSrtpData(protectionProfiles, clientSrtpData.Mki);
        }

        /// <summary>
        /// Gets the SRTP encryption and authentication policy information for the DTLS-SRTP session
        /// </summary>
        /// <returns></returns>
        public SrtpPolicy GetSrtpPolicy()
        {
            return srtpPolicy;
        }

        /// <summary>
        /// Gets the SRTCP encryption and authenticaion policy information for the DTLS-SRTP session
        /// </summary>
        /// <returns></returns>
        public SrtpPolicy GetSrtcpPolicy()
        {
            return srtcpPolicy;
        }

        /// <summary>
        /// Gets the server's master key for SRTP
        /// </summary>
        /// <returns></returns>
        public byte[] GetSrtpMasterServerKey()
        {
            return srtpMasterServerKey;
        }

        /// <summary>
        /// Gets the server's master salt for SRTP
        /// </summary>
        /// <returns></returns>
        public byte[] GetSrtpMasterServerSalt()
        {
            return srtpMasterServerSalt;
        }

        /// <summary>
        /// Gets the client's master SRTP key
        /// </summary>
        /// <returns></returns>
        public byte[] GetSrtpMasterClientKey()
        {
            return srtpMasterClientKey;
        }

        /// <summary>
        /// Gets the client's master salt
        /// </summary>
        /// <returns></returns>
        public byte[] GetSrtpMasterClientSalt()
        {
            return srtpMasterClientSalt;
        }

        /// <summary>
        /// Called when the DTLS handshake is completed
        /// </summary>
        public override void NotifyHandshakeComplete()
        {
            //Copy master Secret (will be inaccessible after this call)
            masterSecret = new byte[mContext.SecurityParameters.MasterSecret != null ? mContext.SecurityParameters.MasterSecret.Length : 0];
            Buffer.BlockCopy(mContext.SecurityParameters.MasterSecret, 0, masterSecret, 0, masterSecret.Length);

            //Prepare Srtp Keys (we must to it here because master key will be cleared after that)
            PrepareSrtpSharedSecret();
        }

        /// <summary>
        /// Always returns false because this object is the DTLS server
        /// </summary>
        /// <returns></returns>
        public bool IsClient()
        {
            return false;
        }

        //protected override TlsSignerCredentials GetECDsaSignerCredentials()
        //{
        //    return DtlsUtils.LoadSignerCredentials(mContext, mCertificateChain, mPrivateKey, 
        //        new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
        //}

        //protected override TlsEncryptionCredentials GetRsaEncryptionCredentials()
        //{
        //    return DtlsUtils.LoadEncryptionCredentials(mContext, mCertificateChain, mPrivateKey);
        //}

        //protected override TlsSignerCredentials GetRsaSignerCredentials()
        //{
        //    /*
        //     * TODO Note that this code fails to provide default value for the client supported
        //     * algorithms if it wasn't sent.
        //     */
        //    SignatureAndHashAlgorithm signatureAndHashAlgorithm = null;
        //    IList sigAlgs = mSupportedSignatureAlgorithms;
        //    if (sigAlgs != null)
        //    {
        //        foreach (var sigAlgUncasted in sigAlgs)
        //        {
        //            SignatureAndHashAlgorithm sigAlg = sigAlgUncasted as SignatureAndHashAlgorithm;
        //            if (sigAlg != null && sigAlg.Signature == SignatureAlgorithm.rsa)
        //            {
        //                signatureAndHashAlgorithm = sigAlg;
        //                break;
        //            }
        //        }

        //        if (signatureAndHashAlgorithm == null)
        //        {
        //            return null;
        //        }
        //    }

        //    return DtlsUtils.LoadSignerCredentials(mContext, mCertificateChain, mPrivateKey, 
        //        signatureAndHashAlgorithm);
        //}

        /// <summary>
        /// Prepares the SRTP-DTLS shared secret for the DTLS-SRTP handshake
        /// </summary>
        protected virtual void PrepareSrtpSharedSecret()
        {
            //Set master secret back to security parameters (only works in old bouncy castle versions)
            //mContext.SecurityParameters.masterSecret = masterSecret;

            SrtpParameters srtpParams = SrtpParameters.GetSrtpParametersForProfile(serverSrtpData.
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
             * s(k) = salt length
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
        /// Gets the keying material (master keys and master salts)
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected byte[] GetKeyingMaterial(int length)
        {
            return GetKeyingMaterial(ExporterLabel.dtls_srtp, null, length);
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
        /// Gets the cipher suites supported by the server
        /// </summary>
        /// <returns></returns>
        protected override int[] GetCipherSuites()
        {
            int[] cipherSuites = new int[this.cipherSuites.Length];
            for (int i = 0; i < this.cipherSuites.Length; i++)
            {
                cipherSuites[i] = this.cipherSuites[i];
            }
            return cipherSuites;
        }

        /// <summary>
        /// Gets the client's Certificate
        /// </summary>
        /// <returns></returns>
        public Certificate GetRemoteCertificate()
        {
            return ClientCertificate;
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
                description += message;

            if (cause != null)
                description += cause;

            string alertMsg = $"{AlertLevel.GetText(alertLevel)}, {AlertDescription.GetText(alertDescription)}";
            alertMsg += (!string.IsNullOrEmpty(description)) ? $", {description}." : ".";
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
                level = (AlertLevelsEnum)alertLevel;

            if (Enum.IsDefined(typeof(AlertTypesEnum), alertDescription))
                alertType = (AlertTypesEnum)alertDescription;

            string alertMsg = $"{AlertLevel.GetText(alertLevel)}";
            alertMsg += (!string.IsNullOrEmpty(description)) ? $", {description}." : ".";

            OnAlert?.Invoke(level, alertType, description);
        }

        /// <summary>
        /// This override prevents a TLS fault from being generated if a "Client Hello" is received that
        /// does not support TLS renegotiation (https://tools.ietf.org/html/rfc5746).
        /// This override is required to be able to complete a DTLS handshake with the Pion WebRTC library,
        /// see https://github.com/pion/dtls/issues/274.
        /// </summary>
        public override void NotifySecureRenegotiation(bool secureRenegotiation)
        {
            if (!secureRenegotiation)
            {
                //logger.LogWarning($"DTLS server received a client handshake without renegotiation support.");
            }
        }
    }
}