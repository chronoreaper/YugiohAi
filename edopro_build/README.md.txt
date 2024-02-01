# Project Ignis: EDOPro
All assets for the game, except card images. See LICENSE and COPYING in each folder for proper credits, copyright, and rules for redistribution.
On Windows, please do not put your game install in Program Files, Downloads, or any other location that might be read-only or require admin permissions.
On Linux, after moving the game install to your preferred location, you can run `./install.sh` from a terminal to install desktop files for the current user.

## System requirements

Supported platforms:
- Windows 2000 or later, 32-bit or 64-bit
- macOS 10.9 or later
- 64-bit GNU/Linux with X11 and glibc 2.27+ (e.g. Debian 10+, Ubuntu 18.04+, Fedora, CentOS 8+, rolling release distros like Arch)

DirectX 9 or OpenGL 1.0 supporting graphics driver required.

1 GB free disk space recommended for asset updates and images.

1 GB free RAM recommended, though the game is not expected to exceed 300 MB of memory unless you spam the restart button.

### Prerequisites for WindBot Ignite (AI):
- Windows: install .NET Framework 4 if you don't have it. This ships with Windows 10.
- Linux: install the mono-complete package https://www.mono-project.com/download/stable/#download-lin
- macOS: install Mono with the .pkg https://www.mono-project.com/download/stable/#download-mac

## Keyboard and mouse shortcuts

### General:
* ESC: Minimizes the window if not typing
* F9: Toggle topdown field view
* F10: Mac and Windows: Save the current window position (shift+F10 to restore it)
* F11: Toggles fullscreen
* F12: Captures a screenshot
* CTRL+O: Opens the additional settings window
* R: Reloads fonts if not typing
* CTRL+R: Reloads current skin
* CTRL+1: Switch to card info tab
* CTRL+2: Switch to duel log tab
* CTRL+3: Switch to chat log tab
* CTRL+4: Switch to settings tab
* CTRL+5: Switch to repositories tab
* Drag and drop support for files and text:
	* drop an `ydk` file in the main menu or the deck edit area to load that deck
	* drop a card passcode or card name in the deck edit area to add that card to the deck
	* drop a `ydke://` URL in the deck edit area to load the deck specified by that URL
	* drop a `yrpX` file in the main menu or the replay selection menu to load that replay, if valid
	* drop a Lua file in the main menu or the puzzle selection menu to load that puzzle, if valid
	* drop text in a text box to insert text
	* drop a `.pem`, `.cer`, `.crt` certificate bundle file to make the client use that for ssl verification (if you're getting ssl certificate is invalid)

### Deck editor:
* Right Mouse Button: Adds/removes a card from the deck
* Middle Mouse Button: Adds another copy of a card to the deck or side deck
* Shift+Right Mouse Button or Hold Left Mouse Button then click Right Mouse Button: Adds a card to the side deck
* With the exception of Shift+Right Mouse Button, holding Shift will ignore ALL deck building rules

While not typing:
* CTRL+C: Copies a `ydke://` URL of the deck list for sharing
* CTRL+SHIFT+C: Copies a plain text deck list for sharing
* CTRL+V: Imports a `ydke://` URL decklist from the clipboard

### Duel:
* Hold A or Hold Left Mouse Button: Lets the system stop at every timing.
* Hold S or Hold Right Mouse Button: Lets the system skip every timing.
* Hold D: Lets the system stop at available timing.
* F1 to F4: Shows the cards in your GY, banished, Extra Deck, Xyz Materials respectively.
* F5 to F8: Shows the cards in your opponent's GY, banished, Extra Deck, Xyz Materials respectively.
* Double click (or tap) on a card pile: Shows the cards in such pile.

### macOS:
Note that system hotkeys may intercept some of the above keyboard shortcuts.
The following app shortcuts are also available in the app and dock menus:
* Cmd+N opens a new instance with audio muted
* Cmd+Q quits the game
* Ctrl+Cmd+F toggles fullscreen

## Deck editor search functions
* `string`:
	returns all cards that have `string` in their name OR in the card text.
	Example: `Hero`
* `@string`
	returns all cards that belong to the `string`  archetype.
	Example: `@Hero`
* `$string`
	returns all cards that have `string` in their name only, which ignores the card text.
	Example: `$Heroic`
* `string1||string2`
	returns all cards that have `string1` OR `string2` in their name/text.
	Example: `Trickstar||Bounzer`
* `!!string`:
	negative lookup (NOT)
* `string1*string2`
	replaces any character in any amount. Example: `Eyes*Dragon` will return cards Blue-Eyes White Dragon, Red-Eyes B. Dragon, Galaxy-Eyes Photon Dragon, etc.

These can be combined. Example: `@blue-eyes||$eyes of blue` returns all cards that belong to either the `Blue-Eyes` archetype or have `Eyes of Blue` in their names.

The ATK, DEF, Level/Rank, and Scale textboxes support searching for `?`. You can also prefix the search with comparison modifiers <, <=, >=, >, and =. 

## Test hand
A Hand Test mode is accessible from the deck editor, with quick restart.
The duel will **never** end normally in this game mode (e.g. running out of LP, decking out)
* Notice that this mode was not made to control the opponent. Dueling vs yourself in LAN Mode would be a better option for that.

## Discord Rich Presence
Works with the desktop version of Discord. In your Discord settings, turn on Game Activity first.

Your status on Discord will update to be playing a game, including elapsed time.
Activities displayed in Rich Presence:
* Dueling
* In menu
* Playing a puzzle
* Watching a replay
* Editing a deck

### Game invites
Host a room on a server (LAN does not work). In the appropriate channel or private message, the upload (+) icon should change to have an additional green play button.
Clicking on it will send out a game invite to your room with your message of choice. If the room is locked, the password will be skipped for invitees.
Users can accept the invite while EDOPro is closed if they've started it once before; the game should be launched automatically. Note that Discord is rather fickle and changes this behaviour on us very frequently, so if the game fails to automatically launch, try starting the game.

## Customization

### Default textures:
See README in `textures`.

### Skins:
Editable by adding subfolders to **skin**. For each folder, provide a unique `skin.xml` file, with the changes you want.
You can switch skins in the settings (CTRL+O). For instructions on the supported fields and what they change, see README in `skin`.

### Audio:
See README in `sound`. There are many new features, including summon chants!
Music and sound volume controls are also separated.

### MSAA (antialiasing)
Makes sharp/pixelated edges softer, but requires more performance.
2D elements might look blurred at higher levels. Rendering results are hardware- and device-dependent.
The program will automatically try smaller MSAA values if the driver does not support the specified MSAA level.

## Advanced configuration

### system.conf
`config/system.conf` handles most of the configurations available in the game. It is overwritten when the game is closed normally.
Only options not directly configurable in-game are listed here.
Configurations listed as "boolean" accept either 0 for 'disabled' or 1 for 'enabled'.

| Name     | Purpose | Example |
| -------- | ------- | ------- |
| driver_type  | graphic driver used for rendering. Valid values are: opengl, d3d9, ogles1, ogles2, default. The availability of those values is listed in the table below.  | |
| useWayland  | Linux only. 1 = use experimental wayland device; 0 = use x11 device. | |
| textfont | path to the font used for texts and its size | fonts/NotoSansJP-Regular.otf 12 |
| numfont  | path to the font used for numbers            | fonts/NotoSansJP-Regular.otf |
| fallbackFonts  | path to the fonts to be used as fallback for missing characters (each font and its **must** be contained in double quotes `"`, the game ships with a `bundled` font that will always be loaded) | "fonts/fallback1.otf 15" "bundled 12" |

If a character cannot be found in the supplied font, it will not be displayed. The shipped font supports all characters that appear on Yu-Gi-Oh! cards in Latin alphabets and Japanese.

### supported values for driver_type based on the system
|      | opengl | d3d9 | ogles1 | ogles2 | default |
| -------- | :-------: | :-------: | :-------: | :-------: | :-------: |
| Windows  | X | X | X (If supported by the driver) | X (If supported by the driver) | d3d9 |
| Linux Wayland  | X (Only if LibGLx is present) | | X (only if libGLESv1_CM is present) | X | ogles2 |
| Linux X11 | X |  | X (only if libGLESv1_CM is present) | X (only if libGLESv2 is present) | opengl |
| MacOS | X |  |  |  | opengl |
| Android |  | | X | X | ogles2 |

### configs.json
`config/configs.json` handles the servers the client is connected to, which include repositories for updates, servers for duels and pictures.

#### repos (array)
* url: required, the complete url of the repository to check for updates.
* repo_path: optional, the subdirectory in the client's directory where the contents will be saved. If not provided, the folder will be created in the expansions folder and will have the repository's name.
* has_core: optional.
* core_path: optional, used if has_core is true.
* data_path: optional, the folder where the databases and the strings will be loaded from in the repository. If not provided, it will load from the main folder of the repository.
* script_path: optional, the folder where the scripts will be loaded from in the repository. If not provided, it will load from the script folder of the repository.
* pics_path: optional, the folder where the pics will be loaded from in the repository. If not provided, it will load from the pics folder of the repository.
* lflist_path: optional, the path for lflists, if the repository contains any.
* should_update: true/false, optional, if the client will download the contents of the repository. If the repository is missing, it will still be downloaded only for the first time. If not provided, it will be set to true.
* should_read: true/false, if set to false the game will ignore that repository. If not provided, it will be set to true.

#### urls (array)
* url: A URL format string for direct card image download, or "default". Should contain `{}` to be replaced by the client with the card's passcode.
* type: pic/field/cover

#### servers (array)
* name: Display name
* address: URL (domain or IP works) for connecting to rooms nad hosting
* duelport: port for the above
* roomaddress: URL for retrieving the room list via the REST API
* roomlistprotocol: url protocol that will be used for roomaddress. Supported protocols are ``http`` (default if not provided) and ``https``
* roomlistport: port for the above

#### posixPathExtension
Used on macOS and Linux as additional search paths for Mono, required to run WindBot Ignite. Generally you should not need to change this.
