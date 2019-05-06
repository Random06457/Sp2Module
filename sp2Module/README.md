# Sp2Module
This contains the c++ code that'll run with the game.

This is currently just contains Pong as a POC that works on the version 2.3.1 of Splatoon 2. Preview here : https://twitter.com/random666_kys/status/1124817347414511622

# Warning
This project isn't for end users, any code modification will ban you on the latest versions of Splatoon if you go online

# Setup
- Grab `Sp2Patcher.exe` and `LZ4.dll` in the release section and copy them in `sp2Module/`
- Create the folders `sp2Module/sp2` and `sp2Module/sp2_out` and copy your game's exefs content in `sp2`
- Code whatever you want

# Building
After editing `sp2Module/patchConfig.txt`, run `make` and everything will be output in `sp2Module/sp2_out`
