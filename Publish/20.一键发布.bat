:: @echo off

cd /d �ű�\�����ű� 

:: ���з���
:: for /R %%s in (����-*) do (   
::  start "����" "%%s"
:: )  

:: ���з���
for /R %%s in (����-*) do (   
 call "%%s"
)  

cd /d ..\����docker����
call "docker����-1.�����ļ�.bat"

echo �������
echo �������
echo �������

:: pause