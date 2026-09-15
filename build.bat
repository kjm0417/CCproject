@echo off
chcp 65001 > nul
set PROJECT_PATH=%~dp0

:: ==========================================
:: 1. 환경 변수 설정
:: ==========================================
set UNITY_EXE="D:\UnityEditor\6000.5.5f1\Editor\Unity.exe"
set FIREBASE_APP_ID="1:165910021223:android:933ad4bebfb24e82bd00c2"
set WEBHOOK_URL="https://discord.com/api/webhooks/1549406573892206632/RaaQlEbWBi5uipcyA3uiHVcFbZ7z4zEmj5dZfRkQEOTB5kRANIaY-xdhIdBkNjXswpwY"
set DOWNLOAD_URL="https://appdistribution.firebase.google.com"

echo ===================================================
echo [1/3] 유니티 안드로이드 APK 백그라운드 빌드 시작...
echo ===================================================

%UNITY_EXE% -quit -batchmode -projectPath "%PROJECT_PATH%" -executeMethod BuildAutomation.BuildAndroidAPK -logFile build_log.txt

if %ERRORLEVEL% NEQ 0 (
    echo [에러] 유니티 빌드가 실패했습니다. build_log.txt를 확인하세요.
    curl -H "Content-Type: application/json" -X POST -d "{\"embeds\": [{\"title\": \"❌ 유니티 빌드 실패\",\"description\": \"로컬 컴퓨터에서 컴파일 중 에러가 발생했습니다.\n`build_log.txt` 파일을 확인하세요.\",\"color\": 15158332}]}" %WEBHOOK_URL%
    pause
    exit /b %ERRORLEVEL%
)

echo ===================================================
echo [2/3] Firebase App Distribution 모바일 전송 시작...
echo ===================================================

call firebase appdistribution:distribute "Build/Android/Game.apk" --app %FIREBASE_APP_ID% --groups "testers"

if %ERRORLEVEL% NEQ 0 (
    echo [에러] Firebase 업로드에 실패했습니다.
    curl -H "Content-Type: application/json" -X POST -d "{\"embeds\": [{\"title\": \"⚠️ Firebase 배포 실패\",\"description\": \"유니티 빌드는 성공했으나, Firebase 서버 전송 중 오류가 발생했습니다.\",\"color\": 15105570}]}" %WEBHOOK_URL%
    pause
    exit /b %ERRORLEVEL%
)

echo ===================================================
echo [3/3] 디스코드 깔끔한 카드 알림 전송...
echo ===================================================

set JSON_PAYLOAD={\"embeds\": [{\"title\": \"🚀 안드로이드 APK 빌드 및 배포 완료\",\"description\": \"테스트용 신규 빌드가 성공적으로 추출되어 Firebase에 업로드되었습니다.\n스마트폰 및 PC에서 아래 다운로드 버튼을 눌러 설치하세요.\",\"color\": 3066993,\"fields\": [{\"name\": \"📦 파일명\",\"value\": \"`Game.apk` (Development Build)\",\"inline\": true},{\"name\": \"🔗 다운로드 링크 (클릭)\",\"value\": \"[▶️ APK 다운로드 페이지로 이동](%DOWNLOAD_URL%)\",\"inline\": false}]}]}

curl -H "Content-Type: application/json" -X POST -d "%JSON_PAYLOAD%" %WEBHOOK_URL%

echo ===================================================
echo 모든 자동화 프로세스가 성공적으로 완료되었습니다!
echo ===================================================
pause
