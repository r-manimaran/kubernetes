pipeline {
  agent {
    kubernetes {
      namespace 'jenkins-agents'
      yaml '''
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: dotnet
    image: mcr.microsoft.com/dotnet/sdk:8.0
    command: ["sleep"]
    args: ["infinity"]
'''
    }
  }
  stages {
    stage('Smoke test') {
      steps { container('dotnet') { sh 'dotnet --info' } }
    }
  }
}