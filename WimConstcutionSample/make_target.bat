@rem "http://technet.microsoft.com/ja-jp/library/dn621890.aspx"

diskpart /s diskpart_target.txt

mkdir "R:\Images"
copy e:\install.wim "R:\Images"

mkdir "R:\Recovery\WindowsRE"
xcopy /h e:\Winre.wim "R:\Recovery\WindowsRE"

dir "R:\Images"

mkdir C:\Recycler\Scratch
dism /Apply-Image /ImageFile:"R:\Images\install.wim" /ApplyDir:C: /Index:1 /WIMBoot /ScratchDir:C:\Recycler\Scratch

bcdboot c:\windows

c:\Windows\System32\Reagentc /SetREImage /Path R:\Recovery\WindowsRE /Target c:\Windows
