# This is Windows 8.1 WIMBoot installation examples.

## Testing with...
* Target is Teclast X89win (Atom-Z3736F based 7.9-retina LCD tablet PC, smaller eMMC storage 32GB, Enable only 32bit UEFI boot).
* Windows 8.1 enterprise edition 32bit, in Office 2013 PP (Only WXPOO), Visual Studio 2013 Update4, and JetBrains Resharper 9 Ultimate :-)

## Construct template image in Hyper-V.
* Caution:
 * Enable "Administrator" account, and delete first defined user account.
* Install, and update any application and windows.
* Finally, execute sysprep with administrative mode.
 * "C:\Windows\System32\Sysprep.exe /generalize /oobe /shutdown"
* Shutdown guest OS automatically.

## Create WIMBoot image.
* Mount Hyper-V guest OS image (*.vhdx).
* Run "CenterCLR.ExtractDrivers.exe".
 * Extract drivers and generate dism script (template.bat) from vdhx image.

![Extracted drivers.](https://raw.githubusercontent.com/kekyo/CenterCLR.ExtractDrivers/master/WimConstcutionSample/extracted.png)

* Run make_master.bat
 * Caution: You must customize same path in script.
 * Caution: Dism script replace with template.bat.

![Applying drivers with dism.](https://raw.githubusercontent.com/kekyo/CenterCLR.ExtractDrivers/master/WimConstcutionSample/dism-add-driver.png)

* Finally, "install.wim" is constructed.

## Boot target from Windows 8.1 Master DVD
* Keyboard/Mouse selection, next.
* Custom operation, "Start command prompt."
* e:
* Execute "make_target.bat".
* Reboot.

![Teclast X89win is great 7.9 inch tablet PC with retina resolution (2048x1536).](https://raw.githubusercontent.com/kekyo/CenterCLR.ExtractDrivers/master/WimConstcutionSample/x89win_real.png)

