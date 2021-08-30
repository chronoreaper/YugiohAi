import os
import random, os, sys

filePath = "AI_Random.ydk"#os.getcwd() + '/ProjectIgnis/bin/Debug/Decks/'+ sys.argv[1] 

f = open(filePath,"r")
main = []
extra = []
side = []
part = 0
for line in f.readlines():

  if "#extra" in line:
    part = 1
    continue
  elif "!side" in line:
    part = 2
    continue
  elif "#main" in line:
    part = 0
    continue
  elif "#" in line:
    continue

  if part == 0:
    main.append(line.strip())
  elif part == 1:
    extra.append(line.strip())
  else:
    side.append(line.strip())
random.shuffle(main)
random.shuffle(extra)
random.shuffle(side)
f.close()

f = open(filePath, "w")    
f.write("#created by deck_maker_ai\n")

f.write("#main\n")
for i in main:
   f.write(i +'\n')
f.write("#extra\n")
for i in extra:
   f.write(i +'\n')
f.write("!side\n")
for i in side:
   f.write(i +'\n')    

f.close()