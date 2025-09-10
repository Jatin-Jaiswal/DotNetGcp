pipeline {
    agent any
    
    environment {
        GCR_PROJECT = 'iron-handler-471307-u7'
        GCR_REGISTRY = 'gcr.io'
        IMAGE_BACKEND = "${GCR_REGISTRY}/${GCR_PROJECT}/dotnet-crud-backend"
        IMAGE_FRONTEND = "${GCR_REGISTRY}/${GCR_PROJECT}/dotnet-crud-frontend"
        KUBE_NAMESPACE = 'dotnet-app-gcp'
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        
        stage('Build Backend Image') {
            steps {
                script {
                    def backendImage = docker.build("${IMAGE_BACKEND}:${BUILD_NUMBER}")
                    docker.withRegistry("https://${GCR_REGISTRY}", 'gcp-service-account') {
                        backendImage.push("${BUILD_NUMBER}")
                        backendImage.push("latest")
                    }
                }
            }
        }
        
        stage('Build Frontend Image') {
            steps {
                script {
                    def frontendImage = docker.build("${IMAGE_FRONTEND}:${BUILD_NUMBER}", "-f frontend.Dockerfile .")
                    docker.withRegistry("https://${GCR_REGISTRY}", 'gcp-service-account') {
                        frontendImage.push("${BUILD_NUMBER}")
                        frontendImage.push("latest")
                    }
                }
            }
        }
        
        stage('Deploy to GKE') {
            steps {
                script {
                    sh """
                        gcloud container clusters get-credentials jenkins-cluster --region=us-central1
                        kubectl apply -f k8s/
                        kubectl set image deployment/dotnet-crud-backend backend=${IMAGE_BACKEND}:${BUILD_NUMBER} -n ${KUBE_NAMESPACE}
                        kubectl set image deployment/dotnet-crud-frontend frontend=${IMAGE_FRONTEND}:${BUILD_NUMBER} -n ${KUBE_NAMESPACE}
                        kubectl rollout status deployment/dotnet-crud-backend -n ${KUBE_NAMESPACE}
                        kubectl rollout status deployment/dotnet-crud-frontend -n ${KUBE_NAMESPACE}
                    """
                }
            }
        }
    }
    
    post {
        success {
            echo 'Deployment successful!'
        }
        failure {
            echo 'Deployment failed!'
        }
    }
}
