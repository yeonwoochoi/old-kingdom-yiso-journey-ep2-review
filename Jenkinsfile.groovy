def PROJECT_NAME = "old-kingdom-yiso-journey-ep2"
def CUSTOM_WORKSPACE = "C:\\Jenkins\\Unity_Projects"
def UNITY_VERSION = "6000.0.62f1"
def UNITY_INSTALLATION = "C:\\Program Files\\Unity\\Hub\\Editor\\${UNITY_VERSION}\\Editor"

pipeline{
    environment{
        PROJECT_PATH = "${CUSTOM_WORKSPACE}\\${PROJECT_NAME}"
    }

    agent{
        node {
            label ""
            customWorkspace "${CUSTOM_WORKSPACE}\\${PROJECT_NAME}"
        }
    }

    stages{
        stage('Build Windows'){
            when{expression {BUILD_WINDOWS == 'true'}}
            steps{
                script{
                    withEnv(["UNITY_PATH=${UNITY_INSTALLATION}"]){
                        bat '''
                        echo "=== 1. 기존 빌드 폴더 및 파일 청소 ==="
                        if exist "Builds\\Windows" rmdir /s /q "Builds\\Windows"
                        if exist "Builds\\Windows.zip" del /q "Builds\\Windows.zip"
                        
                        echo "=== 2. 유니티 빌드 시작 ==="
                        "%UNITY_PATH%\\Unity.exe" -quit -batchmode -projectPath . -executeMethod BuildScript.BuildWindows -logFile -
                        
                        echo "=== 3. 버전 관리 (History 폴더에 백업) ==="
                        if not exist "Builds\\History" mkdir "Builds\\History"

                        
                        :: Windows.zip이 성공적으로 만들어졌다면, 번호를 붙여서 복사한다.
                        if exist "Builds\\Windows.zip" (
                            copy "Builds\\Windows.zip" "Builds\\History\\Windows_Build_%BUILD_NUMBER%.zip"
                            echo "백업 완료: Builds\\History\\Windows_Build_%BUILD_NUMBER%.zip"
                        ) else (
                            echo "에러: Windows.zip 파일이 생성되지 않았습니다!"
                            exit 1
                        )
                        '''
                    }
                }
            }
        }

        stage('Archive Artifacts') {
            when { expression { BUILD_WINDOWS == 'true' } }
            steps {
                archiveArtifacts artifacts: 'Builds/History/Windows_Build_*.zip', allowEmptyArchive: true, onlyIfSuccessful: true
            }
        }

        stage('Deploy Windows'){
            when{expression {DEPLOY_WINDOWS == 'true'}}
            steps{
                echo 'Deploy Windows'
            }
        }
    }
}