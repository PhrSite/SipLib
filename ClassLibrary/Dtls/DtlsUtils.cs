﻿//-----------------------------------------------------------------------------
// Filename: DtlsUtils.cs
//
// Description: This class provides useful functions to handle certificate in 
// DTLS-SRTP.
//
// Notes: The webrtc specification provides guidelines for X509 certificate
// management:
// https://www.w3.org/TR/webrtc/#certificate-management
//
// In particular:
// "The explicit certificate management functions provided here are optional. 
// If an application does not provide the certificates configuration option 
// when constructing an RTCPeerConnection a new set of certificates MUST be 
// generated by the user agent. That set MUST include an ECDSA certificate with 
// a private key on the P-256 curve and a signature with a SHA-256 hash."
//
// Based on the above it's likely the safest algorithm to use is ECDSA rather
// than RSA (which will then result in an ECDH rather than DH exchange to
// initialise the SRTP keying material).
// https://www.w3.org/TR/WebCryptoAPI/#algorithms
//
// The recommended ECDSA curves are listed at:
// https://www.w3.org/TR/WebCryptoAPI/#ecdsa
// and are:
// - P-256, also known as secp256r1.
// - P-384, also known as secp384r1.
// - P-521, also known as secp521r1.
//
// TODO: Switch the self-signed certificates generated in this class to use
// ECDSA instead of RSA.
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

//  Revised: 26 Nov 23 PHR
//      -- Changed namespace to SipLib.Dtls from SIPSorcery.Net
//      -- Added documentation comments and code cleanup
//      -- Added CreateCertificateFromPfxFile()

using System.Collections;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.IO.Pem;
using Org.BouncyCastle.X509;

using SipLib.Core;

namespace SipLib.Dtls;

/// <summary>
/// This class provides various utility functions for the Datagram Transport Layer Security (DTLS) protocol.
/// </summary>
public class DtlsUtils
{
    /// <summary>
    /// The key size when generating random keys for self signed certificates.
    /// </summary>
    public const int DEFAULT_KEY_SIZE = 2048;

    /// <summary>
    /// Gets the fingerprint of an .NET X.509 certificate.
    /// </summary>
    /// <param name="hashAlgorithm">Specifies the hash algorithm. For example sha-256.</param>
    /// <param name="certificate">X509Certificate2 certificate to get the fingerprint attribute.</param>
    /// <returns>Returns a RTCDtlsFingerprint object that represents the fingerprint of the X.509 certificate
    /// </returns>
    public static RTCDtlsFingerprint Fingerprint(string hashAlgorithm, X509Certificate2 certificate)
    {
        return Fingerprint(hashAlgorithm, LoadCertificateResource(certificate));
    }

    /// <summary>
    /// Gets the fingerprint of a BouncyCastle X.509 certificate
    /// </summary>
    /// <param name="hashAlgorithm">Specifies the hash algorithm. For example sha-256.</param>
    /// <param name="c">BouncyCastle X.509 certificate object</param>
    /// <returns>Returns a RTCDtlsFingerprint object that represents the fingerprint of the X.509 certificate
    /// </returns>
    // <exception cref="ApplicationException">Thrown if the specified hashAlgorithm is not supported</exception>
    public static RTCDtlsFingerprint Fingerprint(string hashAlgorithm, X509CertificateStructure c)
    {
        if (!IsHashSupported(hashAlgorithm))
        {
            throw new ApplicationException($"Hash algorithm {hashAlgorithm} is not supported for DTLS fingerprints.");
        }

        IDigest digestAlgorithm = DigestUtilities.GetDigest(hashAlgorithm.ToString());
        byte[] der = c.GetEncoded();
        byte[] hash = DigestOf(digestAlgorithm, der);

        return new RTCDtlsFingerprint
        {
            algorithm = digestAlgorithm.AlgorithmName.ToLower(),
            value = hash.HexStr(':')
        };
    }

    /// <summary>
    /// Gets the fingerprint of the first BouncyCastle X.509 certificate in a BouncyCastle certificate chain
    /// </summary>
    /// <param name="certificateChain">The certificate chain the contains at least one certificate</param>
    /// <returns>Returns a RTCDtlsFingerprint object that represents the fingerprint of the X.509 certificate
    /// </returns>
    public static RTCDtlsFingerprint Fingerprint(Certificate certificateChain)
    {
        var certificate = certificateChain.GetCertificateAt(0);
        return Fingerprint(certificate);
    }

    /// <summary>
    /// Gets the fingerprint of a .NET X.509 certificate
    /// </summary>
    /// <param name="certificate">Input .NET X.509 certificate</param>
    /// <returns>Returns a RTCDtlsFingerprint object that represents the fingerprint of the X.509 certificate
    /// </returns>
    public static RTCDtlsFingerprint Fingerprint(X509Certificate2 certificate)
    {
        return Fingerprint(LoadCertificateResource(certificate));
    }

    /// <summary>
    /// Gets the fingerprint of a BouncyCastle X.509 certificate
    /// </summary>
    /// <param name="certificate">Input BouncyCastle X.509 certificate</param>
    /// <returns>Returns a RTCDtlsFingerprint object that represents the fingerprint of the X.509 certificate
    /// </returns>
    public static RTCDtlsFingerprint Fingerprint(Org.BouncyCastle.X509.X509Certificate certificate)
    {
        var certStruct = X509CertificateStructure.GetInstance(certificate.GetEncoded());
        return Fingerprint(certStruct);
    }

    /// <summary>
    /// Gets the fingerprint of a BouncyCastle X509CertificateStructure using the SHA-256 hash algorithm
    /// </summary>
    /// <param name="c">Input certificate structure</param>
    /// <returns>Returns a RTCDtlsFingerprint object that represents the fingerprint of the X.509 certificate
    /// </returns>
    public static RTCDtlsFingerprint Fingerprint(X509CertificateStructure c)
    {
        IDigest sha256 = DigestUtilities.GetDigest(HashAlgorithmTag.Sha256.ToString());
        byte[] der = c.GetEncoded();
        byte[] sha256Hash = DigestOf(sha256, der);

        return new RTCDtlsFingerprint
        {
            algorithm = sha256.AlgorithmName.ToLower(),
            value = sha256Hash.HexStr(':')
        };
    }

    /// <summary>
    /// Calculates the digest of a byte array
    /// </summary>
    /// <param name="dAlg">Specifies the hash algorithm to use</param>
    /// <param name="input">Input byte array to calculate the hash of</param>
    /// <returns>Returns a byte array containing the calculated hash value</returns>
    public static byte[] DigestOf(IDigest dAlg, byte[] input)
    {
        dAlg.BlockUpdate(input, 0, input.Length);
        byte[] result = new byte[dAlg.GetDigestSize()];
        dAlg.DoFinal(result, 0);
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="certificate"></param>
    /// <param name="privateKey"></param>
    /// <returns></returns>
    public static TlsAgreementCredentials LoadAgreementCredentials(TlsContext context,
            Certificate certificate, AsymmetricKeyParameter privateKey)
    {
        return new DefaultTlsAgreementCredentials(certificate, privateKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="certResources"></param>
    /// <param name="keyResource"></param>
    /// <returns></returns>
    public static TlsAgreementCredentials LoadAgreementCredentials(TlsContext context,
            string[] certResources, string keyResource)
    {
        Certificate certificate = LoadCertificateChain(certResources);
        AsymmetricKeyParameter privateKey = LoadPrivateKeyResource(keyResource);
        return LoadAgreementCredentials(context, certificate, privateKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="certificate"></param>
    /// <param name="privateKey"></param>
    /// <returns></returns>
    public static TlsEncryptionCredentials LoadEncryptionCredentials(
            TlsContext context, Certificate certificate, AsymmetricKeyParameter privateKey)
    {
        return new DefaultTlsEncryptionCredentials(context, certificate,
                privateKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="certResources"></param>
    /// <param name="keyResource"></param>
    /// <returns></returns>
    public static TlsEncryptionCredentials LoadEncryptionCredentials(
            TlsContext context, string[] certResources, string keyResource)
    {
        Certificate certificate = LoadCertificateChain(certResources);
        AsymmetricKeyParameter privateKey = LoadPrivateKeyResource(keyResource);
        return LoadEncryptionCredentials(context, certificate,
                privateKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="certificate"></param>
    /// <param name="privateKey"></param>
    /// <returns></returns>
    public static TlsSignerCredentials LoadSignerCredentials(TlsContext context,
            Certificate certificate, AsymmetricKeyParameter privateKey)
    {
        return new DefaultTlsSignerCredentials(context, certificate, privateKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="certResources"></param>
    /// <param name="keyResource"></param>
    /// <returns></returns>
    public static TlsSignerCredentials LoadSignerCredentials(TlsContext context,
            string[] certResources, string keyResource)
    {
        Certificate certificate = LoadCertificateChain(certResources);
        AsymmetricKeyParameter privateKey = LoadPrivateKeyResource(keyResource);
        return LoadSignerCredentials(context, certificate, privateKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="certificate"></param>
    /// <param name="privateKey"></param>
    /// <param name="signatureAndHashAlgorithm"></param>
    /// <returns></returns>
    public static TlsSignerCredentials LoadSignerCredentials(TlsContext context,
            Certificate certificate, AsymmetricKeyParameter privateKey,
            SignatureAndHashAlgorithm signatureAndHashAlgorithm)
    {
        return new DefaultTlsSignerCredentials(context, certificate,
                privateKey, signatureAndHashAlgorithm);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="certResources"></param>
    /// <param name="keyResource"></param>
    /// <param name="signatureAndHashAlgorithm"></param>
    /// <returns></returns>
    public static TlsSignerCredentials LoadSignerCredentials(TlsContext context,
            string[] certResources, string keyResource,
            SignatureAndHashAlgorithm signatureAndHashAlgorithm)
    {
        Certificate certificate = LoadCertificateChain(certResources);
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter privateKey = LoadPrivateKeyResource(keyResource);
        return LoadSignerCredentials(context, certificate,
                privateKey, signatureAndHashAlgorithm);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="supportedSignatureAlgorithms"></param>
    /// <param name="signatureAlgorithm"></param>
    /// <param name="certificate"></param>
    /// <param name="privateKey"></param>
    /// <returns></returns>
    public static TlsSignerCredentials LoadSignerCredentials(TlsContext context, IList supportedSignatureAlgorithms,
        byte signatureAlgorithm, Certificate certificate, AsymmetricKeyParameter privateKey)
    {
        /*
         * TODO Note that this code fails to provide default value for the client supported
         * algorithms if it wasn't sent.
         */

        SignatureAndHashAlgorithm signatureAndHashAlgorithm = null;
        if (supportedSignatureAlgorithms != null)
        {
            foreach (SignatureAndHashAlgorithm alg in supportedSignatureAlgorithms)
            {
                if (alg.Signature == signatureAlgorithm)
                {
                    signatureAndHashAlgorithm = alg;
                    break;
                }
            }

            if (signatureAndHashAlgorithm == null)
            {
                return null;
            }
        }

        return LoadSignerCredentials(context, certificate, privateKey, signatureAndHashAlgorithm);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="supportedSignatureAlgorithms"></param>
    /// <param name="signatureAlgorithm"></param>
    /// <param name="certResource"></param>
    /// <param name="keyResource"></param>
    /// <returns></returns>
    public static TlsSignerCredentials LoadSignerCredentials(TlsContext context, IList supportedSignatureAlgorithms,
        byte signatureAlgorithm, string certResource, string keyResource)
    {
        Certificate certificate = LoadCertificateChain(new string[] { certResource, "x509-ca.pem" });
        AsymmetricKeyParameter privateKey = LoadPrivateKeyResource(keyResource);

        return LoadSignerCredentials(context, supportedSignatureAlgorithms, signatureAlgorithm, certificate,
            privateKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="certificates"></param>
    /// <returns></returns>
    public static Certificate LoadCertificateChain(X509Certificate2[] certificates)
    {
        var chain = new Org.BouncyCastle.Asn1.X509.X509CertificateStructure[certificates.Length];
        for (int i = 0; i < certificates.Length; i++)
        {
            chain[i] = LoadCertificateResource(certificates[i]);
        }

        return new Certificate(chain);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="certificate"></param>
    /// <returns></returns>
    public static Certificate LoadCertificateChain(X509Certificate2 certificate)
    {
        return LoadCertificateChain(new X509Certificate2[] { certificate });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resources"></param>
    /// <returns></returns>
    public static Certificate LoadCertificateChain(string[] resources)
    {
        Org.BouncyCastle.Asn1.X509.X509CertificateStructure[]
        chain = new Org.BouncyCastle.Asn1.X509.X509CertificateStructure[resources.Length];
        for (int i = 0; i < resources.Length; ++i)
        {
            chain[i] = LoadCertificateResource(resources[i]);
        }
        return new Certificate(chain);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="certificate"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static X509CertificateStructure LoadCertificateResource(X509Certificate2 certificate)
    {
        if (certificate != null)
        {
            var bouncyCertificate = DotNetUtilities.FromX509Certificate(certificate);
            return X509CertificateStructure.GetInstance(bouncyCertificate.GetEncoded());
        }
        throw new Exception("'resource' doesn't specify a valid certificate");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static X509CertificateStructure LoadCertificateResource(string resource)
    {
        PemObject pem = LoadPemResource(resource);
        if (pem.Type.EndsWith("CERTIFICATE"))
        {
            return X509CertificateStructure.GetInstance(pem.Content);
        }
        throw new Exception("'resource' doesn't specify a valid certificate");
    }

    /// <summary>
    /// Gets the private key of a .NET X509Certificate2 object
    /// </summary>
    /// <param name="certificate">Input certificate that contains a private key</param>
    /// <returns>Returns the private key contained in the certificate</returns>
    public static AsymmetricKeyParameter LoadPrivateKeyResource(X509Certificate2 certificate)
    {
        // TODO: When .NET Standard and Framework support are deprecated this pragma can be removed.
#pragma warning disable SYSLIB0028
        return DotNetUtilities.GetKeyPair(certificate.PrivateKey).Private;
#pragma warning restore SYSLIB0028
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static AsymmetricKeyParameter LoadPrivateKeyResource(string resource)
    {
        PemObject pem = LoadPemResource(resource);
        if (pem.Type.EndsWith("RSA PRIVATE KEY"))
        {
            RsaPrivateKeyStructure rsa = RsaPrivateKeyStructure.GetInstance(pem.Content);
            return new RsaPrivateCrtKeyParameters(rsa.Modulus,
                    rsa.PublicExponent, rsa.PrivateExponent,
                    rsa.Prime1, rsa.Prime2, rsa.Exponent1,
                    rsa.Exponent2, rsa.Coefficient);
        }
        if (pem.Type.EndsWith("PRIVATE KEY"))
        {
            return PrivateKeyFactory.CreateKey(pem.Content);
        }
        throw new Exception("'resource' doesn't specify a valid private key");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static PemObject LoadPemResource(string path)
    {
        using (var s = new System.IO.StreamReader(path))
        {
            PemReader p = new PemReader(s);
            PemObject o = p.ReadPemObject();
            return o;
        }
        throw new Exception("'resource' doesn't specify a valid private key");
    }

    #region Self Signed Utils

    /// <summary>
    /// Creates a BouncyCastle Certificate object and its private key from a PFX file containing a .NET
    /// X509Certificate2 object
    /// </summary>
    /// <param name="CertFileName">File name of the *.PFX file containing a private key.</param>
    /// <param name="CertPassword">Password of the X.509 certificate</param>
    /// <returns>Returns a BouncyCastle Certificate object and its private key.</returns>
    public static (Certificate, AsymmetricKeyParameter) CreateCertificateFromPfxFile(string CertFileName,
        string CertPassword)
    {
        FileStream Fs = File.OpenRead(CertFileName);
        Pkcs12Store Pkcs = new Pkcs12Store(Fs, CertPassword.ToCharArray());
        IEnumerator IEnum = Pkcs.Aliases.GetEnumerator();
        IEnum.MoveNext();
        string alias = IEnum.Current.ToString();
        X509CertificateEntry Ce = Pkcs.GetCertificate(alias);
        Org.BouncyCastle.X509.X509Certificate cert = Ce.Certificate;
        X509CertificateStructure[] certStruct = new X509CertificateStructure[] { cert.CertificateStructure };
        Certificate SsCert = new Certificate(certStruct);
        AsymmetricKeyParameter asymmetricKeyParameter = Pkcs.GetKey(alias).Key;
        return (SsCert, asymmetricKeyParameter);
    }

    /// <summary>
    /// Creates an X.509 self-signed certificate for use with DTLS using RSA and SHA-256.
    /// </summary>
    /// <param name="privateKey">Private key to use. May be null</param>
    /// <returns>Returns a new self-signed certificate</returns>
    public static X509Certificate2 CreateSelfSignedCert(AsymmetricKeyParameter privateKey = null)
    {
        return CreateSelfSignedCert("CN=localhost", "CN=root", privateKey);
    }

    /// <summary>
    /// Creates an X.509 self-signed certificate for use with DTLS using RSA and SHA-256.
    /// </summary>
    /// <param name="subjectName">Subject name for the certificate. For example: "CN=localhost"</param>
    /// <param name="issuerName">Issuer name for the certificate. For example: "CN=root"</param>
    /// <param name="privateKey">Private key to use. May be null.</param>
    /// <returns>Returns a new self-signed certificate</returns>

    public static X509Certificate2 CreateSelfSignedCert(string subjectName, string issuerName, 
        AsymmetricKeyParameter privateKey)
    {
        const int keyStrength = DEFAULT_KEY_SIZE;
        if (privateKey == null)
        {
            privateKey = CreatePrivateKeyResource(issuerName);
        }
        var issuerPrivKey = privateKey;

        // Generating Random Numbers
        var randomGenerator = new CryptoApiRandomGenerator();
        var random = new SecureRandom(randomGenerator);
        ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA256WITHRSA", issuerPrivKey, random);

        // The Certificate Generator
        var certificateGenerator = new X509V3CertificateGenerator();
        certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, new GeneralNames(
            new GeneralName[] { new GeneralName(GeneralName.DnsName, "localhost"), new GeneralName(
                GeneralName.DnsName, "127.0.0.1") }));
        certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, new ExtendedKeyUsage(
            new List<DerObjectIdentifier>() { new DerObjectIdentifier("1.3.6.1.5.5.7.3.1") }));

        // Serial Number
        var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), 
            random);
        certificateGenerator.SetSerialNumber(serialNumber);

        // Issuer and Subject Name
        var subjectDn = new X509Name(subjectName);
        var issuerDn = new X509Name(issuerName);
        certificateGenerator.SetIssuerDN(issuerDn);
        certificateGenerator.SetSubjectDN(subjectDn);

        // Valid For
        var notBefore = DateTime.UtcNow.Date;
        var notAfter = notBefore.AddYears(70);

        certificateGenerator.SetNotBefore(notBefore);
        certificateGenerator.SetNotAfter(notAfter);

        // Subject Public Key
        var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
        var keyPairGenerator = new RsaKeyPairGenerator();
        keyPairGenerator.Init(keyGenerationParameters);
        var subjectKeyPair = keyPairGenerator.GenerateKeyPair();

        certificateGenerator.SetPublicKey(subjectKeyPair.Public);

        // self sign certificate
        var certificate = certificateGenerator.Generate(signatureFactory);

        // Originally pre-processor defines were used to try and pick the supported way to get from a Bouncy Castle
        // certificate and private key to a .NET certificate. The problem is that setting the private key on a .NET
        // X509 certificate is possible in .NET Framework but NOT in .NET Core. To complicate matters even further
        // the workaround in the CovertBouncyCert method of saving a cert + pvt key to a .pfx stream and then
        // reloading does not work on macOS or Unity (and possibly elsewhere) due to .pfx serialisation not being
        // compatible. This is the exception from Unity:
        //
        // Mono.Security.ASN1..ctor (System.Byte[] data) (at <6a66fe237d4242c9924192d3c28dd540>:0)
        // Mono.Security.X509.X509Certificate.Parse(System.Byte[] data)(at < 6a66fe237d4242c9924192d3c28dd540 >:0)
        //
        // Summary:
        // .NET Framework (including Mono on Linux, macOS and WSL)
        //  - Set x509.PrivateKey works.
        // .NET Standard:
        //  - Set x509.PrivateKey for a .NET Framework application.
        //  - Set x509.PrivateKey for a .NET Core application FAILS.
        // .NET Core:
        //  - Set x509.PrivateKey for a .NET Core application FAILS.
        //  - PFX serialisation works on Windows.
        //  - PFX serialisation works on WSL and Linux.
        //  - PFX serialisation FAILS on macOS.
        //
        // For same issue see https://github.com/dotnet/runtime/issues/23635.
        // For fix in net5 see https://github.com/dotnet/corefx/pull/42226.
        try
        {
            // corresponding private key
            var info = Org.BouncyCastle.Pkcs.PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);

            // merge into X509Certificate2
            var x509 = new X509Certificate2(certificate.GetEncoded());

            var seq = (Asn1Sequence)Asn1Object.FromByteArray(info.ParsePrivateKey().GetDerEncoded());
            if (seq.Count != 9)
            {
                throw new Org.BouncyCastle.OpenSsl.PemException("malformed sequence in RSA private key");
            }

            var rsa = RsaPrivateKeyStructure.GetInstance(seq); //new RsaPrivateKeyStructure(seq);
            var rsaparams = new RsaPrivateCrtKeyParameters(
                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);

            // TODO: When .NET Standard and Framework support are deprecated this pragma can be removed.
#pragma warning disable SYSLIB0028
            x509.PrivateKey = ToRSA(rsaparams);
#pragma warning restore SYSLIB0028
            return x509;
        }
        catch
        {
            return ConvertBouncyCert(certificate, subjectKeyPair);
        }
    }

    /// <summary>
    /// Creates a self-signed BouncyCastle X509Certificate and its private key.
    /// </summary>
    /// <returns>Returns a self-signed BouncyCastle X509Certificate and its private key</returns>
    public static (Org.BouncyCastle.X509.X509Certificate certificate, AsymmetricKeyParameter privateKey) 
        CreateSelfSignedBouncyCastleCert()
    {
        return CreateSelfSignedBouncyCastleCert("CN=localhost", "CN=root", null);
    }

    private static (Org.BouncyCastle.X509.X509Certificate certificate, AsymmetricKeyParameter privateKey) 
        CreateSelfSignedBouncyCastleCert(string subjectName, string issuerName, AsymmetricKeyParameter issuerPrivateKey)
    {
        const int keyStrength = DEFAULT_KEY_SIZE;
        if (issuerPrivateKey == null)
        {
            issuerPrivateKey = CreatePrivateKeyResource(issuerName);
        }

        // Generating Random Numbers
        var randomGenerator = new CryptoApiRandomGenerator();
        var random = new SecureRandom(randomGenerator);
        ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA256WITHRSA", issuerPrivateKey, random);

        // The Certificate Generator
        var certificateGenerator = new X509V3CertificateGenerator();
        certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, new GeneralNames(
            new GeneralName[] { new GeneralName(GeneralName.DnsName, "localhost"), new GeneralName(
                GeneralName.DnsName, "127.0.0.1") }));
        certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, new ExtendedKeyUsage(
            new List<DerObjectIdentifier>() { new DerObjectIdentifier("1.3.6.1.5.5.7.3.1") }));

        // Serial Number
        var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue),
            random);
        certificateGenerator.SetSerialNumber(serialNumber);

        // Issuer and Subject Name
        var subjectDn = new X509Name(subjectName);
        var issuerDn = new X509Name(issuerName);
        certificateGenerator.SetIssuerDN(issuerDn);
        certificateGenerator.SetSubjectDN(subjectDn);

        // Valid For
        var notBefore = DateTime.UtcNow.Date;
        var notAfter = notBefore.AddYears(70);

        certificateGenerator.SetNotBefore(notBefore);
        certificateGenerator.SetNotAfter(notAfter);

        // Subject Public Key
        var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
        var keyPairGenerator = new RsaKeyPairGenerator();
        keyPairGenerator.Init(keyGenerationParameters);
        var subjectKeyPair = keyPairGenerator.GenerateKeyPair();

        certificateGenerator.SetPublicKey(subjectKeyPair.Public);

        // self sign certificate
        var certificate = certificateGenerator.Generate(signatureFactory);

        return (certificate, subjectKeyPair.Private);
    }

    /// <summary>
    /// Creates a self-signed BouncyCastle TLS certificate and its private key.
    /// </summary>
    /// <returns>Returns a self-signed (Org.BouncyCastle.Crypto.Tls.Certificate certificate and
    /// its private key.</returns>
    public static (Org.BouncyCastle.Crypto.Tls.Certificate certificate, AsymmetricKeyParameter privateKey) 
        CreateSelfSignedTlsCert()
    {
        return CreateSelfSignedTlsCert("CN=localhost", "CN=root", null);
    }

    /// <summary>
    /// Creates a self-signed BouncyCastle TLS certificate and its private key.
    /// </summary>
    /// <param name="subjectName">Subject Name for the certificate. For example: "CN=localhost"</param>
    /// <param name="issuerName">Issuer Name for the certificate. For example: "CN=root"</param>
    /// <param name="issuerPrivateKey"></param>
    /// <returns></returns>
    public static (Org.BouncyCastle.Crypto.Tls.Certificate certificate, AsymmetricKeyParameter privateKey)
        CreateSelfSignedTlsCert(string subjectName, string issuerName, AsymmetricKeyParameter issuerPrivateKey)
    {
        var tuple = CreateSelfSignedBouncyCastleCert(subjectName, issuerName, issuerPrivateKey);
        var certificate = tuple.certificate;
        var privateKey = tuple.privateKey;
         var chain = new Org.BouncyCastle.Asn1.X509.X509CertificateStructure[] { X509CertificateStructure.
             GetInstance(certificate.GetEncoded()) };
        var tlsCertificate = new Org.BouncyCastle.Crypto.Tls.Certificate(chain);

        return (tlsCertificate, privateKey);
    }

    /// <remarks>Plagiarised from https://github.com/CryptLink/CertBuilder/blob/master/CertBuilder.cs.
    /// NOTE: netstandard2.1+ and netcoreapp3.1+ have x509.CopyWithPrivateKey which will avoid the need to
    /// use the serialize/deserialize from pfx to get from bouncy castle to .NET Core X509 certificates.</remarks>
    public static X509Certificate2 ConvertBouncyCert(Org.BouncyCastle.X509.X509Certificate bouncyCert, 
        AsymmetricCipherKeyPair keyPair)
    {
        var pkcs12Store = new Pkcs12Store();
        var certEntry = new X509CertificateEntry(bouncyCert);

        pkcs12Store.SetCertificateEntry(bouncyCert.SerialNumber.ToString(), certEntry);
        pkcs12Store.SetKeyEntry(bouncyCert.SerialNumber.ToString(),
            new AsymmetricKeyEntry(keyPair.Private), new[] { certEntry });

        X509Certificate2 keyedCert;

        using (MemoryStream pfxStream = new MemoryStream())
        {
            pkcs12Store.Save(pfxStream, new char[] { }, new SecureRandom());
            pfxStream.Seek(0, SeekOrigin.Begin);
            keyedCert = new X509Certificate2(pfxStream.ToArray(), string.Empty, X509KeyStorageFlags.Exportable);
        }

        return keyedCert;
    }

    /// <summary>
    /// Creates a private key
    /// </summary>
    /// <param name="subjectName">Subject Name for the private key. Defaults to "CN=root"</param>
    /// <returns>Returns a new private key.</returns>
    public static AsymmetricKeyParameter CreatePrivateKeyResource(string subjectName = "CN=root")
    {
        const int keyStrength = DEFAULT_KEY_SIZE;

        // Generating Random Numbers
        var randomGenerator = new CryptoApiRandomGenerator();
        var random = new SecureRandom(randomGenerator);

        // Subject Public Key
        var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
        var keyPairGenerator = new RsaKeyPairGenerator();
        keyPairGenerator.Init(keyGenerationParameters);
        var subjectKeyPair = keyPairGenerator.GenerateKeyPair();

        return subjectKeyPair.Private;
    }

    #endregion

    /// <summary>
    /// This method and the related ones have been copied from the BouncyCode DotNetUtilities 
    /// class due to https://github.com/bcgit/bc-csharp/issues/160 which prevents the original
    /// version from working on non-Windows platforms.
    /// </summary>
    public static RSA ToRSA(RsaPrivateCrtKeyParameters privKey)
    {
        return CreateRSAProvider(ToRSAParameters(privKey));
    }

    private static RSA CreateRSAProvider(RSAParameters rp)
    {
        //CspParameters csp = new CspParameters();
        //csp.KeyContainerName = string.Format("BouncyCastle-{0}", Guid.NewGuid());
        //RSACryptoServiceProvider rsaCsp = new RSACryptoServiceProvider(csp);
        RSACryptoServiceProvider rsaCsp = new RSACryptoServiceProvider();
        rsaCsp.ImportParameters(rp);
        return rsaCsp;
    }

    /// <summary>
    /// Converts a BouncyCastle RsaPrivateCrtKeyParameters to a .NET RSAParameters object.
    /// </summary>
    /// <param name="privKey">Input RSA private key parameters</param>
    /// <returns>Returns a new .NET RSAParameters object.</returns>
    public static RSAParameters ToRSAParameters(RsaPrivateCrtKeyParameters privKey)
    {
        RSAParameters rp = new RSAParameters();
        rp.Modulus = privKey.Modulus.ToByteArrayUnsigned();
        rp.Exponent = privKey.PublicExponent.ToByteArrayUnsigned();
        rp.P = privKey.P.ToByteArrayUnsigned();
        rp.Q = privKey.Q.ToByteArrayUnsigned();
        rp.D = ConvertRSAParametersField(privKey.Exponent, rp.Modulus.Length);
        rp.DP = ConvertRSAParametersField(privKey.DP, rp.P.Length);
        rp.DQ = ConvertRSAParametersField(privKey.DQ, rp.Q.Length);
        rp.InverseQ = ConvertRSAParametersField(privKey.QInv, rp.Q.Length);
        return rp;
    }

    private static byte[] ConvertRSAParametersField(BigInteger n, int size)
    {
        byte[] bs = n.ToByteArrayUnsigned();

        if (bs.Length == size)
        {
            return bs;
        }

        if (bs.Length > size)
        {
            throw new ArgumentException("Specified size too small", "size");
        }

        byte[] padded = new byte[size];
        Array.Copy(bs, 0, padded, size - bs.Length, bs.Length);
        return padded;
    }

    /// <summary>
    /// Verifies the hash algorithm is supported by the utility functions in this class.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm to check.</param>
    public static bool IsHashSupported(string hashAlgorithm)
    {
        switch (hashAlgorithm.ToLower())
        {
            case "sha1":
            case "sha-1":
            case "sha256":
            case "sha-256":
            case "sha384":
            case "sha-384":
            case "sha512":
            case "sha-512":
                return true;
            default:
                return false;
        }
    }
}
