# Yugioh Ai
This is a Yugioh Ai Simulator.
The final build will have Ai find the best deck as well as play optimally.
## Breakdown
There are 2 Ais that need to made. A deck building Ai which is written in python. And a game Ai written in c#.

### Setup
* Use Project Ignis Edopro OR if building EdoPro, see https://github.com/edo9300/edopro/wiki/
* You will also need to edit the EDOPro's code in order to launch LAN mode and start the game automaticly when running from the python scripts. See ChangestoEdopro.txt
* Download [sqllite expert personal](http://www.sqliteexpert.com/download.html) to read the .cdb database files
### How to run
* Run python train_ai.py -t -r to train the ai. -t is to set the flag to train, -r is to set the flag to reset the database
* ```read_game_data.py``` will train all the data in the database. Add the flag -s to see what it thinks which action to take
* ```get_action_weights.py``` will run an http server which windbot uses. You must run this if you are going to manualy run the AI.
* After building the Windbot, in the Debug/bin file, you can run a pre program AI using this command ``` .\WindBot.exe Deck=Master Hand=2 Name=Random2 TotalGames=25 IsTraining=false IsFirst=false ``` and run a trained AI using this command ``` .\WindBot.exe Deck=Random1 Hand=1 Name=Random1 TotalGames=100 IsFirst=true IsTraining=false IsManual=false ``` be sure to run the python server by using the command ```get_action_weights.py```.
### Editing the ai
* Notable files in Windbot are Logger.cs, GameAI.cs and RandomExecutorBase.cs
* Logger.cs controls the Weights when they are updated and read
* GameAI controls how the ai works
* RandomExecutorBase controls all the math and decision the AI makes.
* In order to run the AI on EDOPro, you need to update the version number of the windbot in WindbotInfo.cs. You can get the version number from EDOPro by running a LAN game, adding a bot, and copying it's launch arguments
### Using 
* Project Ignis EDOPRO-core https://github.com/ProjectIgnis/EDOPro-Core
* Project Ignis Windbot https://github.com/ProjectIgnis/windbot
