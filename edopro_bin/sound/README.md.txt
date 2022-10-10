# Audio

The game supports three types of audio:
- sound effects
- background music
- chants, which override certain sound effects

Audio files are loaded when the game starts. You can force a refresh by pressing F9.

## Sound effects (no subfolder)

Played when a certain action is performed in a duel. You can disable them in the settings. These may also be easier to hear if you turn down or disable game music. `.wav`, `.ogg`, `.mp3`, and `.flac` files are supported.

These files will be loaded and played; you may replace them with custom ones as long as the name is the same.

	activate.wav          : played when cards or effects are activated, unless there is an associated chant for that card.
	addcounter.wav        : played when counters are added to cards.
	attack.wav            : played when a monster declares an attack, unless there is an associated chant for that monster.
	banished.wav          : played when cards are banished.
	chatmessage.wav       : played when messages are sent in the chat of the current room.
	coinflip.wav          : played when coins are flipped.
	damage.wav            : played when damage is inflicted, either by battle or by effect.
	destroyed.wav         : played when cards are destroyed.
	diceroll.wav          : played when dice are rolled.
	draw.wav              : played when a player draws cards.
	equip.wav             : played when a card is equipped to another.
	flip.wav              : played when cards are flipped, including when a monster is Flip Summoned, unless there is an associated chant for that monster.
	gainlp.wav            : played when a player recovers Life Points.
	nextturn.wav          : played when the next turn starts.
	phase.wav             : played when you are moving from a phase to another.
	playerenter.wav       : played when a player joins the room.
	removecounter.wav     : played when counters are removed from cards.
	set.wav               : played when cards are set on the field.
	shuffle.wav           : played when cards are shuffled.
	specialsummon.wav     : played when a monster is Special Summoned, unless there is an associated chant for that monster.
	summon.wav            : played when a monster is Normal Summoned, unless there is an associated chant for that monster.

## Background music (BGM subfolder)

Played any time while the client is open. You can disable them in the settings. `.wav`, `.ogg`, `.mp3`, and `.flac` files are supported.

To change your background music, go to the BGM subfolder in this folder or create it.
Add any number of files to the following folders (create if missing).

	advantage    : music played when you have an LP advantage over your opponent.
	deck         : music played while you are in the "Deck Edit" menu.
	disadvantage : music played when you have an LP disadvantage compared to your opponent.
	duel		     : music played in a duel, while neither advantage nor disadvantage are played.
	lose         : music played when you lose the duel, until leaving that room.
	menu         : music played in the other game menus, like the home screen.
	win          : music played when you win the duel, until leaving that room.
The current track for the situation is selected randomly from the files in the corresponding folder. Loose files not in one of these subfolders are treated as additional duel music.

## Chants (activate, attack, summon folders)

Chants override certain sound effects and are controlled by the same settings. `.wav`, `.ogg`, `.mp3`, and `.flac` files are supported.

Files are loaded from these subfolders if and only if their name matches the passcode of a card. For example, Lunalight Leo Dancer's passcode is 24550676, so you will need to name the file `24550676.wav` (file extension may vary) and put it in one of these folders:

	activate:   overrides `activate.wav` when the effect of the corresponding card is activated.
	attack:			overrides `attack.wav` when the corresponding monster attacks.
	summon:     overrides `flip.wav`, `summon.wav`, and `specialsummon.wav` when the corresponding monster is summoned.
Do not put multiple files with the same passcode in the same subfolder; the result will be unpredictable.
