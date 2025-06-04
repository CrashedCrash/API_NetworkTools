# API_NetworkTools

Herzlich willkommen bei API_NetworkTools! Dies ist eine ASP.NET Core Web API, die eine Sammlung von Netzwerk-Dienstprogrammen bereitstellt. Entwickelt mit .NET 9, bietet sie Werkzeuge wie Ping, einen URL-Verkürzer, DNS-Lookups (A, AAAA, MX, NS, TXT, Reverse DNS), Traceroute, Port-Scanner, Whois-Abfragen, IP-Geolocation, HTTP-Header-Anzeige und SSL-Zertifikatsinformationen.

## 🌐 Live-Demo & Testen

Du kannst die API und ihre Funktionen direkt online auf der folgenden Webseite ausprobieren:
[API Live-Test auf SolidState.Network](https://solidstate.network/?page_id=1815)

## ✨ Hauptfunktionen

* **Umfangreiche Tool-Sammlung**: Bietet 14 verschiedene Netzwerk-Diagnose- und Informationswerkzeuge.
* **Moderne Technologie**: Entwickelt mit ASP.NET Core 9.0.
* **Datenbankintegration**: Verwendet SQLite und Entity Framework Core für den URL-Verkürzer, inklusive automatischer Migrationen beim Start.
* **Interaktive API-Dokumentation**: Via Swagger UI (`/swagger`).
* **CORS-Unterstützung**: Konfiguriert, um Anfragen von beliebigen Ursprüngen zu erlauben.

## 🛠️ Verwendete Technologien

* ASP.NET Core 9.0
* Entity Framework Core
* SQLite
* Swashbuckle (Swagger UI)
* DnsClient.NET (für erweiterte DNS-Lookups)
* WhoisClient.NET (für Whois-Abfragen)

## 🚀 Erste Schritte

### Voraussetzungen

* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) oder eine neuere kompatible Version.

### Installation & Ausführung

1.  **Repository klonen:**
    ```bash
    git clone [https://github.com/SolidStateNetwork/API_NetworkTools.git](https://github.com/SolidStateNetwork/API_NetworkTools.git)
    cd API_NetworkTools
    ```

2.  **Abhängigkeiten wiederherstellen:**
    Navigiere in das Projektverzeichnis (`API_NetworkTools/`) und führe aus:
    ```bash
    dotnet restore API_NetworkTools.csproj
    ```

3.  **Datenbank einrichten:**
    Die API nutzt SQLite für den URL-Verkürzer. Die Datenbankdatei (`urlshortener.db`) wird automatisch im Hauptverzeichnis der API (`API_NetworkTools/`) erstellt. Notwendige Migrationen werden beim ersten Start der Anwendung ausgeführt.
    Die Verbindungszeichenfolge ist in `API_NetworkTools/appsettings.json` unter `ConnectionStrings:DefaultConnection` definiert.

4.  **Anwendung starten:**
    ```bash
    dotnet run --project API_NetworkTools.csproj
    ```
    Die API ist standardmäßig unter `https://localhost:7067` und `http://localhost:5199` erreichbar (siehe `API_NetworkTools/Properties/launchSettings.json`).

## 🛡️ Wichtige Berechtigungshinweise für Linux

Damit die Netzwerk-Tools **Ping** und **Traceroute** unter Linux korrekt funktionieren, wenn die API mit einem nicht-privilegierten Benutzer (z.B. `www-data`) ausgeführt wird, benötigt die ausführbare Datei der Anwendung spezielle Berechtigungen.

Nachdem du die Anwendung veröffentlicht hast (siehe Abschnitt "Veröffentlichen für Linux"), führe folgende Befehle für die ausführbare Datei aus (z.B. `API_NetworkTools/bin/publish/linux-x64-selfcontained/API_NetworkTools`):

1.  **Ausführbar machen:**
    ```bash
    sudo chmod a+x /pfad/zu/deiner/API_NetworkTools_Executable
    ```
    *(Hinweis: Sicherer ist es oft, spezifischere Rechte zu vergeben, z.B. `sudo chown root:www-data /pfad/zur/executable` und dann `sudo chmod 750 /pfad/zur/executable`)*

2.  **Netzwerk-RAW-Fähigkeit gewähren:**
    Um ICMP-Anfragen (benötigt für Ping/Traceroute) ohne volle Root-Rechte senden zu können:
    ```bash
    sudo setcap cap_net_raw+eip /pfad/zu/deiner/API_NetworkTools_Executable
    ```
    Ersetze `/pfad/zu/deiner/API_NetworkTools_Executable` mit dem tatsächlichen Pfad zu deiner veröffentlichten ausführbaren Datei.

Diese Schritte sind typischerweise notwendig, wenn die API als systemd-Service betrieben wird.

## 📚 API-Dokumentation & Endpunkte

### Interaktive Dokumentation

Sobald die Anwendung läuft, ist eine interaktive API-Dokumentation über Swagger UI verfügbar. Öffne dazu folgende URL in deinem Browser:
`/swagger` (z.B. `https://localhost:7067/swagger`)

### Haupt-Endpunkte

* **`GET /api/tools`**
    * Beschreibung: Ruft eine Liste aller verfügbaren Netzwerk-Tools ab.
    * Antwort: Eine Liste von `ToolInfo`-Objekten (Identifier, Anzeigename, Beschreibung, Parameter).
* **`GET /api/tools/execute`**
    * Beschreibung: Führt ein bestimmtes Netzwerk-Tool aus.
    * Query-Parameter:
        * `toolIdentifier` (string, erforderlich): Der eindeutige Bezeichner des Tools.
        * `target` (string, erforderlich): Das Ziel für das Tool (z.B. Hostname, IP, URL).
        * `options` (Dictionary<string, string>, optional): Zusätzliche, toolspezifische Optionen.
    * Antwort: Ein `ToolOutput`-Objekt mit dem Ergebnis.
* **`GET /s/{shortCode}`**
    * Beschreibung: Leitet zur ursprünglichen langen URL weiter, die mit dem `shortCode` verknüpft ist. Erhöht den Klickzähler.

## 🧰 Verfügbare Tools

Alle Tools werden über den Endpunkt `/api/tools/execute` mit dem entsprechenden `toolIdentifier` aufgerufen.

1.  **Ping**
    * `toolIdentifier`: `ping`
    * Beschreibung: Sendet ICMP Echo-Anfragen an einen Host.
    * `target`: Hostname oder IP-Adresse.
    * Beispiel: `/api/tools/execute?toolIdentifier=ping&target=google.com`

2.  **URL Shortener**
    * `toolIdentifier`: `url-shortener`
    * Beschreibung: Erstellt eine kurze URL für eine lange URL.
    * `target`: Die zu kürzende URL (muss mit `http://` oder `https://` beginnen).
    * Hinweis: Die Basis-URL für Kurzlinks ist in `appsettings.json` unter `AppSettings:ShortLinkBaseUrl` konfigurierbar.
    * Beispiel: `/api/tools/execute?toolIdentifier=url-shortener&target=https://www.example.com/sehr/lange/url`

3.  **A Record Lookup (IPv4)**
    * `toolIdentifier`: `a-lookup`
    * Beschreibung: Findet IPv4-Adressen (A-Records) für einen Hostnamen.
    * `target`: Hostname.
    * Beispiel: `/api/tools/execute?toolIdentifier=a-lookup&target=github.com`

4.  **AAAA Record Lookup (IPv6)**
    * `toolIdentifier`: `aaaa-lookup`
    * Beschreibung: Findet IPv6-Adressen (AAAA-Records) für einen Hostnamen.
    * `target`: Hostname.
    * Beispiel: `/api/tools/execute?toolIdentifier=aaaa-lookup&target=google.com`

5.  **Traceroute**
    * `toolIdentifier`: `traceroute`
    * Beschreibung: Verfolgt die Route zu einem Host (filtert standardmäßig lokale Hops).
    * `target`: Hostname oder IP-Adresse.
    * Beispiel: `/api/tools/execute?toolIdentifier=traceroute&target=google.com`

6.  **Reverse DNS Lookup (PTR Record)**
    * `toolIdentifier`: `reverse-dns`
    * Beschreibung: Ermittelt den Hostnamen zu einer IP-Adresse.
    * `target`: IP-Adresse.
    * Beispiel: `/api/tools/execute?toolIdentifier=reverse-dns&target=8.8.8.8`

7.  **MX Record Lookup**
    * `toolIdentifier`: `mx-lookup`
    * Beschreibung: Findet Mail Exchange (MX) Records für eine Domain.
    * `target`: Domainname.
    * Beispiel: `/api/tools/execute?toolIdentifier=mx-lookup&target=google.com`

8.  **NS Record Lookup**
    * `toolIdentifier`: `ns-lookup`
    * Beschreibung: Findet Name Server (NS) Records für eine Domain.
    * `target`: Domainname.
    * Beispiel: `/api/tools/execute?toolIdentifier=ns-lookup&target=github.com`

9.  **TXT Record Lookup**
    * `toolIdentifier`: `txt-lookup`
    * Beschreibung: Findet Text (TXT) Records für eine Domain.
    * `target`: Domainname.
    * Beispiel: `/api/tools/execute?toolIdentifier=txt-lookup&target=google.com`

10. **Port Scanner**
    * `toolIdentifier`: `port-scan`
    * Beschreibung: Überprüft den Status von TCP-Ports (verantwortungsbewusst einsetzen!).
    * `target`: Hostname oder IP-Adresse.
    * `options`:
        * `ports` (erforderlich): Kommagetrennte Ports (z.B. "80,443,22").
        * `timeout` (optional): Timeout pro Port in ms (Standard: 2000).
    * Beispiel: `/api/tools/execute?toolIdentifier=port-scan&target=scanme.nmap.org&options[ports]=22,80,443`

11. **Whois Lookup**
    * `toolIdentifier`: `whois-lookup`
    * Beschreibung: Ruft öffentliche Registrierungsinformationen für eine Domain ab.
    * `target`: Domainname (z.B. "google.com").
    * Beispiel: `/api/tools/execute?toolIdentifier=whois-lookup&target=example.com`

12. **IP Geolocation**
    * `toolIdentifier`: `ip-geolocation`
    * Beschreibung: Ermittelt geografische Informationen zu einer IP-Adresse oder Domain (via ip-api.com).
    * `target`: IP-Adresse oder Domainname.
    * Hinweis: Verwendet den kostenlosen Endpunkt von ip-api.com (Ratenbegrenzung beachten).
    * Beispiel (IP): `/api/tools/execute?toolIdentifier=ip-geolocation&target=1.1.1.1`
    * Beispiel (Domain): `/api/tools/execute?toolIdentifier=ip-geolocation&target=google.com`

13. **HTTP Header Viewer**
    * `toolIdentifier`: `http-headers`
    * Beschreibung: Zeigt HTTP-Antwort-Header einer URL an.
    * `target`: Vollständige URL (z.B. "https://www.google.com").
    * `options`:
        * `method` (optional): "HEAD" (Standard) oder "GET".
    * Beispiel: `/api/tools/execute?toolIdentifier=http-headers&target=https://example.com&options[method]=HEAD`

14. **SSL Certificate Information**
    * `toolIdentifier`: `ssl-cert-info`
    * Beschreibung: Ruft SSL/TLS-Zertifikatsdetails von einem Host ab.
    * `target`: Hostname des Servers (z.B. "google.com").
    * `options`:
        * `port` (optional): Portnummer (Standard: 443).
    * Beispiel: `/api/tools/execute?toolIdentifier=ssl-cert-info&target=google.com&options[port]=443`

## 📦 Veröffentlichen für Linux (Self-Contained)

Das Projekt enthält einen VS Code-Task in `.vscode/tasks.json` (oder `API_NetworkTools/tasks.json` falls im Projektordner) zum Veröffentlichen einer eigenständigen Linux x64-Version.

Alternativ über die Kommandozeile (im Hauptverzeichnis des Repositories ausführen):
```bash
dotnet publish API_NetworkTools/API_NetworkTools.csproj --configuration Release --runtime linux-x64 --self-contained true --output API_NetworkTools/bin/publish/linux-x64-selfcontained
