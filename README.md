# portforward
> portforward为net core开发的端口转发工具  
> 代码地址: https://github.com/Vit-Lib/PortForward  

# 1. create container

``` bash

# show help
docker run --rm -it --net=host serset/portforward dotnet PortForward.dll Help

# 后台运行端口反向转发服务
docker run --name=portforward --restart=always --net=host -d serset/portforward dotnet PortForward.dll PortForwardServer--at111sssssfvbvdscv--10010--10011


#运行容器，在断开后自动关闭并清理
docker run --rm -it --net=host serset/portforward sh
dotnet PortForward.dll PortForwardLocal--8000--192.168.1.5--3384--NoPrint


docker run --rm -it --net=host serset/portforward dotnet PortForward.dll \
PortForwardLocal--8000--192.168.1.5--3384--NoPrint

docker run --rm -it --net=host serset/portforward dotnet PortForward.dll \
PortForward.dll PortForwardServer--authToken--6202--6203

docker run --rm -it --net=host serset/portforward dotnet PortForward.dll \
PortForwardClient--authToken--192.168.1.100--6203--abc.com--3389--5
 

```
 

# 2.命令说明
从参数获取配置信息,分为“本地端口转发工具”和“端口桥接工具”。  
----本地端口转发工具----  
     配置信息格式为：  
         PortForwardLocal--inputConnPort--outputConnHost--outputConnPort--NoPrint  
     Demo:  
         dotnet PortForward.dll PortForwardLocal--8000--192.168.1.5--3384--NoPrint  

     说明:  
         把本地的8000端口转发至 主机192.168.1.5的3384端口  
     NoPrint:定义是否回显，若指定为NoPrint则不实时回显连接信息  
  
----端口桥接工具----  
     客户端格式为：  
         PortForwardClient--authToken--serverHost--outputConnPort-localConnHost--localConnPort--ConnectCount--NoPrint  
     服务端格式为：  
         PortForwardServer--authToken--inputConnPort--outputConnPort--NoPrint  
     Demo:  
         dotnet PortForward.dll PortForwardClient--authToken--192.168.1.100--6203--abc.com--3389--5  
         dotnet PortForward.dll PortForwardServer--authToken--6202--6203  
  
     说明:  
         把服务端（serverHost）的 inputConnPort端口转发至 客户端连接的 主机localConnHost的端口localConnPort  
         服务端和客户端通过端口outputConnPort连接  
     autoToken   :权限校验字段，服务端和客户端必须一致  
     ConnectCount:客户端保持的空闲连接个数，推荐5  
     NoPrint     :定义是否回显，若指定为NoPrint则不实时回显连接信息，可不指定  
 


