Auto Input Manager (aka Input Enchanter) makes input management easier by modifying your scripts. It finds hard-coded input keys from the script files and makes them compatible with Unity's built-in input manager (Converts Input.GetKey() /GetKeyDown() /GetKeyUp() methods to Input.GetButton() /GetButtonDown() /GetButtonUp() methods).
With Auto Input Manager, your script files becomes more modular and customizing keybindings becomes possible. With that non-programmers can change default keybindings in the editor without editing the code and players can customize their own keybindings in the start-up resolution dialog (only for standalone builds: Windows/OSX/Linux). Also, managing input will be easier since they will be stored in one place, not scattered over many script files.


Contact:
merryyellow@outlook.com


Forum:
https://forum.unity.com/threads/not-released-free-auto-input-manager.592852/


Restrictions:
* Unity 3.4.0 or higher (Unity 2018.X, Unity 2017.X, Unity 5.X, Unity 4.X, Unity 3.5.X and Unity 3.4.X are supported).


Before Using (Windows):
* Install Microsoft .NET Framework 4.7.1 or higher. Link: https://www.microsoft.com/en-us/download/details.aspx?id=56115
* Install Microsoft Visual C++ Redistributable for Visual Studio 2017. Link: https://go.microsoft.com/fwlink/?LinkId=746572
* Make sure your code/project compiles successfully. There shouldn't be any compile errors (But there could be warnings, they are fine).
* Backup your script files or whole project. Auto Input Manager is going to make minor changes to your script files and these changes are not revertible. Committing to repository or copying files is highly recommended before using Auto Input Manager.


Before Using (macOS)
* Install Microsoft .NET Core SDK 2.1 or higher. Link: https://www.microsoft.com/net/download?initial-os=macos
* Make sure your code/project compiles successfully. There shouldn't be any compile errors (But there could be warnings, they are fine).
* Backup your script files or whole project. Auto Input Manager is going to make minor changes to your script files and these changes are not revertible. Committing to repository or copying files is highly recommended before using Auto Input Manager.


Before Using (Linux)
* Install Microsoft .NET Core SDK 2.1 or higher. Link: https://www.microsoft.com/net/download?initial-os=linux
* Make sure your code/project compiles successfully. There shouldn't be any compile errors (But there could be warnings, they are fine).
* Backup your script files or whole project. Auto Input Manager is going to make minor changes to your script files and these changes are not revertible. Committing to repository or copying files is highly recommended before using Auto Input Manager.


How to Use:
* Copy folder under "Assets"
* Choose "Tools"->"Auto Input Manager" from menu
* Click "Enchant" button
* Click "I Made a Backup. Go Ahead!" button
* Wait. Do not alt-tab, do not go back to scene or inspector window, or do not edit anything
* Click "Exit" button


How to Use (Detailed):
* Copy "AutoInputManager" folder (with all subfolders and files) under "Assets" folder (or any subfolder of "Assets" folder) of your project(s).
* In the Unity Editor choose "Tools" menu from the menu bar then choose menu item of "Auto Input Manager".
* A new window with title "Auto Input Mng" should open. Wait for a few seconds for initialization.
* Choose "Advanced" if you want to omit some files, though it is not recommended.
* Choose "Enchant" to advance.
* Choose "I Made a Backup. Go Ahead!" to begin Auto Input Manager. From now on your source files may change, because of that it is strongly recommended making a backup or committing your changes.
* Auto Input Manager will iterate over your source files, progress will be visible on the window. During this time, do not alt-tab, do not go back to scene or inspector window, or do not edit anything
* "Pause" button will pause the progress, and "Resume" will continue.
* "Exit" button will appear when Auto Input Manager finishes, click it to close the window. Results will appear on the window.


Options:
Options can be changed by editing file "EnchanterOptions.txt". Default options should be sufficient for most users, changing options is not recommended.
* IsLoggingToFile {boolean}: Standard file logging.
* LogFileName {string}: File name of default logging, the file is placed under asset directory.
* IsLoggingAll {boolean}: Logs all available data. Useful when an error occurs, default is off.
* IsPreCompiling {boolean}: Checks if code compiles before enchanting the code. Useful for preventing malfunction.
* IsPostCompiling {boolean}: Checks if code compiles after code has been enchanted. Useful when an error occurs while enchanting.


Thank you for using Auto Input Manager. We hope this will you in your crazy journey called game development :)