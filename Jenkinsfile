pipeline {
    agent any
    
    environment {
        PROJECT_ID = 'iron-handler-471307-u7'
        GCR_REGISTRY = 'gcr.io'
        IMAGE_BACKEND = "${GCR_REGISTRY}/${PROJECT_ID}/dotnet-crud-backend"
        IMAGE_FRONTEND = "${GCR_REGISTRY}/${PROJECT_ID}/dotnet-crud-frontend"
        CLUSTER_NAME = 'jenkins-cluster'
        REGION = 'us-central1'
        KUBE_NAMESPACE = 'dotnet-app-gcp'
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        
        stage('Docker Build') {
            steps {
                script {
                    sh '''
                        docker build -f backend.Dockerfile -t ${IMAGE_BACKEND}:${BUILD_NUMBER} .
                        docker build -f frontend.Dockerfile -t ${IMAGE_FRONTEND}:${BUILD_NUMBER} .
                    '''
                }
            }
        }

        stage('Push Docker Images to GCR') {
            steps {
                withCredentials([file(credentialsId: 'jenkins-gke-sa', variable: 'GCP_KEY_FILE')]) {
                    sh '''
                    set -e
                    echo "Activating Google Cloud Service Account..."
                    gcloud auth activate-service-account --key-file=${GCP_KEY_FILE}
                    gcloud config set project ${PROJECT_ID}

                    echo "Configuring Docker to use gcloud as a credential helper..."
                    gcloud auth configure-docker ${GCR_REGISTRY} -q

                    echo "Pushing images with tag ${BUILD_NUMBER}..."
                    docker push ${IMAGE_BACKEND}:${BUILD_NUMBER}
                    docker push ${IMAGE_FRONTEND}:${BUILD_NUMBER}

                    echo "Tagging and pushing 'latest'..."
                    docker tag ${IMAGE_BACKEND}:${BUILD_NUMBER} ${IMAGE_BACKEND}:latest
                    docker tag ${IMAGE_FRONTEND}:${BUILD_NUMBER} ${IMAGE_FRONTEND}:latest
                    docker push ${IMAGE_BACKEND}:latest
                    docker push ${IMAGE_FRONTEND}:latest
                    '''
                }
            }
        }

        stage('Deploy to GKE') {
            steps {
                withCredentials([file(credentialsId: 'jenkins-gke-sa', variable: 'GCP_KEY_FILE')]) {
                    sh '''
                    set -e
                    echo "Authenticating and fetching GKE credentials..."
                    gcloud auth activate-service-account --key-file=${GCP_KEY_FILE}
                    gcloud config set project ${PROJECT_ID}
                    gcloud container clusters get-credentials ${CLUSTER_NAME} --region ${REGION}

                    echo "Applying Kubernetes manifests and updating images..."
                    kubectl apply -f k8s/
                    kubectl set image deployment/dotnet-crud-backend backend=${IMAGE_BACKEND}:${BUILD_NUMBER} -n ${KUBE_NAMESPACE} || true
                    kubectl set image deployment/dotnet-crud-frontend frontend=${IMAGE_FRONTEND}:${BUILD_NUMBER} -n ${KUBE_NAMESPACE} || true
                    kubectl rollout status deployment/dotnet-crud-backend -n ${KUBE_NAMESPACE}
                    kubectl rollout status deployment/dotnet-crud-frontend -n ${KUBE_NAMESPACE}

                    echo "---------------------------------------"
                    echo "Service URL:"
                    echo "---------------------------------------"
                    FRONTEND_IP=$(kubectl -n ${KUBE_NAMESPACE} get svc dotnet-crud-frontend-service -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
                    if [ -n "$FRONTEND_IP" ]; then
                      echo "Application URL: http://$FRONTEND_IP"
                    else
                      echo "Frontend LoadBalancer IP not ready yet. Run: kubectl -n ${KUBE_NAMESPACE} get svc dotnet-crud-frontend-service -w"
                    fi
                    '''
                }
            }
        }
    }
    
    post {
        success {
            echo 'Deployment successful!'
            script {
                sh '''
                    set -e
                    FRONTEND_IP=$(kubectl -n ${KUBE_NAMESPACE} get svc dotnet-crud-frontend-service -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
                    if [ -n "$FRONTEND_IP" ]; then
                      echo "\nApplication URL: http://$FRONTEND_IP\n"
                    else
                      echo "\nApplication LoadBalancer external IP not ready yet. Run: kubectl -n ${KUBE_NAMESPACE} get svc dotnet-crud-frontend-service -w\n"
                    fi
                '''
            }
        }
        failure {
            echo 'Deployment failed!'
        }
    }
}
