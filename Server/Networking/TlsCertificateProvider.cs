using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SFSEnhanced.Server.Networking
{
    internal static class TlsCertificateProvider
    {
        public static X509Certificate2 LoadOrCreate(string path, string password, string subject)
        {
            if (File.Exists(path)) return new X509Certificate2(path, password);
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            using var rsa = RSA.Create(3072);
            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(3));
            var bytes = certificate.Export(X509ContentType.Pfx, password);
            File.WriteAllBytes(path, bytes);
            return new X509Certificate2(bytes, password, X509KeyStorageFlags.EphemeralKeySet);
        }

        public static string Fingerprint(X509Certificate2 certificate)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(certificate.RawData));
        }
    }
}
