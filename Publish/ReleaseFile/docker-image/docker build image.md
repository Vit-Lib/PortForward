#构建多架构镜像

#docker login -u serset -p xxxxxxxxx

#---------------------------------------------------------------------
#(x.1)初始化构建器

#启用 buildx 插件
export DOCKER_CLI_EXPERIMENTAL=enabled

#验证是否开启
docker buildx version

#启用 binfmt_misc
docker run --rm --privileged docker/binfmt:66f9012c56a8316f9244ffd7622d7c21c1f6f28d

#验证是 binfmt_misc 否开启
ls -al /proc/sys/fs/binfmt_misc/


#创建一个新的构建器
docker buildx create --use --name mybuilder

#启动构建器
docker buildx inspect mybuilder --bootstrap

#查看当前使用的构建器及构建器支持的 CPU 架构，可以看到支持很多 CPU 架构：
docker buildx ls



#---------------------------------------------------------------------
#(x.2)构建多架构镜像（ arm、arm64 和 amd64 ）并推送到 Docker Hub

#把本文件所在目录下的文件夹拷贝到image 
cd /home/ec2-user/image 


#构建镜像并推送到 Docker Hub
cd portforward
docker buildx build -t serset/portforward:1.12.359 -t serset/portforward --platform=linux/amd64,linux/arm64,linux/arm/v7 . --push

 



#强制删除镜像名称中包含portforward的镜像
# docker rmi --force $(docker images | grep portforward | awk '{print $3}')












 