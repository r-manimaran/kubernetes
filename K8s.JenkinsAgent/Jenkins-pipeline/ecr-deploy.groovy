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
'''
    }
  }

  environment {
    AWS_REGION     = 'us-east-1'
    AWS_ACCOUNT_ID = '395109667422'
    ECR_REGISTRY   = "${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"
    ECR_IMAGE      = "${ECR_REGISTRY}/microservices/orderapi"
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

    stage('Docker Build & ECR Push') {
      steps {
        container('kaniko') {
          withCredentials([usernamePassword(
              credentialsId: 'aws-ecr-creds',
              usernameVariable: 'AWS_ACCESS_KEY_ID',
              passwordVariable: 'AWS_SECRET_ACCESS_KEY')]) {
            sh '''
              mkdir -p /kaniko/.docker
              echo "{\\"credHelpers\\":{\\"${ECR_REGISTRY}\\":\\"ecr-login\\"}}" > /kaniko/.docker/config.json

              /kaniko/executor \
                --context "${WORKSPACE}/${DOCKER_CONTEXT}" \
                --dockerfile "${WORKSPACE}/${DOCKER_CONTEXT}/Dockerfile" \
                --destination "${ECR_IMAGE}:${BUILD_NUMBER}" \
                --destination "${ECR_IMAGE}:latest"
            '''
          }
        }
      }
    }
  }

  post {
    success { notify('Deployment succeeded!') }
    failure { notify('Deployment failed!') }
  }
}