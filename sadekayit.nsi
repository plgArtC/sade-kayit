Unicode True
!include "MUI2.nsh"
Name "SadeKayıt"
OutFile "SadeKayit_Kurulum.exe"
InstallDir "$LOCALAPPDATA\SadeKayit"
RequestExecutionLevel user
SetCompressor /SOLID lzma
!define MUI_ICON "sadekayit.ico"
!define MUI_UNICON "sadekayit.ico"
!define MUI_FINISHPAGE_RUN "$INSTDIR\SadeKayit.exe"
!define MUI_FINISHPAGE_RUN_TEXT "SadeKayıt'ı şimdi aç"
!insertmacro MUI_PAGE_LICENSE "LICENSE"
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Turkish"
Section "Kur"
  SetOutPath "$INSTDIR"
  File "SadeKayit.exe"
  File "ffmpeg.exe"
  File "sadekayit.ico"
  File "LICENSE"
  File "THIRD_PARTY_NOTICES.md"
  CreateShortcut "$DESKTOP\SadeKayıt.lnk" "$INSTDIR\SadeKayit.exe" "" "$INSTDIR\sadekayit.ico"
  CreateDirectory "$SMPROGRAMS\SadeKayıt"
  CreateShortcut "$SMPROGRAMS\SadeKayıt\SadeKayıt.lnk" "$INSTDIR\SadeKayit.exe" "" "$INSTDIR\sadekayit.ico"
  WriteUninstaller "$INSTDIR\Kaldir.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SadeKayit" "DisplayName" "SadeKayıt"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SadeKayit" "UninstallString" '"$INSTDIR\Kaldir.exe"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SadeKayit" "Publisher" "Emre Bulut (plgArtC)"
SectionEnd
Section "Uninstall"
  Delete "$DESKTOP\SadeKayıt.lnk"
  Delete "$SMPROGRAMS\SadeKayıt\SadeKayıt.lnk"
  RMDir "$SMPROGRAMS\SadeKayıt"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SadeKayit"
SectionEnd
