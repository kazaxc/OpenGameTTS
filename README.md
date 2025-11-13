<br/>
<div align="center">
    <img src="/TTSGameOverlay/logo-512.png" alt="Logo" width="128" height="128">
</div>

<h3 align="center">Open Game TTS Overlay</h3>
<p align="center">
    Generate TTS for use in voice proximity games, Discord, etc.
    <br/>
    <a href="https://github.com/kazaxc/OpenGameTTS/releases"><strong>Download Here</strong></a>
</p>


## About

Open Game TTS Overlay is an overlay that you run on your games that allows you to input text that you wish to be TTS, the output is played back to you and pushed through [VB-Audio Virtual Cable](https://vb-audio.com/Cable/) for the ability to talk through programs such as discord or used in games with voice proximity. The project is heavily inspired by Sea Of Thieves and R.E.P.O. which include a native TTS feature for their in-game chats, the idea is to make proximity chat games more accessible to those who do not use a microphone. 

<div align="center">
    <img src="CurrentAppDesign.png" alt="Current application design">
</div>

Uses Microsoft speech synthesis - Any language packs or SAPI5 voices you have installed on windows are available for you to use. If you want additional voices follow [this](https://support.microsoft.com/en-gb/topic/download-languages-and-voices-for-immersive-reader-read-mode-and-read-aloud-4c83a8d8-7486-42f7-8e46-2b0fdf753130) guide by Microsoft.

Early stages work in progess but it is fully functional, the code is just not very clean and the project layout needs some work along with a few extra features. Major refactor coming soon.

### Built with

* [WinForms](https://github.com/dotnet/winforms)
* [NAudio](https://github.com/naudio/NAudio)
* [VB-Audio Virtual Cable](https://vb-audio.com/Cable/)
* [Microsoft Speech System](https://learn.microsoft.com/en-gb/previous-versions/windows/desktop/ms723627(v=vs.85))


## Getting started

### Prerequisits
You must download [VB-Audio Virtual Cable](https://vb-audio.com/Cable/) as this is how speech is transmitted to your desired outputs.

### Installation
1. Download the latest [latest release](https://github.com/kazaxc/OpenGameTTS/releases).

2. Set VB-Audio Virtual Cable as your input device on desired applications.

3. Run the executable and start typing, its that easy.

### Hotkeys
Focus on overlay:
    ```CTRL + ENTER```

Exit:
    ```ESC```

## Coming Soon
A major refactor of the project is currently underway to move away from Win Forms as this was only to get an initial working build over to [Avalonia UI](https://avaloniaui.net/). In the process of this change more features will be added such as persitant settings changes, hotkey rebinding and much more.