import sys, subprocess, os

os.chdir(os.getcwd()+'/WindBot-Ignite-master/bin/Debug')

Deck = "Random1"
Name = "Random1"
Hand = 0
TotalGames = 1
RolloutCount = 0
IsFirst = True
IsTraining = True
WinsThreshold = 50
PastWinsLimit = 20

if "--deck" in sys.argv:
	Deck = int(sys.argv[sys.argv.index("--deck")+1])
if "--name" in sys.argv:
	Name = int(sys.argv[sys.argv.index("--name")+1])
if "--hand" in sys.argv:
	Hand = int(sys.argv[sys.argv.index("--hand")+1])
if "--total" in sys.argv:
	TotalGames = int(sys.argv[sys.argv.index("--total")+1])
if "--rollout" in sys.argv:
	RolloutCount = int(sys.argv[sys.argv.index("--rollout")+1])
if "--first" in sys.argv:
	IsFirst = int(sys.argv[sys.argv.index("--first")+1])
if "--training" in sys.argv:
	IsTraining = int(sys.argv[sys.argv.index("--training")+1])
if "--winthresh" in sys.argv:
	WinsThreshold = int(sys.argv[sys.argv.index("--winthresh")+1])
if "--pastwinslimit" in sys.argv:
	PastWinsLimit = int(sys.argv[sys.argv.index("--pastwinslimit")+1])

p = subprocess.Popen(["WindBot.exe","Deck="+Deck,
                        "Name="+str(Name),
                        "Hand="+str(Hand),
                        "IsTraining="+str(IsTraining), 
                        "TotalGames="+str(TotalGames), 
                        "RolloutCount="+str(RolloutCount), 
                        "IsFirst="+str(IsFirst), 
                        "WinsThreshold="+str(WinsThreshold), 
                        "PastWinsLimit="+str(PastWinsLimit)],
                        #"Chat=false"],
               shell=True, stdout=subprocess.PIPE, stderr = subprocess.PIPE, universal_newlines=True)
output, stderr = p.communicate()

#if recordDeck == '1':
#for line in output.split("\n"):
#	print(line)
# print(output)
# print(stderr)
    
# result = 0
# if format(output).find(': Win') > 0:
#     result = 1
# elif format(output).find(': Lose') > 0:
#     result = -1   

# Deck=Random1 Hand=1 Name=Random1 TotalGames=200 RolloutCount=1 IsFirst=true IsTraining=true WinsTreshold=50 PastWinsLimit=20
# .\WindBot.exe Deck=Master Hand=3 Name=Master TotalGames=200 RolloutCount=1 IsFirst=False IsTraining=true WinsTreshold=50 PastWinsLimit=20
# .\WindBot.exe Deck=Master Hand=3 Name=Random2 TotalGames=200 RolloutCount=1 IsFirst=True IsTraining=False WinsTreshold=50 PastWinsLimit=20