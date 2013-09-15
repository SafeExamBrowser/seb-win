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
	const NOT_A_SEB_REQUEST = 0;
	const SEB_REQUEST = 1;
	const ROLES_NONE = 0;
	const ROLES_ALL = 1;
	const BROWSER_KIOSK_ALL = 0;
	const BROWSER_KIOSK_SEB = 1;
	
	public static function _isAPCInstalled() {
		//$ret = return 1;
		return (function_exists("apc_store") && function_exists("apc_fetch"));
	}
	
	function getPluginName() {
		return "SEB";
	}
	
}

?>
