#!/bin/bash
mono update.exe -ci
git add ../. -A
git commit -m "update data"
git push origin master