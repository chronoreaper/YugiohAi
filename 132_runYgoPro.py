import sys, string, os, time, subprocess

print("running ygppro")
os.chdir(os.getcwd()+'/KoishiPro_Sakura')

#os.system(os.getcwd() + '/ygopro.exe')
subprocess.run([os.getcwd() + "/ygopro.exe","-c"],shell=True)