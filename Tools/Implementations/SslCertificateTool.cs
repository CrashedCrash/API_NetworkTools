// API_NetworkTools/Tools/Implementations/SslCertificateTool.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading; // Für CancellationTokenSource
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;

namespace API_NetworkTools.Tools.Implementations
{
    // --- DTOs für Zertifikatsdetails ---
    public record CertificatePrincipalInfo
    {
        public string? Raw { get; init; }
        public Dictionary<string, string>? Attributes { get; init; }
    }

    public record CertificateValidityInfo
    {
        public DateTime NotBeforeUtc { get; init; }
        public DateTime NotAfterUtc { get; init; }
        public string? Status { get; init; } 
        public double DaysRemaining { get; init; }
    }

    public record BasicCertificateData
    {
        public CertificatePrincipalInfo? Subject { get; init; }
        public CertificatePrincipalInfo? Issuer { get; init; }
        public CertificateValidityInfo? Validity { get; init; }
        public string? Version { get; init; }
        public string? SerialNumber { get; init; }
        public string? ThumbprintSha1 { get; init; }
        public string? ThumbprintSha256 { get; init; }
        public string? SignatureAlgorithm { get; init; }
        public string? PublicKeyAlgorithm { get; init; }
        public int? PublicKeyLength { get; init; }
    }

    public record CertificateExtensionInfo
    {
        public string? Oid { get; init; }
        public string? FriendlyName { get; init; }
        public string? Value { get; init; }
        public bool Critical { get; init; }
    }

    public record CertificateChainInfo
    {
        public BasicCertificateData? Certificate { get; init; }
        public List<string>? Errors { get; init; }
        public bool IsSelfSigned { get; init; }
    }
    
    public record FullCertificateDetails
    {
        public BasicCertificateData? MainCertificate { get; init; }
        public List<string>? SubjectAlternativeNamesFormatted { get; init; }
        public List<CertificateExtensionInfo>? Extensions { get; init; }
        public List<CertificateChainInfo>? Chain { get; init; }
        public string? RawCertificateBase64 { get; init; }
    }

    public class SslCertificateTool : INetworkTool
    {
        public string Identifier => "ssl-cert-info";
        public string DisplayName => "SSL Certificate Information";
        public string Description => "Ruft Details des SSL/TLS-Zertifikats von einem Host ab.";

        public List<ToolParameterInfo> GetParameters()
        {
            return new List<ToolParameterInfo>
            {
                new ToolParameterInfo(
                    name: "port",
                    label: "Port (Standard: 443)",
                    type: "number",
                    isRequired: false,
                    defaultValue: "443"
                )
            };
        }

        public async Task<ToolOutput> ExecuteAsync(string targetHost, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(targetHost))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Hostname darf nicht leer sein." };
            }

            if (Uri.CheckHostName(targetHost) == UriHostNameType.Unknown && !targetHost.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiger Hostname: {targetHost}" };
            }
            
            options.TryGetValue("port", out string? portString);
            if (!int.TryParse(portString, out int port) || port <= 0 || port > 65535)
            {
                port = 443; 
            }

            X509Certificate2? serverCertificate = null;
            X509Chain? capturedChain = null;

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTimeout = TimeSpan.FromSeconds(10);
                    using (var ctsConnect = new CancellationTokenSource(connectTimeout))
                    {
                        try
                        {
                            await client.ConnectAsync(targetHost, port, ctsConnect.Token);
                        }
                        catch (OperationCanceledException) when (ctsConnect.IsCancellationRequested)
                        {
                            return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Timeout ({connectTimeout.TotalSeconds}s) beim Verbinden mit {targetHost}:{port}." };
                        }
                        // ConnectAsync wirft SocketException bei anderen Fehlern
                    }

                    if (!client.Connected) // Sollte durch obiges abgedeckt sein, aber als extra Check
                    {
                         return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Verbindung zu {targetHost}:{port} konnte nicht hergestellt werden." };
                    }

                    using (var sslStream = new SslStream(client.GetStream(), false,
                        (sender, certificate, chain, sslPolicyErrors) =>
                        {
                            if (certificate != null)
                            {
                                serverCertificate = new X509Certificate2(certificate); // Kopie erstellen
                            }
                            if (chain != null)
                            {
                                // Erstelle eine Kopie der Kette oder baue sie neu auf, um Thread-Probleme zu vermeiden
                                // und sicherzustellen, dass sie außerhalb des Callbacks gültig ist.
                                capturedChain = new X509Chain();
                                capturedChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                                capturedChain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreInvalidBasicConstraints | 
                                                                          X509VerificationFlags.AllowUnknownCertificateAuthority | 
                                                                          X509VerificationFlags.IgnoreNotTimeValid; // Grundlegende Flags für Inspektion
                                if (serverCertificate != null) // Verwende das bereits kopierte Zertifikat
                                {
                                   capturedChain.Build(serverCertificate);
                                }
                            }
                            return true; 
                        },
                        null))
                    {
                        var authTimeout = TimeSpan.FromSeconds(15);
                        using (var ctsAuth = new CancellationTokenSource(authTimeout))
                        {
                            try
                            {
                                var sslClientAuthOptions = new SslClientAuthenticationOptions
                                {
                                    TargetHost = targetHost,
                                    EnabledSslProtocols = SslProtocols.None, // Lässt das OS wählen
                                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true // Erneut, da der Callback im Konstruktor manchmal nicht ausreicht
                                };
                                await sslStream.AuthenticateAsClientAsync(sslClientAuthOptions, ctsAuth.Token);
                            }
                            catch (OperationCanceledException) when (ctsAuth.IsCancellationRequested)
                            {
                                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Timeout ({authTimeout.TotalSeconds}s) während des SSL-Handshakes mit {targetHost}:{port}." };
                            }
                            // Andere Exceptions (AuthenticationException, IOException) werden unten gefangen
                        }
                        // Das Zertifikat wurde im RemoteCertificateValidationCallback oben gesetzt
                        if (sslStream.RemoteCertificate != null && serverCertificate == null) // Fallback, falls Callback nicht wie erwartet serverCertificate gesetzt hat
                        {
                            serverCertificate = new X509Certificate2(sslStream.RemoteCertificate);
                            if (capturedChain == null && serverCertificate != null) { // Versuche Kette hier zu bauen, falls im Callback nicht geschehen
                                capturedChain = new X509Chain();
                                capturedChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                                capturedChain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreInvalidBasicConstraints | X509VerificationFlags.AllowUnknownCertificateAuthority | X509VerificationFlags.IgnoreNotTimeValid;
                                capturedChain.Build(serverCertificate);
                            }
                        }
                    }
                }

                if (serverCertificate == null)
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Konnte kein SSL-Zertifikat von {targetHost}:{port} abrufen (nach Handshake)." };
                }

                var details = ParseCertificate(serverCertificate, capturedChain);
                return new ToolOutput { Success = true, ToolName = DisplayName, Data = details };
            }
            catch (SocketException sockEx)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Socket-Fehler (Code: {sockEx.SocketErrorCode}) beim Verbinden mit {targetHost}:{port}: {sockEx.Message}" };
            }
            catch (AuthenticationException authEx)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"SSL/TLS Authentifizierungsfehler für {targetHost}:{port}: {authEx.Message}", Data = authEx.InnerException?.Message };
            }
            catch (System.IO.IOException ioEx)
            {
                 return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"E/A-Fehler während der SSL-Kommunikation mit {targetHost}:{port}: {ioEx.Message}" };
            }
            catch (TaskCanceledException tex) // Fängt generelle TaskCancellations (auch die von den CTS)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Die Operation für {targetHost}:{port} wurde abgebrochen (Timeout oder anderer Grund): {tex.Message}" };
            }
            catch (Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ({ex.GetType().Name}) ist aufgetreten: {ex.Message}" };
            }
        }

        private CertificatePrincipalInfo ParseDn(string distinguishedName)
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = distinguishedName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kv = part.Trim().Split(new[] { '=' }, 2);
                if (kv.Length == 2 && !attributes.ContainsKey(kv[0].Trim()))
                {
                    attributes.Add(kv[0].Trim(), kv[1].Trim());
                }
            }
            return new CertificatePrincipalInfo { Raw = distinguishedName, Attributes = attributes };
        }
        
        private FullCertificateDetails ParseCertificate(X509Certificate2 cert, X509Chain? chain)
        {
            int? pkLength = null;
            try { pkLength = cert.PublicKey.GetRSAPublicKey()?.KeySize ?? cert.PublicKey.GetECDsaPublicKey()?.KeySize; } catch { /* Ignorieren, falls Schlüsseltyp nicht unterstützt */ }

            var basicData = new BasicCertificateData
            {
                Subject = ParseDn(cert.SubjectName.Name ?? string.Empty),
                Issuer = ParseDn(cert.IssuerName.Name ?? string.Empty),
                Version = cert.Version.ToString(),
                SerialNumber = cert.SerialNumber,
                ThumbprintSha1 = cert.GetCertHashString(HashAlgorithmName.SHA1),
                ThumbprintSha256 = cert.GetCertHashString(HashAlgorithmName.SHA256),
                SignatureAlgorithm = cert.SignatureAlgorithm.FriendlyName,
                PublicKeyAlgorithm = cert.PublicKey.Oid.FriendlyName,
                PublicKeyLength = pkLength, // Korrigiert für SYSLIB0027
                Validity = new CertificateValidityInfo
                {
                    NotBeforeUtc = cert.NotBefore.ToUniversalTime(),
                    NotAfterUtc = cert.NotAfter.ToUniversalTime(),
                    DaysRemaining = Math.Round((cert.NotAfter.ToUniversalTime() - DateTime.UtcNow).TotalDays, 2),
                    Status = DateTime.UtcNow < cert.NotBefore.ToUniversalTime() ? "Not Yet Valid" : (DateTime.UtcNow > cert.NotAfter.ToUniversalTime() ? "Expired" : "Valid")
                }
            };

            var sansFormatted = new List<string>();
            var extensions = new List<CertificateExtensionInfo>();

            foreach (var ext in cert.Extensions)
            {
                string? value = null;
                if (ext is X509SubjectAlternativeNameExtension sanExt) // Korrekte Typüberprüfung für SAN
                {
                    // sanExt.Format(false) gibt oft eine für Menschen lesbare, aber schwer zu parsende Zeichenkette zurück.
                    // Für eine echte Liste von DNS-Namen/IPs bräuchte man tiefere ASN.1-Dekodierung.
                    // Hier nehmen wir die formatierte Ausgabe.
                    value = sanExt.Format(false); 
                    if(!string.IsNullOrWhiteSpace(value)) sansFormatted.Add(value);
                } else {
                    try { value = ext.Format(true); } // Format(true) für multiline
                    catch { 
                        // Fallback für nicht standardmäßig formatierbare Erweiterungen
                        value = BitConverter.ToString(ext.RawData).Replace("-", ""); 
                    }
                }
                
                extensions.Add(new CertificateExtensionInfo
                {
                    Oid = ext.Oid?.Value,
                    FriendlyName = ext.Oid?.FriendlyName ?? "Unknown",
                    Critical = ext.Critical,
                    Value = value
                });
            }

            var chainDetails = new List<CertificateChainInfo>();
            if (chain != null && chain.ChainElements.Count > 0)
            {
                foreach (var chainElement in chain.ChainElements)
                {
                    var errors = chainElement.ChainElementStatus
                        .Select(s => s.StatusInformation.Trim())
                        .Where(s => !string.IsNullOrEmpty(s) && 
                                    !s.Equals("ok", StringComparison.OrdinalIgnoreCase) && 
                                    !s.Equals("no error", StringComparison.OrdinalIgnoreCase) &&
                                    !s.Equals("The certificate is not time valid.", StringComparison.OrdinalIgnoreCase) && // Ignoriere TimeValid-Fehler, da wir es im Callback erlaubt haben
                                    !s.Equals("A certificate chain could not be built to a trusted root authority.", StringComparison.OrdinalIgnoreCase) // Ignoriere UnknownAuthority, da wir es im Callback erlaubt haben
                                    )
                        .ToList();
                    
                    chainDetails.Add(new CertificateChainInfo
                    {
                        Certificate = ParseBasicCertificateDataForChain(chainElement.Certificate),
                        Errors = errors.Any() ? errors : null,
                        IsSelfSigned = chainElement.Certificate.IssuerName.Name == chainElement.Certificate.SubjectName.Name
                    });
                    if(chainElement.Certificate != cert) chainElement.Certificate.Dispose(); // Dispose intermediate certs
                }
            }

            return new FullCertificateDetails
            {
                MainCertificate = basicData,
                SubjectAlternativeNamesFormatted = sansFormatted.Any() ? sansFormatted.Distinct().ToList() : null,
                Extensions = extensions,
                Chain = chainDetails.Any() ? chainDetails : null,
                RawCertificateBase64 = Convert.ToBase64String(cert.RawData)
            };
        }
        
        private BasicCertificateData ParseBasicCertificateDataForChain(X509Certificate2 cert) // Eigene Methode für Kettenglieder, um Rekursion zu vermeiden
        {
            int? pkLength = null;
            try { pkLength = cert.PublicKey.GetRSAPublicKey()?.KeySize ?? cert.PublicKey.GetECDsaPublicKey()?.KeySize; } catch { /* Ignorieren */ }

             return new BasicCertificateData
            {
                Subject = ParseDn(cert.SubjectName.Name ?? string.Empty),
                Issuer = ParseDn(cert.IssuerName.Name ?? string.Empty),
                Version = cert.Version.ToString(),
                SerialNumber = cert.SerialNumber,
                ThumbprintSha1 = cert.GetCertHashString(HashAlgorithmName.SHA1),
                ThumbprintSha256 = cert.GetCertHashString(HashAlgorithmName.SHA256),
                SignatureAlgorithm = cert.SignatureAlgorithm.FriendlyName,
                PublicKeyAlgorithm = cert.PublicKey.Oid.FriendlyName,
                PublicKeyLength = pkLength,
                Validity = new CertificateValidityInfo
                {
                    NotBeforeUtc = cert.NotBefore.ToUniversalTime(),
                    NotAfterUtc = cert.NotAfter.ToUniversalTime(),
                    DaysRemaining = Math.Round((cert.NotAfter.ToUniversalTime() - DateTime.UtcNow).TotalDays, 2),
                    Status = DateTime.UtcNow < cert.NotBefore.ToUniversalTime() ? "Not Yet Valid" : (DateTime.UtcNow > cert.NotAfter.ToUniversalTime() ? "Expired" : "Valid")
                }
            };
        }
    }
}