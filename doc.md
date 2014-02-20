Documentation
=============

## Getting Started ##
Please check the **Requirement** section in the README.md first.
Have a look at the seb config files: ``` seb/browser/apps/chrome/defaults/seb/config.PRESET.json ```
and the startup scripts *.sh or *.bat: ``` seb/browser/bin/OS/ ```

* ``` -config "debug" ``` loads ``` seb/browser/apps/chrome/defaults/seb/config.debug.json ``` which enables a titlbar and disables the locking for the main seb window.
* ``` -config "demo" ``` loads ``` seb/browser/apps/chrome/defaults/seb/config.demo.json ``` which enables a local websocket server and the screenshot controller.
* omitting the ```-config``` param will load ``` seb/browser/apps/chrome/defaults/seb/config.json ```
* ``` -configpath "PATH" ``` loads a config file from local filesystem or http|file urls (``` -config "PRESET" ``` will be ignored).
* The commandline option ``` -ctrl 1 ``` or ``` -ctrl "ewoic3RhcnR....." ``` can be used to assign control params at runtime. 
This is currently used by the SEB 2.0 windows host application. Setting ``` -ctrl 1 ``` will use an OS specific controller config ``` seb/browser/apps/chrome/defaults/seb/winctrl.json | linuxctrl.json ``` (this is just used for debugging purposes).
The windows seb host application assigns the config params directly as base64 encoded json string ``` -ctrl -ctrl "ewoic3RhcnR....." ``` . 
The corresponding controller mappings are defined in the new jsm modules: ``` seb/browser/apps/modules/winctrl.jsm | linuxctrl.jsm ``` . 
The params can be directly mapped to a seb config param like ``` "seb.url" : "startURL" ``` , or to a function like ``` "seb.mainWindow.screen"	: mainWindowScreen ``` .
If a windows mapping param exists, its appended to the param section in the following documentation.

## Debugging ##
The ``` *_debug ``` startup script opens an additional debug window and loads debug preferences for xulrunner (see: ``` seb/browser/apps/chrome/defaults/seb/preferences/debug.js ```)

## config.json ##

### preferences ###
In the preferences section you can add any xulrunner preference (see http://kb.mozillazine.org/About:config_entries, beware that some config entries are only supported by firefox not xulrunner).
``` 
"prefs": {
  "general.useragent.override" : "SEB"
},
```
ILIAS or Moodle are still using a special user-agent key for access control. 
! The "general.useragent.override" entry overrides the **whole** user-agent string on every request, so some web applications with browser detection might be confused (switch to a mobile version or display the wrong css rules).
The mechanism should be replaced by a custom request header (see next).

### request header / request value ###
```
"seb.request.header"			: "X-SafeExamBrowser",
"seb.request.value"			: "SEB",
```
The request header and value are sent on every request and can be used to customize the behavior of the web application. 
A common application for this config is the assignment of SEB requests to standard user access and / or a special KIOSK mode.

### url ###
The most important field is the autostart **URL** of the embedded Safe-Exam-Browser (SEB). It might be usefull to extend the url execution by other applications like java webstarter.
```
"seb.url" : "http://safeexambrowser.org",
```

### titlebar ###
```
 "seb.mainWindow.titlebar.enabled" : false,
 "seb.popupWindows.titlebar.enabled" : true,
```
enables/disables the titlebar of the main window and the popup windows. 
On locked = true the quit button on the mainWindow title bar is deactivated. Unfortunately hiding the quit button is not possible.

### window position and size ###
```
"seb.mainWindow.screen"	: {
 "sizemode" : "full", 
 "width" : 0,
 "height" : 0,
 "position" : "left" 
},
"seb.popupWindows.screen" : {
 "sizemode" : "relative", 
 "width" : 50,
 "height" : 0,
 "position" : "right",
 "offset" : 40 
},
```
```"sizemode" : "full"``` = fullscreen mode, positioning is ignored (only mainWindow)
```"sizemode" : "relative"``` = width and height are interpreted as percentage values relative to the available screen size.
```"sizemode" : "absolute"``` = width and height are interpreted as pixel values
```"width"```, ```"height"``` = dependant on sizemode. 0 means full width or height (relative and absolute)
```offset``` = popups will be positioned with an offset (pixel) to previous popups (only popupWindows)

If your exam needs two or more independant secure browser splitted on the screen take a look to some example start files:

```start_demo.left.*``` ```start_demo.right.*``` and a ```start_demo.split.*``` which starts the two files in one single command 

The corresponding configs: 
``` seb/browser/apps/chrome/defaults/seb/config.demo.left.json ```
``` seb/browser/apps/chrome/defaults/seb/config.demo.right.json ```

### blacklists / whitelists ###
```
"seb.trusted.content" : true,
"seb.pattern.regex" : false,
"seb.blacklist.pattern" : "",
"seb.whitelist.pattern" : "",
```
To control the network traffic you can define blacklist and whitelist patterns. 

```
"seb.trusted.content" : true,
```
If **"seb.trusted.content"** is set to **true** the defined blacklist and whitelist pattern will only check the main  url. 
If **"seb.trusted.content"** is set to **false** the pattern will check **every** embedded resources (js, css, images...). 
Check the debug mode and try this setting:
```
"seb.url" : "http://eqsoft.org/sebian/",
"seb.trusted.content" : false,
"seb.pattern.regex" : false,
"seb.blacklist.pattern" : "*/sebian.png",
"seb.whitelist.pattern" : "",
```

You should see a message in the error console that sebian.png is not allowed and it will not be loaded.
The blacklist is a list of comma seperated url pattern for disallowed ressources and will be first executed.
The whitelist is a list of comma seperated url pattern for explicitly allowed ressources. All other ressources will be denied.

If you set **"seb.pattern.regex"** to **true** the blacklist and whitelist pattern will be interpreted as regular expressions. 
If you are not sure about regular expressions keep the default setting.

### locking ###
```
 "seb.locked" : true,
```
The default setting is a locked SEB. That means if you enable the main window titlebar or a shutdown key this will not work until you **unlock** SEB. An unlocked SEB can be relocked with:
```
"seb.lock.keycode" : "VK_F2",
"seb.lock.modifiers" : "control shift",
```

### unlocking ###
```
 "seb.unlock.enabled" : false,
```
If you enable the unlock function you can unlock a locked SEB:
```
 "seb.unlock.keycode" : "VK_F3",
 "seb.unlock.modifiers" : "control shift",
```

### shutdown ###
An unlocked SEB can be shutdown with: 
```
 "seb.shutdown.keycode" : "VK_F4",
 "seb.shutdown.modifiers" : "control shift",
```
or if you enabled the titlebar of the main window you can shutdown SEB with the default window close control of the titlebar.

With a shutdown url, seb can be forced to quit by calling a special embedded url, locking will be ignored. 
```
"seb.shutdown.url" : "http://seb/shutdown",
"seb.shutdown.warning" : true,
```

### loading alternative page ###
```
"seb.load" : "",
"seb.load.referrer.instring" : "",
"seb.load.keycode" : "VK_F6",
"seb.load.modifiers" : "control shift",
```
The navigation in a kiosk browser is restricted without any addressbar or bookmarks. you might need jumping from a special url (**"seb.load.referrer.instring":"CHECK_IF STRING_IN_CURRENT_URL"** to another url (**"seb.url" : "JUMP_TO_ME"**). If you press the defined hot key SEB checks if you defined a target url in **"seb.url"** and if the current url contains the **"seb.load.referrer.instring"**.

### reload ###
```
 "seb.reload.keycode" : "VK_F5",
 "seb.reload.modifiers" : "control shift",
```

Sometimes a reload of the browsers page is required. The default setting to reload a page in SEB is **ctrl-shift-F5** If you want to get the normal browser behavior (only **F5**) you can set the modifiers string **"control shift"** to an empty string **""**:

```
"seb.reload.keycode" : "VK_F5",
"seb.reload.modifiers" : "",
```

### connection timeout ###
```  
"seb.net.tries.enabled" : false,
"seb.net.max.times" : 3,
"seb.net.timeout" : 10000,
```
Sometimes network connection errors were reported after boot process of sebian. Before loading the url and with net.tries enabled the browser tries to get a response from the start page with a delay of **"seb.net.timeout"** ms. If this failes a blue page will appear and if the **"seb.restart.mode"** > 0 (see next paragraph) and a **"seb.restart.key"** is defined, you can reload the start url manually with that hotkey.
   
### restart mode of the SEB url ###
```
"seb.restart.mode" : 2,
"seb.restart.keycode" : "VK_F9",
"seb.restart.modifiers" : "control shift",
```
**"seb.restart.mode"** = 0: manually restart is deactivated
**"seb.restart.mode"** = 1: manually restart is only enabled if the initial conection failed and a blue page appears.
**"seb.restart.mode"** = 2: manually restart is always enabled

### navigation ###
```
"seb.navigation.enabled" : false,
"seb.back.keycode" : "VK_LEFT"
"seb.back.modifiers" : "control",
"seb.forward.keycode" : "VK_RIGHT",
"seb.forward.modifiers" : "control",
"seb.bypass.cache" : true,
```
With **"seb.navigation.enabled"** enabled you can navigate throw the browser history with assigned keys and key modifier. **"seb.bypass.cache"** controls the response caching, you have to check this for your environment.

```
"seb.showall.keycode" : "VK_F1",
```
If popup windows will lost their focus, they disappear behind the main window. To get them all back into the front you can define a **"seb.showall.keycode"** hotkey. Maybe a sort of taskbar to call a single popup window would be a good idea for a further implementation.

```
"seb.distinct.popup" : true,
```
New windows opened with target="_blank" don't know each others, so multiple link requests will open multiple windows with the same url. To avoid this you can set a **"seb.distinct.popup"** flag. 
New windows with the same url of an already open window will be supressed and the first window instance is focused.

```
"seb.alert.controller" : true,
```
To avoid the default title bar of alert dialogs which always contains the url of the web application (!), you can set the **"seb.alert.controller" : true**. The title will be replaced with "alert".

### remove profile ###
```
"seb.removeProfile" : false
```
The default SEB setting does not remove the xulrunner profile after shutdown. You have to check this feature for your environment.

### screenshot and demo server (experimental) ###
Please read the **Requirements** on the README.md
```
"seb.server.enabled" : true,
"seb.server" :  {
  "url"    : "https://localhost:8443/websocket",
  "socket" : "wss://localhost:8443/websocket"
}
"seb.togglehidden.enabled"		: true,
"seb.togglehidden.keycode"		: "VK_F10",
"seb.togglehidden.modifiers"		: "",
```
The seb server prototype based on node.js is a backend component to serve special requirements like the generation of screenshots. 
We are planing to extend the component with other administrative features. 
With **"seb.server.enabled" : true** an independant browser component connects to a secure websocket server **"url" : "https://localhost:8443/websocket"** and keeps a websocket connection **"wss://localhost:8443/websocket"** for the whole browser session.
With **"seb.togglehidden.enabled" : true** you can toggle the main SEB window and the server window, where asynchronous websocket messages will be displayed.

```
"seb.screenshot.controller" : true, 
"sc.image.mimetype" : "image/jpeg",
"sc.sound" : true,
```
With **"seb.screenshot.controller" : true** SEB will provide a function **seb_ScreenShot** in the DOM of the window from where the web application may trigger a screenshot of the current page.

```
file = {
	path : [folder1, folder2, ...],
	filename : "filename"
};
seb_ScreenShot(file, window);

```
The screenshots will be stored on the server:
```
seb/server/websocket/data/folder1/folder2/filename.jpg
```
**"sc.image.mimetype" : "image/jpeg"** is the default mimetype. 
With **"sc.sound" : true** the screenshot will trigger a snapshot sound. 
Check the demo and the third party ILIAS sample code.

## Screenshot Demo ##
After installing node.js and the required modules you need to start ```./server.sh``` on Linux or ```server.bat``` on windows in the ```seb/server/``` folder.
There should be 2 server listening on port 8443 (seb server) and 8442 (monitor server).
Now you can start a seb browser with a demo config ```linux32_demo.sh``` or
```win_demo.bat``` in the ```seb/browser/``` folder. You can play around with the corresponding config file in ```seb/browser/apps/chrome/defaults/seb/config.demo.json```
On the seb start screen edit the ```test id```  and ```user id```. Your screenshots will be stored in ```seb/server/websocket/data/TESTID/USERID/SEQUENCEID_QUESTIONID_TIMESTAMP.jpg|png``` 


## Third Party ##
To use ILIAS with a SEB screenshot controller you can find a patch in ```seb/thirdparty/ilias```
The patch will notify an existing SEB controller and hooks into the submit event of the question formular. The hook script send meta information like the id of the test, user, question and sequence and a window handle to the screenshot controller. SEB will perform a screenshot of the window DOM and send the binary screenshot data through a persistant secure websocket connection to the server (port 8443). The server stores the screenshot into ```seb/server/websocket/data/TESTID/USERID/SEQUENCEID_QUESTIONID_TIMESTAMP.jpg|png```

## Security: ssl and client certificates ##
The demo version provides a CA and two client certificates to demonstrate the usage of ssl certificates: 
```
seb/server/ssl/ca.crt
seb/server/ssl/user.p12
seb/server/ssl/admin.p12
```
The CA and the signed user.p12 client cert are embedded into the browser component per default. The seb websocket server will request a client certificate and validates the cert against the CA. Any unauthorized clients will be rejected.

## Monitoring (experimental) ##
A monitor server is started on a seperate port (8442). To check the experimental features of the seb client monitor you have to import the ca.crt and admin.p12 (see **Security: ssl and client certificates**) into your browser. Now you can connect to the monitor admin site ```https://localhost:8442/websocket/monitor.html```. Any seb client connection will be listed in the table data. Check the send message and shutdown buttons. The demo should only demonstrate the potential usage of remote controlling and monitoring connected seb clients.
  
