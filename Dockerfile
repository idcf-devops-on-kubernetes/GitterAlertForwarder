FROM mcr.azk8s.cn/dotnet/core/runtime:3.1 AS artifact

WORKDIR /app
COPY ./ /app

ENV TZ=Asia/Shanghai

ENV ASPNET_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "./GitterAlertForwarder.dll"]

# dotnet publish -r linux-x64 -o publish 
# docker build -t ccr.ccs.tencentyun.com/idcf-k8s-devops/gitter-messager:v1 -f ./Dockerfile publish