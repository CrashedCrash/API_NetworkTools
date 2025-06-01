# API_NetworkTools

Herzlich willkommen zu API_NetworkTools! Dies ist eine ASP.NET Core Web API, die eine Sammlung von Netzwerk-Dienstprogrammen bereitstellt. Sie wurde mit .NET 9 entwickelt und beinhaltet Werkzeuge wie Ping, URL-Verkürzer, A-Record-Lookup und AAAA-Record-Lookup.

##  Übersicht

Das Projekt zielt darauf ab, grundlegende Netzwerk-Tools über eine einfach zu bedienende HTTP-API zugänglich zu machen. Es verwendet SQLite für die Persistenz von Daten des URL-Verkürzers und Swagger für eine interaktive API-Dokumentation.

## 🌐 Live-Demo & Testen

Du kannst die API und ihre Funktionen auch direkt online auf der folgenden Webseite ausprobieren:

[API Live-Test auf SolidState.Network](https://solidstate.network/?page_id=1815)

## ✨ Features

* **Ping**: Sendet ICMP Echo-Anfragen an einen Zielhost.
* **A Record Lookup**: Ruft die IPv4-Adressen (A-Records) für einen Hostnamen ab.
* **AAAA Record Lookup**: Ruft die IPv6-Adressen (AAAA-Records) für einen Hostnamen ab.
* **Traceroute**: Verfolgt die Route von Paketen zu einem Netzwerkhost und zeigt die einzelnen Hops an.
* **URL Shortener**: Erstellt eine kurze, eindeutige URL für eine gegebene lange URL und leitet über den Kurzlink zum Original weiter.
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

## 📦 Publishing für Linux

Das Projekt enthält eine vordefinierte VS Code-Task in `API_NetworkTools/tasks.json`, um eine eigenständige (self-contained) Linux x64-Version der Anwendung zu veröffentlichen.

Du kannst die Anwendung auch manuell über die Kommandozeile veröffentlichen. Führe dazu folgenden Befehl im Hauptverzeichnis deines Projekts (dem Verzeichnis, das den Ordner `API_NetworkTools` enthält) aus:

```bash
dotnet publish API_NetworkTools/API_NetworkTools.csproj --configuration Release --runtime linux-x64 --self-contained true --output API_NetworkTools/bin/publish/linux-x64-selfcontained
