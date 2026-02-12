# TorrentClient.NET

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)
![React](https://img.shields.io/badge/React-18-61DAFB?style=flat&logo=react)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20Hexagonal-success)

Cliente BitTorrent completo construido desde cero en C# (.NET 10) con Clean Architecture.

Descarga archivos de la red P2P implementando el protocolo BitTorrent (BEP 3): parsing de .torrent, comunicación con trackers HTTP, handshake con peers vía TCP, descarga concurrente de múltiples fuentes simultáneas, y verificación SHA-1 de integridad. Incluye API REST y dashboard React.

---

## Instalación

### Clonar y ejecutar
```bash
git clone https://github.com/tu-usuario/TorrentClient.NET.git
cd TorrentClient.NET

# Backend (API)
cd src/TorrentClient.Presentation.API
dotnet run

# Frontend (nueva terminal)
cd torrent-client-ui
npm install && npm run dev
```

La API estará en `http://localhost:5259` y el dashboard en `http://localhost:5173`.

### CLI
```bash
cd src/TorrentClient.Presentation.CLI
dotnet run -- archivo.torrent
```

---

## Uso

### Web UI
1. Abre `http://localhost:5173`
2. Arrastra un archivo `.torrent`
3. Haz clic en "Start Download"

### API REST
```bash
# Iniciar descarga
curl -X POST http://localhost:5259/api/torrents -F "file=@debian.torrent"

# Ver progreso
curl http://localhost:5259/api/torrents/{id}

# Listar todas
curl http://localhost:5259/api/torrents
```

---

## Arquitectura
```
Domain (Entities: Torrent, Peer, Piece)
  ↑
Application (TorrentManager, UseCases, Ports)
  ↑
Infrastructure (TCP Sockets, HTTP Tracker, Disco, Bencode)
  ↑
Presentation (CLI / API REST / React UI)
```

Implementa **Clean Architecture** con **Hexagonal Pattern** (Ports & Adapters). Domain no depende de nadie, Infrastructure implementa interfaces de Application.

---

## Características técnicas

**Protocolo:**
- Bencode parser (decodificación/codificación)
- Tracker HTTP (discovery de peers)
- Peer Wire Protocol (handshake, bitfield, interested, unchoke, request, piece)
- Verificación SHA-1 pieza por pieza

**Concurrencia:**
- Worker pool con 5 peers simultáneos
- `ConcurrentQueue` thread-safe para distribución de piezas
- `SemaphoreSlim` para escritura segura en disco
- Async/await en toda la app

**Stack:**
- Backend: C# 12, .NET 10, ASP.NET Core
- Frontend: React 18, Vite
- Testing: xUnit, Moq

---

## Estructura
```
src/
├── TorrentClient.Domain/           # Entities, ValueObjects
├── TorrentClient.Application/      # Managers, UseCases, Ports
├── TorrentClient.Infrastructure/   # Adapters (TCP, HTTP, Disco, Bencode)
├── TorrentClient.Presentation.CLI/ # Consola
└── TorrentClient.Presentation.API/ # API REST + wwwroot
tests/
├── TorrentClient.Domain.Tests/
├── TorrentClient.Application.Tests/
└── TorrentClient.Infrastructure.Tests/
torrent-client-ui/                  # React app
```

---

## Cómo funciona

1. **Parser** lee `.torrent` (Bencode) y extrae InfoHash, announce URL, piezas
2. **Tracker** HTTP devuelve lista de peers (IP:Puerto)
3. **Handshake** TCP (68 bytes) con cada peer
4. **Negociación**: Bitfield → Interested → Unchoke → Request/Piece
5. **Descarga** paralela en bloques de 16KB
6. **Verificación** SHA-1 al completar cada pieza (256KB)
7. **Escritura** en disco con acceso aleatorio (FileStream)

---


## Referencias

- [BitTorrent Protocol (BEP 3)](http://www.bittorrent.org/beps/bep_0003.html)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---


## Licencia

[MIT](LICENSE) © [Thomas Zavalia]

---

##  Autor

**Thomas Zavalia**

- GitHub: [@ThomasZavalia](https://github.com/ThomasZavalia)
- LinkedIn: [in/thomas-zavalia](https://www.linkedin.com/in/thomas-zavalia/)
- Email: zavaliathomas@gmail.com

---
