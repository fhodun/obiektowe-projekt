# AGENT.md

## Multimedialny Menedżer Notatek i Rysunków (MAUI .NET 10)

Realizacja w **.NET MAUI (.NET 10.0)** (zamiast WinForms/WPF). Priorytet: działa i jest czytelnie.

## 1) Funkcje (MVP)
- Lista notatek (Title, UpdatedAt): dodaj/usuń/duplikuj.
- Edycja tekstu notatki.
- Rysowanie odręczne: kolor, grubość, Undo (usuń ostatni stroke), Clear.
- Zapis/odczyt notatek jako JSON.
- Szyfrowanie danych w storage lokalnym (AES).
- Eksport wybranych notatek do ZIP.
- Timer auto-zapisu co X sekund (tylko gdy są zmiany).
- Audio: opcjonalnie (podpięcie pliku + odtwarzanie).

## 2) Architektura
MVVM:
- Views (XAML) bez logiki domenowej.
- ViewModels: stan + komendy.
- Models: domena i operator overloady.
- Services: storage, crypto, export, drawing, timer, (opcjonalnie audio).

## 3) Biblioteki (mainstream)
- CommunityToolkit.Mvvm
- CommunityToolkit.Maui (MediaElement, jeśli audio)
Wbudowane: System.Text.Json, System.IO.Compression, System.Security.Cryptography, ObservableCollection.

## 4) Modele
- Note: Id, Title, Body, CreatedAt, UpdatedAt, Drawing, Audio?
- DrawingData: lista Stroke
- Stroke: lista PointF, Thickness, Color(ARGB)
- AudioAttachment: FileName, StoredPath

## 5) Operatory (min. 4)
W Note:
- == i != porównanie po Id
- > i < porównanie po UpdatedAt
- + scala dwie notatki (dokleja Body i stroke’y)

## 6) Generyki (min. 2)
- IRepository<T> + EncryptedJsonRepository<T> (load/save)
- Result<T> (IsSuccess, Value, Error)

## 7) Szyfrowanie
PBKDF2 (Rfc2898DeriveBytes) + AES (preferuj AesGcm). Format pliku: salt|nonce|ciphertext|tag.

## 8) Definicja “gotowe”
Da się dodać notatkę, wpisać tekst, narysować, zapisać, wczytać po restarcie i wyeksportować ZIP. Kod: MVVM, DI, bez spaghetti.
