<?php

include_once("./Services/UIComponent/classes/class.ilUserInterfaceHookPlugin.php");
 
/**
 * Example user interface plugin
 *
 * @author Alex Killing <alex.killing@gmx.de>
 * @version $Id$
 *
 */
class ilSEBPlugin extends ilUserInterfaceHookPlugin
{
	function getPluginName()
	{
		return "SEB";
	}
}

?>
