@Library('jenkins-shared-library') _

pipeline {
  agent {
    kubernetes {
      namespace 'jenkins-agents'
      defaultContainer 'jnlp'
      yaml '''
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: dotnet
    image: mcr.microsoft.com/dotnet/sdk:8.0
    command: ["sleep"]
    args: ["infinity"]
    resources:
      requests: { cpu: "500m", memory: 1Gi }
      limits:   { cpu: "2",    memory: 3Gi }
  - name: kaniko
    image: gcr.io/kaniko-project/executor:debug
    command: ["sleep"]
    args: ["infinity"]
    resources:
      requests: { cpu: "500m", memory: 1Gi }
      limits:   { cpu: "2",    memory: 4Gi }
    volumeMounts:
    - name: docker-config
      mountPath: /kaniko/.docker
  volumes:
  - name: docker-config
    secret:
      secretName: acr-cred
      items:
      - key: .dockerconfigjson
        path: config.json
'''
    }
  }

  environment {
    ACR_NAME       = '<acrName>'                         // change
    ACR_REGISTRY   = "${ACR_NAME}.azurecr.io"
    IMAGE_NAME     = 'ordersapi'
    ACR_IMAGE      = "${ACR_REGISTRY}/${IMAGE_NAME}"
    // Folder that contains the Dockerfile (adjust to your repo layout)
    DOCKER_CONTEXT = 'Jenkins-Smart-Utils/Microservices/OrdersApi'
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
  }

  stages {
    stage('Greetings') {
      steps {
        helloWorld("Message from Pipeline")
      }
    }

    stage('Checkout') {
      steps {
        checkoutGit('https://github.com/r-manimaran/Full-Stack-App.git', 'main')
      }
    }

    stage('Restore') {
      steps {
        container('dotnet') {
          dotnetBuild('Jenkins-Smart-Utils/Microservices/OrdersApi', 'OrdersApi.csproj')
        }
      }
    }

    stage('Docker Build & ACR Push') {
      steps {
        container('kaniko') {
          sh '''
            /kaniko/executor \
              --context "${WORKSPACE}/${DOCKER_CONTEXT}" \
              --dockerfile "${WORKSPACE}/${DOCKER_CONTEXT}/Dockerfile" \
              --destination "${ACR_IMAGE}:${BUILD_NUMBER}" \
              --destination "${ACR_IMAGE}:latest" \
              --cache=true \
              --cache-repo="${ACR_IMAGE}-cache" \
              --digest-file=/tmp/digest.txt
            echo "Pushed ${ACR_IMAGE}:${BUILD_NUMBER}"
            echo "Digest: $(cat /tmp/digest.txt)"
          '''
        }
      }
    }
  }

  post {
    success { notify('Deployment succeeded!') }
    failure { notify('Deployment failed!') }
  }
}