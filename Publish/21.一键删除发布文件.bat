 
rd /s/q "04.Publish"

rd /s/q "05.nuget/nuget"

cd /d 脚本/制作docker镜像
call "docker镜像-2.删除文件.bat"
cd /d ../..
 