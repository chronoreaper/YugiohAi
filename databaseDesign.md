# Database Design
This is the documentation on how the database stores data.
## cardList
The list of cards in the card pool.

* Id, unique, stores the card id
* Name, the name of the cardi id
## cardRelated
Stores the relationship between cards within a deck.

* id, the id of a card in the deck.
* relatedid, another id inside the same deck, can be the same as id.
* idQuant, how many copies of id you have in the deck.
* relatedQuant, how many copies of relatedid you have in the deck.
* wins, how many times they have won together
* gamesPlayed, how many games they have played togther
* games, how many times they appear in the same deck together.

Note

wins <= gamesPlayed <= games

Other Columns, use SQL Query

* percentage = round(wins*1.0/gamesPlayed,3)
* weight = round(wins*wins*1.0/gamesPlayed,2)

## cardCounter
The relationship comparing to see how many times a card wins against another card.

* id, a card in your deck that was played in the game
* otherid, a card in the opposing deck that was played in the game
* wins, how many times id has won playing against otherid
* games, how many games they have played against each other.

## playCard
The data to see if the ai should play a card or not

* id, the card to check if it should play
* action, the action about to be performed
* verify, what it should check in the game
* value, the value of what it should check
* count, the number of times this holds true
* wins, how many times it has won if the ai has played that card
* games, how many games it has played the card
* inprogress, keeps track if the ai has not yet recorded the results of the games yet.
