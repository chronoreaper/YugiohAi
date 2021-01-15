# Yugioh Ai
This is a Yugioh Ai Simulator.
The final build will have Ai find the best deck as well as play optimally.
## Breakdown
There are 2 Ais that need to made. A deck building Ai which is written in python. And a game Ai written in c#.

### Setup
* Download all the cards using KoishiPro_Sakura/update-koishipro/update.exe
* Download for python pyautogui (pip install pyautogui) and cv2 (pip install opencv-python)
* Download [sqllite expert personal](http://www.sqliteexpert.com/download.html) to read the .cdb database files
* Follow the instructions in Windbot
### How to run
* Run 1_MainFunction.py. It will run all the other python scripts. Scripts are ran as ordered, 1, 11, 12, 13, 131, 132, 133.
* The decks that are ran from windbot is random and random2
### Editing the ai
* Notable files in Windbot are Logger.cs, GameAI.cs and RandomExecutorBase.cs
* Logger.cs controls the Weights when they are updated and read
* GameAI controls how the ai works
* RandomExecutorBase controls all the math and decision the AI makes.
### Using 
* Windbot: https://github.com/IceYGO/windbot
* KoishiPro: https://github.com/purerosefallen/ygopro/releases
