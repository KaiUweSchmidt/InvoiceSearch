# Copilot Instructions for InvoiceSearch

## Project Overview

InvoiceSearch is a WPF desktop application (.NET 10, C# 14) for searching invoices in e-mail accounts using MailKit/IMAP.

## Technology Stack

- **Framework:** .NET 10 (`net10.0-windows`)
- **UI:** WPF (XAML + code-behind)
- **Language:** C# 14
- **E-Mail:** MailKit (IMAP)
- **Nullable:** enabled
- **ImplicitUsings:** enabled

## Coding Conventions

- Use modern C# 14 features where appropriate (file-scoped namespaces, primary constructors, pattern matching, raw string literals, switch expressions, ranges/indices).
- Follow WPF best practices: prefer MVVM where practical, use data binding, keep code-behind minimal.
- Namespace: `InvoiceSearch`.
- Use German for user-facing UI strings (labels, menu items, dialogs, messages).
- Use English for code identifiers (class names, method names, variable names, comments).

## Architecture Guidelines

- Keep view models, models, and views in separate folders when the project grows.
- Use `async/await` end-to-end for all I/O operations (MailKit IMAP calls, file access).
- Pass `CancellationToken` through async call chains.
- Guard public method parameters with `ArgumentNullException.ThrowIfNull()`.
- Prefer records for DTOs and configuration models.

## MailKit / E-Mail Specific

- Use `MailKit.Net.Imap.ImapClient` for IMAP connections.
- Always dispose IMAP connections properly (`await using`).
- Support OAuth2 and password-based authentication.
- Store credentials securely — never hardcode secrets.
- Use the Documentation https://github.com/jstedfast/MailKit/tree/master/Documentation for reference on MailKit usage and best practices.

## WPF / UI Specific

- Menu items and dialog labels should be in German.
- Use `Window` or `UserControl` for dialogs — no third-party UI frameworks unless already referenced.
- Prefer XAML for layout; use code-behind only for event wiring or logic that cannot be expressed in bindings.

## Testing

- Test project naming: `InvoiceSearch.Tests`.
- Use xUnit for unit tests.
- Name tests by behavior: `WhenConditionThenExpectedResult`.
- Mock external dependencies (MailKit) only; don't mock internal code.

## Security

- Never store passwords in plain text or in source control.
- Use `System.Security.Cryptography.ProtectedData` (DPAPI) or a secure vault for credential storage on Windows.
