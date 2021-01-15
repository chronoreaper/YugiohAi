import sys, subprocess, os

os.chdir(os.getcwd()+'/windbot_master/bin/Debug')

recordDeck = sys.argv[3]

result = 0

p = subprocess.Popen(["WindBot.exe","Deck="+sys.argv[1],"Name="+sys.argv[2]],
               shell=True, stdout=subprocess.PIPE, stderr = subprocess.PIPE)
output, stderr = p.communicate()

#if recordDeck == '1':
    #print(format(output))    
if format(output).find(': Win') > 0:
    result = 1
elif format(output).find(': Lose') > 0:
    result = -1   

if recordDeck == '1':
    if result != 0:  
        if result  == 1:
            print("[win]")
        elif result == -1:
            print("[lose]")