@echo off
cd /d %~dp0
update -ci
git add ../. -A
git commit -m "update data"
git push origin master