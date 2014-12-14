# CenterCLR.ExtractDrivers
## Binaries
* https://raw.githubusercontent.com/kekyo/CenterCLR.ExtractDrivers/master/CenterCLR.ExtractDrivers-1.0.0.0.zip

## What is this?
* CenterCLR.ExtractDrivers is a tool of extract driver packages from installed windows 8.1 environment.
 * Simple command line tool.
 * Automatically parse windows driver information files (*.inf), calculate require additional driver files.
 * Copy out from installed files to local folders.
 * Auto generate dism.exe sample script (You can use WIMBoot-able operation).

## Example: Windows 8.1 with WIMBoot installation in small resource system.
 * Extract driver, and apply plane windows 8.1 japanese system (Teclast X89win), Office 2013 (WXPOO), Visual Studio 2013 Update4, JetBrains Resharper 9 Ultimate, IN eMMC 32GB STORAGE, CURRENT FREE SPACE IS 10GB over!!
![Extract driver, and apply plane windows 8.1 japanese system (Teclast X89win)](https://raw.githubusercontent.com/kekyo/CenterCLR.ExtractDrivers/master/WimConstcutionSample/x89win.png)

## How to use
* Prerequisities: Windows 8.1 complete running system.
 * Can handle Hyper-V imaged guest OS (In Vhdx format).
 * Vhdx'ed image mount working machine.
* Run cmd.exe in administrative.
* Run CenterCLR.ExtractDrivers.exe

```
CenterCLR.ExtractDrivers.exe D:\Images\x89win_windows\Windows
```

* Done extract drivers!
* Drivers stored to "DriverStore" sub folders.
* Extract from "D:\Images\x89win_windows\Windows", this folder must contains "installed Windows" folder.
 * Mounted from Vhdx.
 * Mounted from dism.
 * Referenced from real system via network.

* Embedded help:
```
CenterCLR.ExtractDrivers - Extract driver files from installed windows.
Copyright (c) Kouji Matsui, All rights reserved.

usage: CenterCLR.ExtractDrivers.exe <pickup windows folder path> [<inf search pattern> [<output folder path>]]
   ex: CenterCLR.ExtractDrivers.exe C:\Windows oem*.inf DriverStore
```

* Enjoy!
