# API NetworkTools

Eine ASP.NET Core Web API, die eine Sammlung von Netzwerk-Dienstprogrammen bereitstellt.

## Inhaltsverzeichnis

- Über das Projekt
- Funktionen
- Technologie-Stack
- API Endpunkte
  - Verfügbare Tools auflisten
  - Tool ausführen
  - URL-Weiterleitung
- Erste Schritte
  - Voraussetzungen
  - Installation & Konfiguration
  - Datenbankmigration
  - API starten
- Verwendung der Tools (Beispiele)
  - Ping
  - URL Shortener
  - A Record Lookup (IPv4)
  - AAAA Record Lookup (IPv6)
- Veröffentlichen

## Über das Projekt

API_NetworkTools ist eine vielseitige Web-API, die entwickelt wurde, um gängige Netzwerkaufgaben über einfache HTTP-Anfragen auszuführen. Sie beinhaltet Tools für DNS-Lookups, Ping-Anfragen und das Kürzen von URLs. Die API ist mit ASP.NET Core erstellt und verwendet Entity Framework Core mit SQLite für die Persistenz von Daten des URL-Kürzers.

## Funktionen

Die API stellt folgende Netzwerk-Tools zur Verfügung:

* **Ping**: Sendet ICMP Echo-Anfragen an einen angegebenen Host oder eine IP-Adresse, um die Erreichbarkeit zu überprüfen und die Antwortzeit zu messen.
* **URL Shortener**: Generiert einen kurzen, eindeutigen Code für eine lange URL und leitet Benutzer bei Zugriff auf den Kurzlink zur ursprünglichen URL weiter. Klicks werden gezählt.
* **A Record Lookup (IPv4)**: Ruft die IPv4-Adressen (A-Records) ab, die mit einem Hostnamen verbunden sind.
* **AAAA Record Lookup (IPv6)**: Ruft die IPv6-Adressen (AAAA-Records) ab, die mit einem Hostnamen verbunden sind.

## Technologie-Stack

* **ASP.NET Core 9.0**: Framework zum Erstellen der Web-API.
* **Entity Framework Core (EF Core)**: ORM für die Datenbankinteraktion, insbesondere für den URL Shortener.
* **SQLite**: Leichtgewichtige, dateibasierte Datenbank, die für den URL Shortener verwendet wird.
* **Swagger / OpenAPI**: Zur Dokumentation und zum Testen der API-Endpunkte.
* **C#**: Hauptprogrammiersprache.

## API Endpunkte

Die API stellt folgende Endpunkte bereit:

### Verfügbare Tools auflisten

* **GET** `/api/tools`
    * Beschreibung: Gibt eine Liste aller verfügbaren Netzwerk-Tools zurück, einschließlich ihrer Bezeichner, Anzeigenamen, Beschreibungen und erforderlichen Parameter.
    * Antwort:
        > [
        >   {
        >     "identifier": "ping",
        >     "displayName": "Ping",
        >     "description": "Sendet ICMP Echo-Anfragen an einen Host.",
        >     "parameters": []
        >   },
        >   {
        >     "identifier": "url-shortener",
        >     "displayName": "URL Shortener",
        >     "description": "Erstellt eine kurze, eindeutige URL für eine lange URL.",
        >     "parameters": []
        >   },
        >   // ... weitere Tools
        > ]

### Tool ausführen

* **GET** `/api/tools/execute`
    * Beschreibung: Führt ein bestimmtes Netzwerk-Tool mit den angegebenen Parametern aus.
    * Query-Parameter:
        * `toolIdentifier` (string, erforderlich): Der Bezeichner des auszuführenden Tools (z.B. "ping", "url-shortener").
        * `target` (string, erforderlich): Das Ziel für das Tool (z.B. eine IP-Adresse für Ping, eine lange URL für den URL Shortener).
        * `options` (Dictionary<string, string>, optional): Zusätzliche optionsspezifische Parameter für das Tool.
    * Antwort (Beispiel für Ping):
        > // Erfolg
        > {
        >   "success": true,
        >   "toolName": "Ping",
        >   "data": {
        >     "target": "google.com",
        >     "ipAddress": "142.250.185.14",
        >     "roundtripTime": 15,
        >     "ttl": 117,
        >     "status": "Success"
        >   },
        >   "errorMessage": null,
        >   "rawOutput": null
        > }
        >
        > // Fehler
        > {
        >   "success": false,
        >   "toolName": "Ping",
        >   "data": null,
        >   "errorMessage": "Ziel (Host/IP) darf nicht leer sein.",
        >   "rawOutput": null
        > }

### URL-Weiterleitung

* **GET** `/s/{shortCode}`
    * Beschreibung: Leitet zu der ursprünglichen langen URL weiter, die dem angegebenen `shortCode` zugeordnet ist. Erhöht auch den Klickzähler für den Kurzlink.
    * Pfad-Parameter:
        * `shortCode` (string, erforderlich): Der generierte Kurzcode für die URL.
    * Antwort:
        * `302 Found` mit `Location`-Header zur langen URL bei Erfolg.
        * `404 Not Found`, wenn der Kurzcode nicht existiert oder leer ist.

## Erste Schritte

Folge diesen Anweisungen, um das Projekt lokal einzurichten und auszuführen.

### Voraussetzungen

* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (oder höher)
* Git (optional, zum Klonen des Repositories)

### Installation & Konfiguration

1.  **Klone das Repository (optional):**
    > git clone <repository-url>
    > cd API_NetworkTools

2.  **Abhängigkeiten wiederherstellen:**
    Navigiere in das Projektverzeichnis `API_NetworkTools` und führe aus:
    > dotnet restore

3.  **Konfiguration:**
    Die Hauptkonfiguration befindet sich in `API_NetworkTools/appsettings.json`.
    Für Entwicklungseinstellungen kann `API_NetworkTools/appsettings.Development.json` verwendet werden.
    * **Datenbankverbindung**: Die Standard-Datenbankverbindung ist für SQLite konfiguriert und lautet `Data Source=urlshortener.db`. Diese Datei wird im Hauptverzeichnis der Anwendung erstellt.
        > // appsettings.json (Beispiel für DefaultConnection)
        > {
        >   "ConnectionStrings": {
        >     "DefaultConnection": "Data Source=urlshortener.db"
        >   },
        >   // ...
        > }
    * **CORS**: Standardmäßig sind CORS-Richtlinien so konfiguriert, dass Anfragen von beliebigen Ursprüngen, mit beliebigen Headern und Methoden erlaubt sind (`policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();`). Dies ist in `Program.cs` definiert.

### Datenbankmigration

Für den URL Shortener wird eine Datenbank verwendet. Stelle sicher, dass die Migrationen angewendet werden, um das Datenbankschema zu erstellen/aktualisieren:

Das Projekt ist so konfiguriert, dass Migrationen beim Start der Anwendung automatisch angewendet werden:
> // API_NetworkTools/Program.cs
> using (var scope = app.Services.CreateScope())
> {
>     var services = scope.ServiceProvider;
>     try
>     {
>         var dbContext = services.GetRequiredService<AppDbContext>();
>         dbContext.Database.Migrate(); // Wendet ausstehende Migrationen an
>     }
>     catch (Exception ex)
>     {
>         var logger = services.GetRequiredService<ILogger<Program>>();
>         logger.LogError(ex, "An error occurred while migrating or initializing the database.");
>     }
> }

Falls du Migrationen manuell verwalten möchtest (z.B. neue erstellen):
> dotnet tool install --global dotnet-ef
> dotnet ef migrations add NameDerMigration -p API_NetworkTools/API_NetworkTools.csproj -s API_NetworkTools/API_NetworkTools.csproj
> dotnet ef database update -p API_NetworkTools/API_NetworkTools.csproj -s API_NetworkTools/API_NetworkTools.csproj

### API starten

Du kannst die API über die Kommandozeile starten:
> cd API_NetworkTools
> dotnet run

Standardmäßig läuft die Anwendung unter:
* HTTP: `http://localhost:5199`
* HTTPS: `https://localhost:7067` (falls konfiguriert)

Nach dem Start ist die Swagger UI unter `http://localhost:5199/swagger` (oder der entsprechenden HTTPS-URL) verfügbar, um die API-Endpunkte zu erkunden und zu testen.

## Verwendung der Tools (Beispiele)

Hier sind einige Beispiele, wie du die Tools über `curl` oder einen API-Client wie Postman verwenden kannst.

### Ping

> curl -X GET "http://localhost:5199/api/tools/execute?toolIdentifier=ping&target=google.com"

Erwartete Antwort (Erfolg):
> {
>   "success": true,
>   "toolName": "Ping",
>   "data": {
>     "target": "google.com",
>     "ipAddress": "...", // IP-Adresse von google.com
>     "roundtripTime": 20, // Beispiel-Antwortzeit
>     "ttl": 56, // Beispiel-TTL
>     "status": "Success"
>   },
>   "errorMessage": null,
>   "rawOutput": null
> }

### URL Shortener

> curl -X GET "http://localhost:5199/api/tools/execute?toolIdentifier=url-shortener&target=https%3A%2F%2Fwww.google.com%2Fsearch%3Fq%3Dsehr%2Blange%2Burl%2Bmit%2Bvielen%2Bparametern"

Erwartete Antwort (Erfolg):
> {
>   "success": true,
>   "toolName": "URL Shortener",
>   "data": {
>     "shortUrl": "https://api.solidstate.network/s/xxxxxx", // xxxxxxx ist der generierte Kurzcode
>     "longUrl": "https://www.google.com/search?q=sehr+lange+url+mit+vielen+parametern"
>   },
>   "errorMessage": null,
>   "rawOutput": null
> }

Anschließend kannst du `https://api.solidstate.network/s/xxxxxx` im Browser öffnen, um zur langen URL weitergeleitet zu werden.
*Hinweis: Die `ShortLinkBaseUrl` ist derzeit auf `https://api.solidstate.network/s/` hardcodiert. Für lokale Tests wird die Weiterleitung über deinen lokalen Host erfolgen, z.B. `http://localhost:5199/s/xxxxxx`.*

### A Record Lookup (IPv4)

> curl -X GET "http://localhost:5199/api/tools/execute?toolIdentifier=a-lookup&target=github.com"

Erwartete Antwort (Erfolg):
> {
>   "success": true,
>   "toolName": "A Record Lookup (IPv4)",
>   "data": [
>     "140.82.121.4" // Beispiel-IPv4-Adresse
>   ],
>   "errorMessage": null,
>   "rawOutput": null
> }

### AAAA Record Lookup (IPv6)

> curl -X GET "http://localhost:5199/api/tools/execute?toolIdentifier=aaaa-lookup&target=google.com"

Erwartete Antwort (Erfolg):
> {
>   "success": true,
>   "toolName": "AAAA Record Lookup (IPv6)",
>   "data": [
>     "2a00:1450:4001:82b::200e" // Beispiel-IPv6-Adresse
>   ],
>   "errorMessage": null,
>   "rawOutput": null
> }

## Veröffentlichen

Das Projekt enthält eine VS Code Task-Konfiguration (`.vscode/tasks.json`), um die Anwendung für Linux (als selbst gehostete, eigenständige Anwendung) zu veröffentlichen.

**Task-Label**: `Publish for Linux (Self-Contained)`
**Befehl**: `dotnet publish API_NetworkTools/API_NetworkTools.csproj --configuration Release --runtime linux-x64 --self-contained true --output API_NetworkTools/bin/publish/linux-x64-selfcontained`

Du kannst diesen Task in VS Code über `Terminal > Run Task...` ausführen.
Die veröffentlichten Dateien befinden sich dann im Ordner `API_NetworkTools/bin/publish/linux-x64-selfcontained`.
