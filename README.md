# InvoiceSearch

Eine Windows-Desktop-Anwendung (WPF) zur automatischen Erkennung und Verwaltung von Rechnungen in E-Mail-Postfächern. Die App durchsucht IMAP-Konten nach PDF-Anhängen, extrahiert deren Text und klassifiziert sie mithilfe eines lokalen KI-Modells (Ollama/Llama 3) als Rechnung oder Nicht-Rechnung.

## Features

### E-Mail-Anbindung (IMAP)
- Verbindung zu beliebig vielen E-Mail-Konten über IMAP/SSL (MailKit)
- Inkrementelles Laden neuer E-Mails – bereits verarbeitete Nachrichten werden übersprungen
- Automatische Erkennung von UidValidity-Änderungen mit Cache-Invalidierung
- Unterstützung für PDF-, TXT-, CSV-, HTML- und XML-Anhänge

### KI-gestützte Rechnungserkennung
- Lokale Klassifizierung über [Ollama](https://ollama.com/) mit dem Llama 3-Modell – keine Cloud-Anbindung nötig
- Erkennung von Rechnungsbetrag, Rechnungsdatum und Rechnungsersteller
- Umfangreiche Ausschlussliste (Gehaltsabrechnungen, Versicherungsbescheide, Angebote, Kontoauszüge u. v. m.)
- Benutzerdefinierte Klassifikationsregeln zur Feinsteuerung der Erkennung

### Dokumenten-Cache
- Alle E-Mail-Anhänge werden lokal in einer SQLite-Datenbank zwischengespeichert
- Klassifizierungsergebnisse werden persistent gespeichert und beim Start automatisch geladen
- Erneute Klassifizierung ohne Server-Zugriff möglich

### Export
- Konfigurierbarer Standard-Ablageort für Rechnungen
- Batch-Export aller angezeigten, noch nicht exportierten Rechnungen
- Automatische Gruppierung in Verzeichnisse nach ausgeschriebenem Monat des Rechnungsdatums (z. B. `Januar`, `Februar`)
- Export-Zeitstempel wird pro Dokument gespeichert und in der Übersicht angezeigt

### Verwaltung & Anzeige
- DataGrid-Übersicht mit Sortierung: Absender, Eingangsdatum, Betreff, Anhang, Rechnungsbetrag, Rechnungsdatum, Rechnungsersteller, Exportiert
- Manuelles Ausschließen/Einschließen einzelner Dokumente (Mehrfachauswahl)
- Filter: Ausgeschlossene und/oder bereits exportierte Dokumente ausblenden
- Anhänge direkt aus der App heraus öffnen
- Fensterposition und -größe werden sitzungsübergreifend gespeichert

### Sicherheit
- Passwörter werden mit DPAPI (Windows Data Protection API) verschlüsselt gespeichert
- Keine Klartext-Speicherung von Zugangsdaten

## Voraussetzungen

- **Windows 10/11** (WPF-Anwendung)
- [**.NET 10 SDK**](https://dotnet.microsoft.com/download/dotnet/10.0)
- [**Ollama**](https://ollama.com/) lokal installiert mit dem Modell `llama3`:
  ```bash
  ollama pull llama3
  ```

## Bauen & Starten

```bash
cd InvoiceSearch
dotnet build
dotnet run
```

## Erste Schritte

1. **E-Mail-Konto anlegen:** *Datei → Konten* – IMAP-Server, Port, Benutzername und Passwort eingeben. Die Verbindung kann direkt getestet werden.
2. **Ollama starten:** Sicherstellen, dass Ollama lokal läuft (`http://localhost:11434`).
3. **Suche starten:** Über die Toolbar *🔍 Suche starten* – E-Mails werden geladen, gecacht und klassifiziert.
4. **Ablageort festlegen:** *Datei → Ablageort festlegen…* – Zielverzeichnis für den Export wählen.
5. **Exportieren:** *📁 Exportieren* – Alle angezeigten Rechnungen werden in den Ablageort exportiert.

## Technologie-Stack

| Komponente | Technologie |
|---|---|
| Framework | .NET 10, C# 14 |
| UI | WPF (XAML + Code-behind) |
| E-Mail | MailKit (IMAP/SSL) |
| PDF-Extraktion | PdfPig |
| KI-Klassifizierung | Ollama REST API (Llama 3) |
| Datenbank | SQLite (Microsoft.Data.Sqlite) |
| Verschlüsselung | DPAPI (System.Security.Cryptography.ProtectedData) |

## Projektstruktur

```
InvoiceSearch/
├── Data/                          # SQLite-Repositories
│   ├── AccountRepository.cs       # E-Mail-Konten CRUD
│   ├── ClassificationRuleRepository.cs  # Klassifikationsregeln CRUD
│   └── DocumentCacheRepository.cs # Dokumenten-Cache mit Schemamigration
├── Models/                        # Datenmodelle (Records)
│   ├── CachedDocument.cs          # Gecachter E-Mail-Anhang
│   ├── ClassificationRule.cs      # Benutzerdefinierte Regel
│   ├── EmailAccount.cs            # IMAP-Kontokonfiguration
│   ├── InvoiceAnalysis.cs         # KI-Analyseergebnis
│   └── InvoiceSearchResult.cs     # Anzeige-DTO für das DataGrid
├── Services/                      # Geschäftslogik
│   ├── AppSettingsService.cs      # App-Einstellungen (Ablageort)
│   ├── ClassificationService.cs   # Ollama-Kommunikation & Prompt-Aufbau
│   ├── CredentialProtector.cs     # DPAPI-Verschlüsselung
│   ├── ImapTestService.cs         # IMAP-Verbindungstest
│   ├── MailSearchService.cs       # Fetch, Cache, Klassifizierungs-Pipeline
│   ├── PdfTextExtractor.cs        # PDF-Textextraktion (PdfPig)
│   └── WindowPlacementService.cs  # Fensterposition speichern/laden
├── AccountDialog.xaml/.cs         # Dialog: Konto anlegen/bearbeiten
├── AccountsWindow.xaml/.cs        # Fenster: Kontoverwaltung
├── ClassificationRuleDialog.xaml/.cs  # Dialog: Regel anlegen/bearbeiten
├── ClassificationRulesWindow.xaml/.cs # Fenster: Regelverwaltung
├── MainWindow.xaml/.cs            # Hauptfenster mit DataGrid & Toolbar
└── InvoiceSearch.csproj           # Projektdatei
```

## Datenspeicherung

Alle lokalen Daten liegen unter `%LocalAppData%\InvoiceSearch\`:

| Datei | Inhalt |
|---|---|
| `accounts.db` | SQLite-Datenbank (Konten, Dokumenten-Cache, Klassifikationsregeln) |
| `window-placement.json` | Fensterposition und -größe |
| `app-settings.json` | App-Einstellungen (Standard-Ablageort) |
