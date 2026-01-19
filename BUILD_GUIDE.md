# Building SupStick on Windows 11 Home - Complete Guide

This guide will walk you through building SupStick on a fresh Windows 11 Home installation, from nothing to a fully working application on Windows PC, Android emulator, and iOS simulator (via Mac connection).

## Table of Contents
1. [Prerequisites Installation](#prerequisites-installation)
2. [Building for Windows PC](#building-for-windows-pc)
3. [Testing on Android Emulator](#testing-on-android-emulator)
4. [Testing on iPhone (requires Mac)](#testing-on-iphone-requires-mac)
5. [Troubleshooting](#troubleshooting)

---

## Prerequisites Installation

### Step 1: Install Visual Studio 2022 Community Edition

1. **Download Visual Studio 2022**
   - Go to: https://visualstudio.microsoft.com/downloads/
   - Click "Free download" under "Community 2022"
   - Run the downloaded `VisualStudioSetup.exe`

2. **Select Workloads During Installation**
   
   In the Visual Studio Installer, select these workloads:
   
   ✅ **.NET Multi-platform App UI development** (this includes .NET MAUI)
   - This will automatically include:
     - .NET 9.0 SDK
     - Android SDK
     - Android emulators
     - MAUI templates and tools
   
   ✅ **Mobile development with .NET** (for Android/iOS)
   
   ✅ **Universal Windows Platform development** (optional, for UWP)

3. **Individual Components to Verify**
   
   Switch to the "Individual components" tab and ensure these are selected:
   - .NET 9.0 Runtime
   - .NET SDK
   - Android SDK setup (API 34 recommended)
   - Android SDK build-tools
   - Android emulator
   - Intel HAXM (if you have an Intel CPU)
   
   Click **Install** (this will take 30-60 minutes)

4. **Restart Your Computer** after installation completes

### Step 2: Verify .NET Installation

1. Open **Command Prompt** (Win + R, type `cmd`, press Enter)

2. Check .NET version:
   ```cmd
   dotnet --version
   ```
   Expected output: `9.0.100` or similar

3. List installed workloads:
   ```cmd
   dotnet workload list
   ```
   You should see `maui` and related workloads listed

4. If MAUI is not listed, install it manually:
   ```cmd
   dotnet workload install maui
   ```

### Step 3: Install Git (for cloning the repository)

1. Download Git from: https://git-scm.com/download/win
2. Run the installer with default options
3. Verify installation:
   ```cmd
   git --version
   ```

---

## Building for Windows PC

### Step 1: Clone the Repository

1. Open **Command Prompt** or **PowerShell**

2. Navigate to where you want the project:
   ```cmd
   cd C:\Users\YourUsername\Documents
   ```

3. Clone the repository:
   ```cmd
   git clone https://github.com/embiimob/SupStick.git
   cd SupStick
   ```

### Step 2: Restore Dependencies

```cmd
dotnet restore
```

This will download all NuGet packages (NBitcoin, IPFS libraries, SQLite, etc.)

### Step 3: Build for Windows

```cmd
dotnet build -f net9.0-windows10.0.19041.0 -c Debug
```

**What this does:**
- `-f net9.0-windows10.0.19041.0`: Target framework for Windows
- `-c Debug`: Build in Debug configuration (use `-c Release` for optimized build)

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Step 4: Run the Application on Windows

**Option A: Run from Command Line**
```cmd
dotnet run -f net9.0-windows10.0.19041.0
```

**Option B: Run from Visual Studio**
1. Open `SupStick.sln` in Visual Studio 2022
2. In the toolbar, select:
   - Configuration: `Debug`
   - Platform: `Any CPU`
   - Target: `Windows Machine`
3. Press **F5** or click the green "Start" button

**Option C: Run the compiled executable**
```cmd
cd bin\Debug\net9.0-windows10.0.19041.0\win10-x64
SupStick.exe
```

### Step 5: Test the Application on Windows

Once the app launches:

1. **Check P2P Connections**
   - Navigate to the **Setup** tab
   - Click "Check Connection Status"
   - Wait for Bitcoin testnet3 and IPFS to connect (green checkmarks)
   - This may take 30-60 seconds for initial peer discovery

2. **Start Monitoring**
   - Navigate to the **Status** tab
   - Click "Start Monitoring"
   - The app will begin monitoring Bitcoin testnet3 transactions

3. **Test Media Player**
   - Navigate to the **Player** tab
   - You'll need some media files indexed first
   - For testing, you can manually add media files to the library

---

## Testing on Android Emulator

### Step 1: Set Up Android Emulator

1. **Open Android Device Manager**
   
   In Visual Studio 2022:
   - Go to **Tools** → **Android** → **Android Device Manager**
   
   Or from Command Prompt:
   ```cmd
   "%LOCALAPPDATA%\Android\Sdk\tools\bin\avdmanager.bat" list avd
   ```

2. **Create a New Emulator** (if none exists)
   
   In Android Device Manager:
   - Click **+ New Device**
   - Choose a device (recommended: **Pixel 5**)
   - Select API level: **API 34 (Android 14.0)**
   - Click **Create**

3. **Start the Emulator**
   - Click the **Play** button next to your device
   - Wait for Android to boot (2-3 minutes first time)

### Step 2: Build for Android

1. **Build the APK**
   ```cmd
   dotnet build -f net9.0-android -c Debug
   ```

2. **If you get errors about Android SDK**, set the path:
   ```cmd
   set ANDROID_HOME=%LOCALAPPDATA%\Android\Sdk
   dotnet build -f net9.0-android -c Debug
   ```

### Step 3: Deploy to Android Emulator

**Option A: Using Visual Studio**
1. Open `SupStick.sln` in Visual Studio
2. In the toolbar, select:
   - Configuration: `Debug`
   - Platform: `Any CPU`
   - Target: Your Android emulator from the dropdown
3. Press **F5** to deploy and run

**Option B: Using Command Line**
1. List connected devices:
   ```cmd
   "%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe" devices
   ```
   You should see your emulator listed

2. Install the APK:
   ```cmd
   "%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe" install -r bin\Debug\net9.0-android\com.embiimob.supstick-Signed.apk
   ```

3. Launch the app:
   ```cmd
   "%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe" shell am start -n com.embiimob.supstick/.MainActivity
   ```

### Step 4: Test on Android Emulator

1. The app should launch on the emulator
2. **Grant Permissions** if prompted (storage, network)
3. Navigate through the tabs (Status, Player, Search, Setup)
4. Test the same features as Windows (P2P connections, monitoring)

**Note:** The emulator has internet access through your PC's connection, so P2P connectivity should work.

---

## Testing on iPhone (Requires Mac)

**Important:** Building iOS apps on Windows is not directly supported by .NET MAUI. You need a Mac with Xcode for iOS builds.

### Option 1: Hot Restart (Limited Testing)

Visual Studio on Windows supports "Hot Restart" which allows limited iOS testing without a full Mac build:

1. **Requirements:**
   - A Mac on the same network
   - Xcode installed on the Mac
   - Mac configured for remote login

2. **Setup:**
   - In Visual Studio: **Tools** → **iOS** → **Pair to Mac**
   - Follow the wizard to connect to your Mac
   - Enter Mac credentials when prompted

3. **Deploy with Hot Restart:**
   - Select an iOS simulator target
   - Press **F5**
   - Hot Restart will deploy to the Mac's simulator

**Limitations of Hot Restart:**
- Can't use all device features
- Limited debugging capabilities
- Not suitable for production testing

### Option 2: Full Build on Mac (Recommended)

**On your Mac:**

1. **Install Prerequisites**
   ```bash
   # Install Xcode from App Store
   xcode-select --install
   
   # Install .NET SDK
   curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0
   
   # Install MAUI workload
   dotnet workload install maui
   ```

2. **Clone Repository on Mac**
   ```bash
   git clone https://github.com/embiimob/SupStick.git
   cd SupStick
   ```

3. **Build for iOS Simulator**
   ```bash
   dotnet build -f net9.0-ios -c Debug
   ```

4. **Run on iOS Simulator**
   ```bash
   dotnet run -f net9.0-ios
   ```
   
   Or use Visual Studio for Mac:
   - Open `SupStick.sln`
   - Select iOS Simulator
   - Press Run

5. **Test on Physical iPhone**
   
   For physical device testing:
   - Connect iPhone via USB
   - Trust the computer on iPhone
   - Select the device in Visual Studio
   - You'll need an Apple Developer account (free or paid)
   - May need to provision the device

### Option 3: Remote Mac Build from Windows

If you have a Mac available but want to develop on Windows:

1. **Pair Visual Studio to Mac:**
   - Tools → iOS → Pair to Mac
   - Enter Mac IP address and credentials

2. **Select iOS Target:**
   - In the target dropdown, select an iOS simulator or device
   - Visual Studio will build on the Mac remotely

3. **Deploy and Test:**
   - Press F5
   - The app builds on Mac and runs in the iOS simulator
   - Debugging works from Windows

---

## Troubleshooting

### Common Build Issues

**1. "MAUI workload not found"**
```cmd
dotnet workload install maui
dotnet workload restore
```

**2. "Android SDK not found"**
```cmd
set ANDROID_HOME=%LOCALAPPDATA%\Android\Sdk
```

**3. "Unable to find package Ipfs.Engine"**
```cmd
dotnet nuget add source https://api.nuget.org/v3/index.json
dotnet restore --force
```

**4. Build errors with NBitcoin or IPFS**
- Make sure you're connected to the internet during first build
- NuGet needs to download packages
- Check `dotnet restore` completes successfully

### Runtime Issues

**1. "Cannot connect to Bitcoin P2P network"**
- Check your firewall allows outbound connections
- Bitcoin testnet3 needs ports 18333 open
- May take 30-60 seconds to discover peers

**2. "IPFS initialization failed"**
- IPFS creates a local repository in `%LOCALAPPDATA%`
- First run may take longer to initialize
- Check disk space (IPFS needs ~100MB minimum)

**3. "Database error" or "SQLite error"**
- The app needs write access to `%LOCALAPPDATA%\supstick.db3`
- Check folder permissions
- Try running as administrator once to create initial DB

**4. Android emulator won't start**
- Check if Hyper-V is enabled (required for Windows emulators)
- Or disable Hyper-V and use Intel HAXM
- **Enable Hyper-V:**
  ```cmd
  DISM /Online /Enable-Feature /All /FeatureName:Microsoft-Hyper-V
  ```
  Then restart

### Performance Tips

**1. Faster Android Emulator**
- Use x86_64 system image (faster than ARM)
- Allocate more RAM in AVD settings (4GB+)
- Enable "Host GPU" in emulator settings

**2. Faster Builds**
- Use `-c Release` for optimized builds
- Close other applications during build
- Use SSD for project location

**3. Reduce P2P Connection Time**
- The app caches connected peers
- Subsequent launches connect faster
- Keep app running to maintain peer connections

---

## Quick Reference Commands

### Windows PC
```cmd
# Clone
git clone https://github.com/embiimob/SupStick.git
cd SupStick

# Restore
dotnet restore

# Build
dotnet build -f net9.0-windows10.0.19041.0 -c Debug

# Run
dotnet run -f net9.0-windows10.0.19041.0
```

### Android
```cmd
# Build
dotnet build -f net9.0-android -c Debug

# Deploy
"%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe" install -r bin\Debug\net9.0-android\com.embiimob.supstick-Signed.apk

# Launch
"%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe" shell am start -n com.embiimob.supstick/.MainActivity
```

### iOS (on Mac)
```bash
# Build
dotnet build -f net9.0-ios -c Debug

# Run
dotnet run -f net9.0-ios
```

---

## Additional Resources

- **.NET MAUI Documentation:** https://docs.microsoft.com/dotnet/maui/
- **Visual Studio MAUI Setup:** https://docs.microsoft.com/dotnet/maui/get-started/installation
- **Android Emulator Guide:** https://developer.android.com/studio/run/emulator
- **Pair to Mac Guide:** https://docs.microsoft.com/xamarin/ios/get-started/installation/windows/connecting-to-mac

---

## Summary

1. ✅ Install Visual Studio 2022 with .NET MAUI workload
2. ✅ Clone repository and restore packages
3. ✅ Build and run on Windows with `dotnet run -f net9.0-windows10.0.19041.0`
4. ✅ Test on Android emulator via Visual Studio or ADB
5. ✅ For iOS, use a Mac or pair Visual Studio to remote Mac

**First-time setup:** ~1-2 hours (mostly downloading Visual Studio and components)
**Subsequent builds:** ~30 seconds to 2 minutes

Need help? Open an issue at: https://github.com/embiimob/SupStick/issues
