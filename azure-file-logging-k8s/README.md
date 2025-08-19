
# Checklist
- [ ] Create the console app to do some processing
    - [ ] Add the Serilog Nuget packages
    - [ ] Add the processing logic to generate sample Info and error logs
    - [ ] Set the Enviornment variable LOG_FILE_PATH in lauchsettings file
    - [ ] Run the Console App and test the logs generated in the defined log path
- [ ] Create the Dockerfile
    - [ ] Add the container support for the console app.
    - [ ] Build the docker image
    - [ ] Tag the image with the docker hub repository information
    - [ ] Push the image to the docker hub
- [ ] Create the Docker Compose file
    - [ ] Add the docker compose file to run the console app
    - [ ] Refer the image from Docker hub
    - [ ] Create a .env file and define the environment variable for Docker hub name
    - [ ] Run the app in the container.
    - [ ] Test the generated log in the volume mapped location.
- [ ] Create Azure Storage Account Files

- [ ] Run the App in the Kubernetes cluster.


docker rm -f processingapp