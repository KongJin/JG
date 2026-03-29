# Firebase Hosting

## 프로젝트 정보

- Firebase 프로젝트: `projectsd-51439`
- 라이브 URL: `https://projectsd-51439.web.app`
- 빌드 출력 폴더: `Build/WebGL`
- 설정 파일: `firebase.json`, `.firebaserc`

## 배포 명령어

| 용도 | 명령어 |
|---|---|
| 테스트 배포 (임시 URL, 7일 만료) | `firebase.cmd hosting:channel:deploy qa` |
| 라이브 배포 (고정 URL) | `firebase.cmd deploy --only hosting` |

## 원클릭 스크립트

PowerShell 스크립트:

```powershell
.\tools\build-and-deploy-webgl.ps1
```

기본 동작:
- Unity MCP `POST /build/webgl` 호출
- `Build/WebGL` 출력
- Firebase preview channel `qa` 배포

옵션 예시:

```powershell
.\tools\build-and-deploy-webgl.ps1 -Channel qa
.\tools\build-and-deploy-webgl.ps1 -Live
.\tools\build-and-deploy-webgl.ps1 -SkipBuild
.\tools\build-and-deploy-webgl.ps1 -BaseUrl http://127.0.0.1:52675
```

## 참고

- 테스트 배포 시 `https://projectsd-51439--qa-xxxxx.web.app` 형태의 임시 URL이 생성된다
- 라이브 배포는 `https://projectsd-51439.web.app`에 덮어쓴다
- PowerShell에서는 `.ps1` 실행 정책 문제로 `firebase` 대신 `firebase.cmd`를 사용한다
- Unity MCP 포트는 `ProjectSettings/UnityMcpPort.txt`에서 자동으로 읽는다
