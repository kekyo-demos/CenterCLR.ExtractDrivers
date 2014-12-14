@rem "http://technet.microsoft.com/ja-jp/library/dn621890.aspx"

set VhdxMountedPath=G:\
set MountFolderPath=D:\Images\windows

dism /Unmount-Image /MountDir:"%MountFolderPath%" /discard

del D:\Images\win8vs.wim
dism /Capture-Image /CaptureDir:%VhdxMountedPath% /ImageFile:D:\Images\win8vs.wim /Name:"Win8VS" /NoRpFix

mkdir D:\Images\windows
dism /Mount-Image /ImageFile:D:\Images\win8vs.wim /Index:1 /MountDir:"%MountFolderPath%"

dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\prnms001\prnms001.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\isstrtc\isstrtc.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\pmic\pmic.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\TXEI\TXEI.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\iaiouart\iaiouart.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\intelaudx86\intelaudx86.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\iwdbusx86\iwdbusx86.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\BcmGnssBus\BcmGnssBus.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\BcmGnssLocationSensor\BcmGnssLocationSensor.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\RtkUart\RtkUart.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\kmdfsamples\kmdfsamples.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\kxfusion\kxfusion.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\rtii2sac\rtii2sac.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\kxspb\kxspb.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\camera\camera.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\gc2235\gc2235.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\ov5648\ov5648.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\netrtwlans\netrtwlans.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\SpbSamples\SpbSamples.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\IntelBatteryManagement\IntelBatteryManagement.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\dptf\dptf.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\iaiogpioe\iaiogpioe.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\iaiogpiovirtual\iaiogpiovirtual.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\igdlh\igdlh.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\iaioi2ce\iaioi2ce.inf"
dism.exe /Add-Driver /Image:"%MountFolderPath%" /Driver:"DriverStore\MBI\MBI.inf"

Dism /Get-Drivers /Image:"%MountFolderPath%"

dism /Optimize-Image /Image:"%MountFolderPath%" /WIMBoot
dism /Unmount-Image /MountDir:"%MountFolderPath%" /Commit

del D:\Images\install.wim
dism /Export-Image /WIMBoot /SourceImageFile:D:\Images\win8vs.wim /SourceIndex:1 /DestinationImageFile:D:\Images\install.wim
