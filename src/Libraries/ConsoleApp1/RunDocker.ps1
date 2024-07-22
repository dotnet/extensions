dotnet build
docker build -t consoleapp1 .
docker stop consoleapp1container
docker rm consoleapp1container
docker run -d --name consoleapp1container consoleapp1
