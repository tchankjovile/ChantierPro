@echo off
chcp 65001 >nul
title ChantierPro - Assistant de Lancement (.NET MAUI)
color 1F

echo ============================================================================
echo               CHANTIERPRO - ASSISTANT DE LANCEMENT AUTOMATIQUE
echo ============================================================================
echo.

cd /d "%~dp0"

:: 1. Verifier que le SDK .NET est bien installe
echo [1/4] Verification du SDK .NET installe...
dotnet --list-sdks >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    color 4F
    echo.
    echo [ERREUR] Aucun SDK .NET n'a ete detecte sur cette machine.
    echo Installez le SDK .NET 9 depuis : https://dotnet.microsoft.com/download/dotnet/9.0
    echo.
    pause
    exit /b 1
)

for /f "tokens=1 delims=." %%a in ('dotnet --version') do set SDK_MAJOR=%%a
set DOTNET_VER=%SDK_MAJOR%.0
echo       SDK detecte : .NET %DOTNET_VER%
echo.

:: 2. Nettoyer les anciens dossiers bin et obj (evite les conflits entre PC)
echo [2/4] Nettoyage des caches de compilation (bin / obj)...
for /d /r . %%d in (bin,obj) do @if exist "%%d" rd /s /q "%%d" 2>nul
echo       Caches nettoyes avec succes.
echo.

:: 3. Verifier si le workload MAUI est installe
echo [3/4] Verification de la charge de travail .NET MAUI...
dotnet workload list | findstr /i "maui" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo       La charge de travail MAUI n'est pas encore installee sur ce PC.
    echo       Installation automatique en cours (cela peut prendre quelques minutes)...
    dotnet workload install maui
    if %ERRORLEVEL% NEQ 0 (
        echo.
        echo [ATTENTION] L'installation de MAUI necessite peut-etre les droits Administrateur.
        echo Faites un clic droit sur "Lancer-ChantierPro.bat" puis "Executer en tant qu'administrateur".
        pause
        exit /b 1
    )
) else (
    echo       Workload MAUI deja installe.
)
echo.

:: 4. Menu de lancement
:MENU
echo ============================================================================
echo   QUE VOULEZ-VOUS LANCER ? (SDK cible : net%DOTNET_VER%)
echo ============================================================================
echo   [1] Lancer l'application sur WINDOWS (Recommande - sans emulateur)
echo   [2] Executer les TESTS UNITAIRES (Verifie les regles RG-01 a RG-08)
echo   [3] Lancer l'application sur ANDROID (Necessite un emulateur ou tel USB)
echo   [4] Quitter
echo ============================================================================
set /p CHOIX="Votre choix (1, 2, 3 ou 4) : "

if "%CHOIX%"=="1" goto RUN_WIN
if "%CHOIX%"=="2" goto RUN_TEST
if "%CHOIX%"=="3" goto RUN_ANDROID
if "%CHOIX%"=="4" exit /b 0
goto MENU

:RUN_WIN
echo.
echo [4/4] Compilation et demarrage de ChantierPro sur Windows...
dotnet build src\ChantierPro.App\ChantierPro.App.csproj -t:Run -f net%DOTNET_VER%-windows10.0.19041.0 -p:ChantierProDotNet=%DOTNET_VER%
echo.
pause
goto MENU

:RUN_TEST
echo.
echo [4/4] Execution des tests unitaires xUnit...
dotnet test tests\ChantierPro.Tests\ChantierPro.Tests.csproj -p:ChantierProDotNet=%DOTNET_VER%
echo.
pause
goto MENU

:RUN_ANDROID
echo.
echo [4/4] Compilation et demarrage de ChantierPro sur Android...
dotnet build src\ChantierPro.App\ChantierPro.App.csproj -t:Run -f net%DOTNET_VER%-android -p:ChantierProDotNet=%DOTNET_VER%
echo.
pause
goto MENU
