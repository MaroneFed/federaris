@echo off
REM Telecharge les vrais sons des memes (mode MEME). Il faut Python : https://www.python.org/downloads/
cd /d "%~dp0.."
py Tools\memes-sons.py
if errorlevel 9009 python Tools\memes-sons.py
