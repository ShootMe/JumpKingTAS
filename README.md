# JumpKingTAS
Simple TAS Tools for the game Jump King

## Installation

- Currently only works/tested for the Windows version.
- Go to [Releases](https://github.com/ShootMe/JumpKingTAS/releases)
- Download JumpKingAddons.dll
- You will need the modified JumpKing.exe as well. I wont host it here.
  - You can modify it yourself with dnSpy or similar
  - This is something you have to figure out if you plan to do it, I currently don't have the time to go through every step
  - Follow the general instructions with this [Modified](https://github.com/ShootMe/JumpKingTAS/blob/master/Game/WhatsModified.txt)

## Input File
Input file is called JumpKing.tas and needs to be in the main JumpKing directory (usually C:\Program Files (x86)\Steam\steamapps\common\JumpKing\JumpKing.tas)

Format for the input file is (Frames),(Actions)

ie) 35,R,J (For 35 frames, hold Right and Jump)

## Actions Available
- R = Right
- L = Left
- U = Up
- D = Down
- J = Jump / Confirm
- P = Pause
- C = Cancel
- X = Reset State

## Special Input
- You can create a break point in the input file by typing *** by itself on a single line
- The program when played back from the start will try and go at 400x till it reaches that line and then go into frame stepping mode
- You can also specify the speed with ***X where X is the speedup factor. ie) ***10 will go at 10x speed

## Playback of Input File
### Controller
While in game
- Playback: Right Stick
- Stop: Right Stick
- Faster Playback: Right Stick X+
- Frame Step: DPad Up
- While Frame Stepping:
  - One more frame: DPad Up
  - Back a frame: DPad Down
  - Continue at normal speed: Left Stick
  - Frame step continuously: Right Stick X-/X+
 
## Jump King Studio
Can be used instead of notepad or similar for easier editing of the TAS file. Is located in [Releases](https://github.com/ShootMe/JumpKingTAS/releases) as well.

If JumpKing.exe is running it will automatically open JumpKing.tas if it exists. You can hit Ctrl+O to open a different file, which will automatically save it to Celeste.tas as well. Ctrl+Shift+S will open a Save As dialog as well.
