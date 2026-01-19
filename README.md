# SupStick

A .NET MAUI-based mini player application that integrates with Bitcoin testnet3 and IPFS for decentralized media distribution.

## Overview

SupStick is a cross-platform media player and P2FK (Pay-to-Future-Key) message indexer that:
- **Connects directly to Bitcoin testnet3 P2P network** (no RPC server required)
- Real-time transaction monitoring via peer-to-peer protocol
- Parses P2FK messages embedded in Bitcoin transactions
- Downloads and indexes media files from IPFS
- Provides seamless audio and video playback with playlist management
- Works offline without reliance on third-party services
- Supports devices from iPod Watch to PC monitors with full-screen capability

## Features

### Bitcoin testnet3 Integration
- **Direct P2P connection** to Bitcoin testnet3 network
- No Bitcoin node or RPC server required
- Real-time transaction monitoring via network gossip
- Mempool transaction tracking
- P2FK message parsing from transaction data

### P2FK Message Support
- Extracts messages and files from Bitcoin transactions
- Parses P2FK protocol messages using adapted Root.cs logic
- Identifies IPFS links in messages (e.g., `<<IPFS:QmHash\filename.ext>>`)
- Supports both text messages and file attachments

### IPFS Integration
- **Direct IPFS P2P network connection**
- No reliance on HTTP gateways
- Connects to IPFS bootstrap nodes
- Downloads files directly from IPFS peers
- Automatic retry with peer reconnection
- File indexing and local storage

### Media Player
- **Seamless playback** between audio and video tracks
- Full playback controls: Play, Pause, Stop, Next, Previous
- **Playlist management**: Create, edit, and delete playlists
- Mixed audio/video playlists with uninterrupted playback
- Repeat and shuffle modes
- Volume control
- **Full-screen mode** support for all devices
- Progress tracking with seek capability

### Data Management
- Encrypted local SQLite database
- View all indexed files and messages
- Search by address or P2FK handle
- Delete specific items or clear all data
- Block addresses to prevent unwanted content

### Security
- Input sanitization for all user inputs
- Encrypted local data storage
- Address blocking to filter content
- Secure file handling

### User Interface
- **Status Screen**: Latest indexed files/messages and monitoring status
- **Media Player Screen**: Full-featured player with library, queue, and playlist management
- **Search Screen**: Find messages/files by P2FK handle or address
- **Setup Screen**: Configure Bitcoin RPC, manage monitored addresses, and settings

## Platform Support

- ✅ PC (Windows)
- ✅ Mac (macOS via MacCatalyst)
- ✅ Linux (planned)
- ✅ Android
- ✅ iPhone (iOS)
- ✅ iPod Watch (watchOS - UI optimized for small screens)

## Architecture

### Services
- **BitcoinService**: Bitcoin testnet3 RPC client
- **P2FKService**: P2FK message parser
- **IpfsService**: IPFS gateway client with retry logic
- **DataStorageService**: Encrypted local database operations
- **TransactionMonitorService**: Real-time transaction monitoring and indexing
- **MediaPlayerService**: Media library and playlist management

### Models
- **P2FKRoot**: P2FK message structure
- **IndexedItem**: Stored messages and files
- **MediaItem**: Media file metadata
- **Playlist**: Playlist definitions
- **MonitoredAddress**: Addresses to monitor
- **BlockedAddress**: Blocked addresses

### ViewModels (MVVM Pattern)
- **StatusViewModel**: Transaction monitoring status
- **MediaPlayerViewModel**: Media playback and playlist management
- **SearchViewModel**: Search functionality
- **SetupViewModel**: Configuration and settings

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- MAUI workloads installed
- **No Bitcoin node required** - connects directly to P2P network

### Building the Application
```bash
# Restore dependencies
dotnet restore

# Build for specific platform
dotnet build -f net9.0-android    # Android
dotnet build -f net9.0-ios        # iOS
dotnet build -f net9.0-maccatalyst # macOS
dotnet build -f net9.0-windows10.0.19041.0 # Windows
```

### Running the Application
1. Launch the app on your target platform
2. The app will **automatically connect** to Bitcoin testnet3 P2P network
3. Navigate to the Setup screen to:
   - Check connection status
   - Add addresses to monitor
   - Manage blocked addresses
4. Navigate to Status screen and start monitoring

**No RPC configuration required!** The app connects directly to Bitcoin testnet3 peers.

## Usage

### Monitoring Transactions
1. Go to **Status** screen
2. Tap **Start Monitoring** to begin indexing P2FK messages
3. View newly indexed items in real-time
4. Swipe items to delete unwanted content

### Playing Media
1. Go to **Media Player** screen
2. Browse the **Library** tab to see all media files
3. Tap **▶** to play a track or swipe to add to queue
4. Use the **Queue** tab to manage current playlist
5. Create playlists in the **Playlists** tab
6. Use playback controls:
   - **▶** Play / **⏸** Pause / **⏹** Stop
   - **⏮** Previous / **⏭** Next
   - **🔀** Shuffle / **🔁** Repeat
   - **⛶** Full Screen (for video)

### Playlist Management
1. Add tracks to queue from Library
2. Tap **New Playlist** button
3. Enter playlist name and description
4. Tap **Save Playlist**
5. Load playlists by tapping **⏯** icon
6. Playlists seamlessly play audio and video tracks

### Searching
1. Go to **Search** screen
2. Enter Bitcoin address or P2FK handle
3. Tap **Search**
4. View results and swipe to delete

### Blocking Addresses
1. Go to **Setup** screen
2. Swipe a monitored address
3. Or view blocked addresses section
4. Swipe to unblock if needed

## Technical Details

### P2FK Protocol
The application parses P2FK (Pay-to-Future-Key) messages embedded in Bitcoin testnet3 transactions using micro-transactions with specific values (0.00000001, 0.00000546, etc.). Messages are encoded in the payload of Bitcoin addresses.

### Direct P2P Connection
The app connects directly to Bitcoin testnet3 peer-to-peer network using NBitcoin's P2P protocol implementation:
- Discovers and connects to testnet3 peers
- Subscribes to transaction inventory messages
- Downloads transactions directly from peers
- No dependency on external RPC servers or blockchain APIs
- Fully decentralized operation

### IPFS Integration
Files referenced with IPFS hashes are automatically downloaded using **direct P2P connection** to the IPFS network:
- Initializes local IPFS engine with repository
- Connects to IPFS bootstrap nodes
- Downloads files directly from IPFS peers
- No dependency on HTTP gateways (ipfs.io, etc.)
- Fully decentralized file retrieval
- Automatic retry with peer reconnection

### Media Playback
The media player supports:
- Audio formats: MP3, WAV, OGG, M4A, AAC, FLAC, WMA, OPUS
- Video formats: MP4, AVI, MKV, MOV, WMV, FLV, WEBM, M4V, MPG, MPEG

Playlists can contain both audio and video tracks, and playback continues seamlessly between different media types.

### Full-Screen Support
The media player adapts to all screen sizes:
- **Small screens** (iPod Watch): Optimized compact UI
- **Mobile** (iPhone/Android): Touch-optimized controls
- **Tablets**: Enhanced layout
- **Desktop** (PC/Mac): Full-featured interface with keyboard support

## Dependencies

- **Microsoft.Maui.Controls** (9.0.10): Cross-platform UI framework
- **NBitcoin** (7.0.40): Bitcoin library for P2P networking and transaction parsing
- **Newtonsoft.Json** (13.0.3): JSON serialization
- **SQLite-net-pcl** (1.9.172): Local database
- **sqlite-net-sqlcipher** (1.9.172): Encrypted database support
- **Ipfs.Core** (0.97.0): IPFS core library
- **Ipfs.Engine** (0.97.0): IPFS engine for direct P2P
- **Ipfs.Http.Client** (0.97.0): IPFS HTTP client fallback

## Security Considerations

- All user inputs are sanitized to prevent injection attacks
- Local database is encrypted using SQLCipher
- File paths are validated before access
- Address blocking prevents malicious content indexing
- No external service dependencies for core functionality

## License

This project is part of the SupStick repository.

## References

- [Bitcoin Testnet](https://en.bitcoin.it/wiki/Testnet)
- [IPFS](https://ipfs.io/)
- [P2FK Protocol Reference](https://github.com/embiimob/Sup/blob/9045206c2159328c54eefed49bbac262f190aa0d/P2FK/contracts/Root.cs)
- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)