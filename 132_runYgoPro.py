import sys, string, os, time, subprocess, keyboard

print("		running ygppro")
os.chdir(os.getcwd()+'/ProjectIgnis')

#os.system(os.getcwd() + '/ygopro.exe')
subprocess.run([os.getcwd() + "/ygopro.exe"],shell=True)
2