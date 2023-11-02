# Yugioh Ai
This is a Yugioh Ai Simulator.
The final build will have Ai find the best deck as well as play optimally.
## Breakdown
There are 2 Ais that need to made. A deck building Ai which is written in python. And a game Ai written in c#.

### Setup
* <s>Download all the cards using KoishiPro_Sakura/update-koishipro/update.exe</s>
* Use Project Ignis Edopro
* Download for python pyautogui (pip install pyautogui) and cv2 (pip install opencv-python)
* Download [sqllite expert personal](http://www.sqliteexpert.com/download.html) to read the .cdb database files
* Follow the instructions in Windbot
### How to run
* Run python train_ai.py -t -r to train the ai. -t is to set the flag to train, -r is to set the flag to reset the database
* read_game_data.py will train all the data in the database. Add the flag -s to see what it thinks which action to take
* get_action_weights.py will run an http server which windbot uses. You must run this if you are going to manualy run the AI.
* You can run a pre program AI using this command ``` .\WindBot.exe Deck=Master Hand=2 Name=Random2 TotalGames=25 IsTraining=false IsFirst=false ``` and run a trained AI using this command ``` .\WindBot.exe Deck=Random1 Hand=1 Name=Random1 TotalGames=100 IsFirst=true IsTraining=false IsManual=false ``` be sure to run the python server.
### Editing the ai
* Notable files in Windbot are Logger.cs, GameAI.cs and RandomExecutorBase.cs
* Logger.cs controls the Weights when they are updated and read
* GameAI controls how the ai works
* RandomExecutorBase controls all the math and decision the AI makes.
* Make sure your computer's default to open .py files is python, or this will not work.
### Using 
* Windbot: https://github.com/IceYGO/windbot
* KoishiPro: https://github.com/purerosefallen/ygopro/releases
