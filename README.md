Standalone Browser Component from Safe-Exam-Browser
=================================================== 
See: http://www.safeexambrowser.org


## OS Support ##
* Windows 32/64Bit
* Linux   32/64Bit

## Requirements ##
Download the latest xulrunner runtime for your OS from:
http://ftp.mozilla.org/pub/mozilla.org/xulrunner/releases/ 

Create a folder structure for the xulrunner runtime(s) like that:

``` 
+ seb (git repository)
- xulrunner
   | - win (shared binary for 32 and 64Bit)
   |     | - xulrunner (p.e. unzipped xulrunner-19.0.en-US.win32.zip)
   |     |    | * 
   | - linux
   |     | - 32
   |     |    | - xulrunner (p.e. unzipped xulrunner-19.0.en-US.linux-i686.tar.bz2)
   |     |    |    | *    
   |     | - 64
   |     |    | - xulrunner (p.e. unzipped xulrunner-19.0.en-US.linux-x86_64.tar.bz2)
   |     |    |    | *
``` 

The start scripts **must** point to an executable xulrunner binary.
Feel free to customize the folder structure and the startup script commands.

``` 
- seb (git repository)
   | - browser
   |    | linux32.sh 
   |    | linux32_debug.sh
   |    | linux64.sh
   |    | linux64_debug.sh
   |    | win.bat
   |    | win_debug.bat   
+ xulrunner
``` 

For demo mode including the screenshot component (pre alpha) you need to install node.js (http://nodejs.org/).
After installing node.js you need to install some node modules:
``` 
npm install fs-extra binaryjs express forever forever-monitor -g 
``` 
Change into the ``` seb/server/ ``` folder and type:
```
start.sh (Linux) or start.bat (Windows)

If you get error messages that node can not find the modules try to set an environment variable in ``` ~/.bashrc ``` or ``` ~/.profile ``` p.e.: ``` export NODE_PATH=/usr/lib/node_modules ```

