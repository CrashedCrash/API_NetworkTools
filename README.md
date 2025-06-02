# API_NetworkTools

Herzlich willkommen zu API_NetworkTools! Dies ist eine ASP.NET Core Web API, die eine Sammlung von Netzwerk-Dienstprogrammen bereitstellt. Sie wurde mit .NET 9 entwickelt und beinhaltet Werkzeuge wie Ping, URL-Verkürzer, A-Record-Lookup, AAAA-Record-Lookup und Traceroute.

##  Übersicht

Das Projekt zielt darauf ab, grundlegende Netzwerk-Tools über eine einfach zu bedienende HTTP-API zugänglich zu machen. Es verwendet SQLite für die Persistenz von Daten des URL-Verkürzers und Swagger für eine interaktive API-Dokumentation.

## 🌐 Live-Demo & Testen

Du kannst die API und ihre Funktionen auch direkt online auf der folgenden Webseite ausprobieren:

[API Live-Test auf SolidState.Network](https://solidstate.network/?page_id=1815)

## ✨ Features

* **Ping**: Sendet ICMP Echo-Anfragen an einen Zielhost.
* **URL Shortener**: Erstellt eine kurze, eindeutige URL für eine gegebene lange URL und leitet über den Kurzlink zum Original weiter.
* **A Record Lookup**: Ruft die IPv4-Adressen (A-Records) für einen Hostnamen ab.
* **AAAA Record Lookup**: Ruft die IPv6-Adressen (AAAA-Records) für einen Hostnamen ab.
* **Traceroute**: Verfolgt die Route von Paketen zu einem Netzwerkhost und zeigt die einzelnen Hops an.
* **Swagger/OpenAPI-Dokumentation**: Interaktive API-Dokumentation über Swagger UI.
* **CORS**: Konfiguriert, um Anfragen von beliebigen Ursprüngen zu erlauben.
* **Datenbank-Migrationen**: Verwendet Entity Framework Core für die Datenbankverwaltung des URL-Verkürzers, Migrationen werden beim Start angewendet.

## 🛠️ Verwendete Technologien

* ASP.NET Core 9.0
* Entity Framework Core (für SQLite)
* SQLite
* Swashbuckle (für Swagger UI)

## 🚀 Erste Schritte

### Voraussetzungen

* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) oder eine neuere kompatible Version.

### Installation & Ausführung

1.  **Repository klonen:**
    ```bash
    git clone https://github.com/SolidStateNetwork/API_NetworkTools.git
    ```

2.  **Abhängigkeiten wiederherstellen:**
    ```bash
    dotnet restore API_NetworkTools/API_NetworkTools.csproj
    ```

3.  **Datenbank einrichten:**
    Die API verwendet SQLite für den URL-Verkürzer. Die Datenbankdatei (`urlshortener.db`) wird automatisch im Hauptverzeichnis der API (`API_NetworkTools/`) erstellt und die notwendigen Migrationen werden beim ersten Start der Anwendung ausgeführt.
    Die Verbindungszeichenfolge kann in `API_NetworkTools/appsettings.json` unter `ConnectionStrings:DefaultConnection` angepasst werden. Falls nicht vorhanden, wird der Standardwert `Data Source=urlshortener.db` verwendet.

4.  **Anwendung starten:**
    ```bash
    dotnet run --project API_NetworkTools/API_NetworkTools.csproj
    ```
    Die API ist standardmäßig unter `https://localhost:7067` und `http://localhost:5199` erreichbar (siehe `API_NetworkTools/Properties/launchSettings.json`).

## 🛡️ Wichtige Hinweise zu Berechtigungen (Linux)

Damit die Netzwerk-Tools **Ping** und **Traceroute** korrekt funktionieren, wenn die API unter einem nicht-privilegierten Benutzer (wie z.B. `www-data`) ausgeführt wird, benötigt die ausführbare Datei der Anwendung spezielle Berechtigungen.

Nachdem du die Anwendung veröffentlicht hast (siehe "Publishing für Linux"), führe die folgenden Befehle für die ausführbare Datei aus (z.B. `API_NetworkTools/bin/publish/linux-x64-selfcontained/API_NetworkTools`):

1.  **Ausführbar machen:**
    Stelle sicher, dass die Datei Ausführungsrechte hat. Der Befehl `chmod a+x` gewährt allen Benutzern Ausführungsrechte:
    ```bash
    sudo chmod a+x /pfad/zu/deiner/API_NetworkTools_Executable
    ```
    *Hinweis: Eine restriktivere und oft bevorzugte Methode ist, die Datei dem korrekten Benutzer/Gruppe zuzuordnen (z.B. `sudo chown root:www-data /pfad/zur/executable`) und dann spezifischere Rechte zu vergeben, z.B. `sudo chmod 750 /pfad/zur/executable` oder `sudo chmod 550 /pfad/zur/executable`.*

2.  **Netzwerk-RAW-Fähigkeit gewähren:**
    Um ICMP-Anfragen (benötigt für Ping/Traceroute) ohne volle Root-Rechte senden zu können, muss der ausführbaren Datei die `CAP_NET_RAW`-Fähigkeit zugewiesen werden:
    ```bash
    sudo setcap cap_net_raw+eip /pfad/zu/deiner/API_NetworkTools_Executable
    ```
    Ersetze `/pfad/zu/deiner/API_NetworkTools_Executable` mit dem tatsächlichen Pfad zu deiner veröffentlichten ausführbaren Datei (z.B. `API_NetworkTools/bin/publish/linux-x64-selfcontained/API_NetworkTools` basierend auf der `tasks.json`).

Diese Schritte sind typischerweise notwendig, wenn die API als systemd-Service unter einem Benutzer wie `www-data` betrieben wird.

## 📚 API-Dokumentation

Eine interaktive API-Dokumentation ist über Swagger UI verfügbar, sobald die Anwendung läuft. Öffne dazu folgende URL in deinem Browser:
`/swagger` (z.B. `https://localhost:7067/swagger`)

## 📡 API-Endpunkte

### Tools

* **`GET /api/tools`**
    * Beschreibung: Ruft eine Liste aller verfügbaren Netzwerk-Tools ab.
    * Antwort: Eine Liste von `ToolInfo`-Objekten, die Identifier, Anzeigename, Beschreibung und Parameter jedes Tools enthalten.

* **`GET /api/tools/execute`**
    * Beschreibung: Führt ein bestimmtes Netzwerk-Tool aus.
    * Query-Parameter:
        * `toolIdentifier` (string, erforderlich): Der eindeutige Bezeichner des auszuführenden Tools.
        * `target` (string, erforderlich): Das Ziel für das Tool (z.B. Hostname, IP-Adresse, URL).
        * `options` (Dictionary<string, string>, optional): Zusätzliche Optionen für das Tool.
    * Antwort: Ein `ToolOutput`-Objekt mit dem Ergebnis der Ausführung.

### URL Shortener Redirect

* **`GET /s/{shortCode}`**
    * Beschreibung: Leitet zur ursprünglichen langen URL weiter, die mit dem `shortCode` verknüpft ist.
    * Erhöht den Klickzähler für den jeweiligen Kurzlink.

## 🧰 Verfügbare Tools

Die folgenden Tools sind über den Endpunkt `/api/tools/execute` verfügbar:

1.  **Ping**
    * `toolIdentifier`: `ping`
    * Beschreibung: "Sendet ICMP Echo-Anfragen an einen Host."
    * `target`: Hostname oder IP-Adresse.
    * Beispiel: `/api/tools/execute?toolIdentifier=ping&target=google.com`

2.  **URL Shortener**
    * `toolIdentifier`: `url-shortener`
    * Beschreibung: "Erstellt eine kurze, eindeutige URL für eine lange URL."
    * `target`: Die lange URL, die verkürzt werden soll (muss mit `http://` oder `https://` beginnen).
    * Ausgabe: Enthält die verkürzte URL.
        * **Wichtiger Hinweis:** Die Basis-URL für die generierten Kurzlinks ist derzeit fest auf `https://api.solidstate.network/s/` in der Datei `API_NetworkTools/Tools/Implementations/UrlShortenerTool.cs` codiert.
    * Beispiel: `/api/tools/execute?toolIdentifier=url-shortener&target=https://www.example.com/eine/sehr/lange/url/zur/verkuerzung`

3.  **A Record Lookup (IPv4)**
    * `toolIdentifier`: `a-lookup`
    * Beschreibung: "Findet die IPv4-Adressen (A-Records) für einen Hostnamen."
    * `target`: Der Hostname, für den die A-Records gesucht werden sollen.
    * Beispiel: `/api/tools/execute?toolIdentifier=a-lookup&target=github.com`

4.  **AAAA Record Lookup (IPv6)**
    * `toolIdentifier`: `aaaa-lookup`
    * Beschreibung: "Findet die IPv6-Adressen (AAAA-Records) für einen Hostnamen."
    * `target`: Der Hostname, für den die AAAA-Records gesucht werden sollen.
    * Beispiel: `/api/tools/execute?toolIdentifier=aaaa-lookup&target=google.com`

5.  **Traceroute**
    * `toolIdentifier`: `traceroute`
    * Beschreibung: "Verfolgt die Route von Paketen zu einem Netzwerkhost."
    * `target`: Hostname oder IP-Adresse.
    * Ausgabe: Eine Liste von Hops mit IP-Adresse, Roundtrip-Zeit und Status.
    * Beispiel: `/api/tools/execute?toolIdentifier=traceroute&target=google.com`

6.  **Reverse DNS Lookup (PTR Record)**
    * `toolIdentifier`: `reverse-dns`
    * Beschreibung: "Ermittelt den Hostnamen zu einer gegebenen IP-Adresse."
    * `target`: Die IP-Adresse, für die der Hostname gesucht werden soll.
    * Beispiel: `/api/tools/execute?toolIdentifier=reverse-dns&target=8.8.8.8`

7.  **MX Record Lookup**
    * `toolIdentifier`: `mx-lookup`
    * Beschreibung: "Findet die Mail Exchange (MX) Records für eine Domain."
    * `target`: Der Domainname, für den die MX-Records gesucht werden sollen.
    * Beispiel: `/api/tools/execute?toolIdentifier=mx-lookup&target=google.com`

8.  **NS Record Lookup**
    * `toolIdentifier`: `ns-lookup`
    * Beschreibung: "Findet die Name Server (NS) Records für eine Domain."
    * `target`: Der Domainname, für den die NS-Records gesucht werden sollen.
    * Beispiel: `/api/tools/execute?toolIdentifier=ns-lookup&target=github.com`

9.  **TXT Record Lookup**
    * `toolIdentifier`: `txt-lookup`
    * Beschreibung: "Findet die Text (TXT) Records für eine Domain (z.B. für SPF, DKIM)."
    * `target`: Der Domainname, für den die TXT-Records gesucht werden sollen.
    * Beispiel: `/api/tools/execute?toolIdentifier=txt-lookup&target=google.com`

10. **Port Scanner**
    * `toolIdentifier`: `port-scan`
    * Beschreibung: "Überprüft den Status von TCP-Ports auf einem Zielhost. (Verantwortungsbewusst einsetzen!)"
    * `target`: Hostname oder IP-Adresse.
    * `options`:
        * `ports` (erforderlich): Kommagetrennte Liste von Portnummern (z.B. "80,443,21,22").
        * `timeout` (optional): Timeout pro Port in Millisekunden (Standard: 2000).
    * Beispiel: `/api/tools/execute?toolIdentifier=port-scan&target=scanme.nmap.org&options[ports]=22,80,443&options[timeout]=1000`

11. **Whois Lookup**
    * `toolIdentifier`: `whois-lookup`
    * Beschreibung: "Ruft öffentliche Registrierungsinformationen für einen Domainnamen ab."
    * `target`: Der Domainname, für den die Whois-Informationen gesucht werden sollen (z.B. "google.com").
    * Ausgabe: Enthält den rohen Whois-Text und einige geparste Felder (falls verfügbar).
    * Beispiel: `/api/tools/execute?toolIdentifier=whois-lookup&target=example.com`

12. **IP Geolocation**
    * `toolIdentifier`: `ip-geolocation`
    * Beschreibung: "Ermittelt geografische Informationen und weitere Details zu einer IP-Adresse über den Dienst ip-api.com."
    * `target`: Die IP-Adresse, für die Geolokalisierungsinformationen abgerufen werden sollen (z.B. "8.8.8.8").
    * Hinweis: Verwendet den kostenlosen Endpunkt von ip-api.com, der Ratenbegrenzungen unterliegt (z.B. 45 Anfragen/Minute).
    * Beispiel: `/api/tools/execute?toolIdentifier=ip-geolocation&target=1.1.1.1`

13. **HTTP Header Viewer**
    * `toolIdentifier`: `http-headers`
    * Beschreibung: "Zeigt die HTTP-Antwort-Header von einer URL an."
    * `target`: Die vollständige URL (z.B. "https://www.google.com").
    * `options`:
        * `method` (optional): Die HTTP-Methode, "HEAD" (Standard) oder "GET".
    * Beispiel: `/api/tools/execute?toolIdentifier=http-headers&target=https://www.example.com&options[method]=HEAD`

**SSL Certificate Information**
    * `toolIdentifier`: `ssl-cert-info`
    * Beschreibung: "Ruft Details des SSL/TLS-Zertifikats von einem Host ab (z.B. Aussteller, Gültigkeit, Kette)."
    * `target`: Der Hostname des Servers (z.B. "google.com").
    * `options`:
        * `port` (optional): Die Portnummer (Standard: 443).
    * Beispiel: `/api/tools/execute?toolIdentifier=ssl-cert-info&target=google.com&options[port]=443`

## 📦 Publishing für Linux

Das Projekt enthält eine vordefinierte VS Code-Task in `API_NetworkTools/tasks.json`, um eine eigenständige (self-contained) Linux x64-Version der Anwendung zu veröffentlichen.

Du kannst die Anwendung auch manuell über die Kommandozeile veröffentlichen. Führe dazu folgenden Befehl im Hauptverzeichnis deines Projekts (dem Verzeichnis, das den Ordner `API_NetworkTools` enthält) aus:

```bash
dotnet publish API_NetworkTools/API_NetworkTools.csproj --configuration Release --runtime linux-x64 --self-contained true --output API_NetworkTools/bin/publish/linux-x64-selfcontained
