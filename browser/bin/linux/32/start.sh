#!/bin/sh

# -configpath can be a local path like /home/xxxx/config.json or any url like file://*, http://* or https://*
# the following command will load a config.json file in the current directory
#currDir=`pwd`
#../../xulrunner/linux/32/xulrunner/xulrunner -app "apps/seb.ini" -configpath "$currDir/config.json"

# the following command will load a config preset like "demo" or "debug" (see apps/chrome/defaults/seb/config.PRESET.json)
../../../../../xulrunner/linux/32/xulrunner/xulrunner -app "../../../apps/seb.ini" -profile "../../../data/profile"
