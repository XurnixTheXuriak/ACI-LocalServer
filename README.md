# Requirements
- [ASP.NET Core Runtime 8.0 Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [RPCS3](https://rpcs3.net/)
- [PS3 Firmware](https://www.playstation.com/en-us/support/hardware/ps3/system-software/)
- A legitimate copy of Ace Combat: Infinity v2.11 along with its license file

# Getting Started
0. Set up RPCS3: https://rpcs3.net/quickstart
1. In Releases, download the LocalServer_*.zip file for your OS and the .yml patch file.
2. Place `imported_patch.yml` in your RPCS3/patches folder.
3. If you haven't, create an RPCN account and sign in to RPCN.
4. Open RPCS3 and go to Manager -> Game Patches -> ACE COMBAT INFINITY and enable the patch.
5. In your RPCS3 game library, right-click the game and select `Create custom configuration`
6. Go to Settings -> Network -> paste `dev-wind.siliconstudio.co.jp=127.0.0.1` in IP/Hosts switches.
7. Make sure Network Status is set to `Connected` and PSN Status is `RPCN`.
8. Click `Apply` and `Save custom configuration`.
9. Extract the server .zip file you downloaded.
10.
    (Windows) Run `LocalServer.exe`. 
    (Linux) Run `sudo ./LocalServer`
12. Launch the game and keep pressing the `X` button past the title screen. The screen will go black for a second and then load to the main menu.

# Instructions
# Linux

## Step 1 - Clone the repository
```
git clone https://github.com/XurnixTheXuriak/ACI-LocalServer.git
```
## Step 2 - Install .NET 8.0 SDK
You need to have the .NET SDK installed for you to build this application

Debian/Ubuntu/Pop!_OS
```
sudo apt update
sudo apt install dotnet-sdk-8.0
```
Fedora
```
sudo dnf install dotnet-sdk-8.0
```
Arch/SteamOS
```
sudo pacman -S dotnet-sdk-8.0
```

## Step 3 - Building
Build a self-contained version:
```
dotnet publish -c Release -r linux-x64  --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Step 4 - Running
Once the build process is finished, run:
```
sudo ./bin/Release/net8.0/linux-x64/publish/LocalServer
```
Note for root: On Linux, Port 80 (HTTP) and Port 443 (HTTPS) are restricted to root-only. Make sure to **not** run applications you do not trust as root.

## Step 5 - Installing (Optional)
If you want to access Local Server globally on your Linux system.

### Step 1 - Create a directory in /opt
```
sudo mkdir /opt/ACI-LocalServer && sudo mkdir /opt/ACI-LocalServer/bin
```
### Step 2 - Copy the files over to /opt/ACI-LocalServer/bin
```
sudo cp -r ./bin/Release/net8.0/linux-x64/publish/* /opt/ACI-LocalServer/bin
```
### Step 3 - Updating File Ownership
You have to update the file ownership of the copied files. Otherwise once you run it. It will present you with ``System.IO.IOException: Permission denied``

To fix this, please run this in your Terminal as root (sudo):
```
sudo chown -R $USER:$USER /opt/ACI-LocalServer/bin
```

## Step 4 - Dealing with the pesky root requirement
Remember when i said this?
> Note for root: On Linux, Port 80 (HTTP) and Port 443 (HTTPS) are restricted to root-only. Make sure to **not** run applications you do not trust as root.

Turns out you can get around this limitation with a utility called ``setcap``, simply run this command in your Terminal:
```
sudo setcap 'cap_net_bind_service=+ep' /opt/ACI-LocalServer/bin/LocalServer
```
Note: Rerunning the build process and following the above steps might remove the capability for it to run without root. Simply rerun ``sudo setcap 'cap_net_bind_service=+ep' /opt/ACI-LocalServer/bin/LocalServer``.

## Step 5 - Symlinking & Running 
For Local Server to be globally accessible, we need to do what is called "Symlinking", think of it as a Mail Forwarding Address in the real world.

To Symlink, Run this in the Terminal:
```
sudo ln -s /opt/ACI-LocalServer/bin/LocalServer /usr/bin/LocalServer
```
And finally... run it.
```
LocalServer
```

If you see this output:
```
[2026-05-09_13-40-05] [Info] [HTTP 127.0.0.1:80] Started listening.
[2026-05-09_13-40-05] [Info] [HTTP 127.0.0.1:443] Started listening.
[2026-05-09_13-40-05] [Info] All listeners started. Server online.
```
Congratulations! You made it! Now go splash some bandits in Ace Combat Infinity once more.

# Windows 
Note: I am on Linux, I cannot easily verify this. Please look up a tutorial.

## Step 1 - Clone the repository
```
git clone https://github.com/XurnixTheXuriak/ACI-LocalServer.git
```
## Step 2 - Install .NET SDK 8.0
You can download it [here](https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.420/dotnet-sdk-8.0.420-win-x64.exe)
Follow the instructions in the installer.

## Step 3 - Install Visual Studio (Optional)
You can download the installer [here](https://visualstudio.microsoft.com/downloads/)
Please ensure to select ".NET desktop development" & “ASP.NET and web development” in the installer in Components

## Step 4a - Building & Running (Visual Studio)
Navigate to the directory which you had cloned ACI-LocalServer and click "LocalServer.sln"
Once it is loaded up, press Ctrl+Shift+B to start building the server application.

Once finished, you can find it the project's ``bin`` folder. Then simply double click LocalServer.exe

## Step 4b - Building & Running (CLI/Command Prompt)
Navigate to the directory which you had cloned ACI-LocalServer

Then in your Command Prompt, run:
```
dotnet publish -c Release -r win-x64 --self-contained true
```
Once finished, you can find it the project's ``bin`` folder. Then simply double click LocalServer.exe

If you see this output:
```
[2026-05-09_13-40-05] [Info] [HTTP 127.0.0.1:80] Started listening.
[2026-05-09_13-40-05] [Info] [HTTP 127.0.0.1:443] Started listening.
[2026-05-09_13-40-05] [Info] All listeners started. Server online.
```
Congratulations! You made it! Now go splash some bandits in Ace Combat Infinity once more.

# Notes
- This server only listens and sends OK responses. It does not save game data nor send game data. It is not possible to save data with this server as it is.
