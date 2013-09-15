<?php

/* Copyright (c) 1998-2010 ILIAS open source, Extended GPL, see docs/LICENSE */

include_once("./Services/UIComponent/classes/class.ilUIHookPluginGUI.php");
include_once("class.ilSEBPlugin.php");
//include_once("./Services/JSON/classes/class.ilJsonUtil.php");

/**
 * User interface hook class
 *
 * @author Stefan Schneider <schneider@hrz.uni-marburg.de>
 * @version $Id$
 * @ingroup ServicesUIComponent
 */
class ilSEBUIHookGUI extends ilUIHookPluginGUI {
	
	private static $_modifyGUI = 0;
	
	function getFullUrl() {
		$s = empty($_SERVER["HTTPS"]) ? '' : ($_SERVER["HTTPS"] == "on") ? "s" : "";
		$sp = strtolower($_SERVER["SERVER_PROTOCOL"]);
		$protocol = substr($sp, 0, strpos($sp, "/")) . $s;
		$port = ($_SERVER["SERVER_PORT"] == "80" || $_SERVER["SERVER_PORT"] == "443") ? "" : (":".$_SERVER["SERVER_PORT"]);
		return $protocol . "://" . $_SERVER['SERVER_NAME'] . $port . $_SERVER['REQUEST_URI'];
	}
	
	function detectSeb() {
		global $ilDB; // ToDo Caching of settings in APC
		//if (ilSEBPlugin::)
		//print($this->getFullUrl());
		$rec;
		if (ilSEBPlugin::_isAPCInstalled() && apc_exists("SEB_CONFIG_CACHE")) {
			$rec = apc_fetch("SEB_CONFIG_CACHE");
			//var_dump($ret);
		}
		else {
			$q = "SELECT * FROM ui_uihk_seb_conf";
			$ret = $ilDB->query($q);
			$rec = $ilDB->fetchAssoc($ret);
		}
		
		/*
		$url_salt = $rec["url_salt"];
		$req_header = $rec["req_header"];
		$seb_key = $rec["seb_key"];
		$role_deny = $rec["role_deny"];
		$browser_access = $rec["browser_access"];
		$role_kiosk = $rec["role_kiosk"];
		$browser_kiosk = $rec["browser_kiosk"];
				
		$ret = array("role_deny" => $role_deny, "browser_access" => $browser_access, "role_kiosk" => $role_kiosk, "browser_kiosk" => $browser_kiosk);
		*/
		
		if ($rec["url_salt"]) {
			$url = strtolower($this->getFullUrl());
			//$ctx = hash_init('sha256');
			//hash_update($ctx, $url.$rec["seb_key"]);
			//hash_update($ctx, $rec["seb_key"]);
			//$rec["seb_key"] = hash_final($ctx);
			$rec["seb_key"] = hash('sha256',$url . $rec["seb_key"]);
			//print $url . "<br/>";
			//print $rec["seb_key"];
		}				
				
		$server_req_header = $_SERVER[$rec["req_header"]];
		// print "<br/>req_header: " . $server_req_header;		
		// ILIAS want to detect a valid SEB with a custom req_header and seb_key
		// if no req_header exists in the current request: not a seb request
		if (!$server_req_header || $server_req_header == "") {		
			$rec["request"] = ilSebPlugin::NOT_A_SEB_REQUEST; // not a seb request
			return $rec;
		}
		
		// if the value of the req_header is not the the stored or hashed seb key: // not a seb request
		if ($server_req_header != $rec["seb_key"]) {
			$rec["request"] = ilSebPlugin::NOT_A_SEB_REQUEST; // not a seb request
			return $rec;
		}
		else {
			$rec["request"] = ilSebPlugin::SEB_REQUEST; // seb request
			return $rec;
		}
	}
	
	function getSebObject() { // obsolet?
		global $ilUser;
		$pl = $this->getPluginObject();
		$ret = "{}"; // for further use
		/*
		$ret = "{
				user: {
					login:'".$ilUser->getLogin()."',
					firstname:'". $ilUser->getFirstname() ."',
					lastname:'".$ilUser->getLastname()."',
					matrikel:'".$ilUser->getMatriculation()."'
				},
				logo : {
					link: './ilias.php?baseClass=ilPersonalDesktopGUI&cmd=jumpToSelectedItems'  
				}
			}";
			*/
		return $ret;
	}
	 
	function exitIlias() {
		global $ilAuth;
		ilSession::setClosingContext(ilSession::SESSION_CLOSE_LOGIN);
		$ilAuth->logout();
		session_unset();
		session_destroy();
		$script = "login.php?target=".$_GET["target"]."&client_id=".$_COOKIE["ilClientId"];
		ilUtil::redirect($script);				
	}
	
	function setSebGUI () {
		global $styleDefinition;
		self::$_modifyGUI = 1;
		$styleDefinition->setCurrentSkin("seb");
		$styleDefinition->setCurrentStyle("seb");
	}
	
	function setUserGUI () {
		global $styleDefinition, $ilUser;
		self::$_modifyGUI = 0;
		$styleDefinition->setCurrentSkin($ilUser->getPref("skin"));
		$styleDefinition->setCurrentStyle($ilUser->getPref("style"));
	}
	/**
	 * Modify HTML output of GUI elements. Modifications modes are:
	 * - ilUIHookPluginGUI::KEEP (No modification)
	 * - ilUIHookPluginGUI::REPLACE (Replace default HTML with your HTML)
	 * - ilUIHookPluginGUI::APPEND (Append your HTML to the default HTML)
	 * - ilUIHookPluginGUI::PREPEND (Prepend your HTML to the default HTML)
	 *
	 * @param string $a_comp component
	 * @param string $a_part string that identifies the part of the UI that is handled
	 * @param string $a_par array of parameters (depend on $a_comp and $a_part)
	 *
	 * @return array array with entries "mode" => modification mode, "html" => your html
	 */
	function getHTML($a_comp, $a_part, $a_par = array()) {		
		global $ilUser, $rbacreview, $tpl;
		
		if (!self::$_modifyGUI) {
			return array("mode" => ilUIHookPluginGUI::KEEP, "html" => "");
		}			
		
		if ($a_comp == "Services/MainMenu" && $a_part == "main_menu_list_entries") {		
			$pl = $this->getPluginObject();
			$tpl->addJavaScript($pl->getDirectory() . "/ressources/seb.js");
			$seb_object = $this->getSebObject(); 
			return array("mode" => ilUIHookPluginGUI::REPLACE, "html" => "<script type=\"text/javascript\">var seb_object = " . $seb_object . ";</script>");
		}
		if ($a_comp == "Services/MainMenu" && $a_part == "main_menu_search") {		
			return array("mode" => ilUIHookPluginGUI::REPLACE, "html" => "");			
		}
		
		if ($a_comp == "Services/Locator" && $a_part == "main_locator") {			
			return array("mode" => ilUIHookPluginGUI::REPLACE, "html" => "");
		}
		
		if ($a_comp == "Services/PersonalDesktop" && $a_part == "right_column") {
			return array("mode" => ilUIHookPluginGUI::REPLACE, "html" => "");
		}
		
		if ($a_comp == "Services/PersonalDesktop" && $a_part == "left_column") {			
			return array("mode" => ilUIHookPluginGUI::REPLACE, "html" => "");
		}
		return array("mode" => ilUIHookPluginGUI::KEEP, "html" => "");
	}
	
	/**
	 * Modify GUI objects, before they generate ouput
	 *
	 * @param string $a_comp component
	 * @param string $a_part string that identifies the part of the UI that is handled
	 * @param string $a_par array of parameters (depend on $a_comp and $a_part)
	 */
	function modifyGUI($a_comp, $a_part, $a_par = array()) {
		global $ilUser, $rbacreview, $ilAuth;
		if ($a_comp == "Services/Init" && $a_part == "init_style") {			
			$req = $this->detectSeb();
			//print_r($req);
			$usr_id = $ilUser->getId();
			$is_admin = $rbacreview->isAssigned($usr_id,2);
			$is_logged_in = ($usr_id && $usr_id != ANONYMOUS_USER_ID);
			$deny_user = false;
			$role_deny = $req['role_deny'];
			// check role deny			
			if ($is_logged_in && $role_deny && !$is_admin) { // check access 				
				$deny_user = ($role_deny == 1 || $rbacreview->isAssigned($usr_id,$role_deny));
			}
			
			// check browser access
			$browser_access = $req['browser_access'];					
			$is_seb = ($req['request'] == ilSebPlugin::SEB_REQUEST);
			$allow_browser = ($browser_access && $is_seb);
			
			if ($deny_user && !$allow_browser) {
				$this->exitIlias();
				return;
			}
				
			// check kiosk mode
			$role_kiosk = $req['role_kiosk'];
			$user_kiosk = false;
			$browser_kiosk = $req['browser_kiosk'];
			$kiosk_user = (($role_kiosk == 1 || $rbacreview->isAssigned($usr_id,$role_kiosk)) && !$is_admin);
			
			if ($is_logged_in) {				
				$switchToSebGUI = false;
				if ($kiosk_user) {
					switch ($browser_kiosk) {
						case ilSebPlugin::BROWSER_KIOSK_ALL :
							$switchToSebGUI = true;
							break;
						case ilSebPlugin::BROWSER_KIOSK_SEB :
							$switchToSebGUI = $is_seb;
							break;
					}
					if ($switchToSebGUI) {
						$this->setSebGUI();
					}
					else {
						$this->setUserGUI();
					}							
				}
				else {
					$this->setUserGUI();
				}
			}
			else { 			
				$switchToSebGUI = false;
				if ($role_kiosk) {
					switch ($browser_kiosk) {
						case ilSebPlugin::BROWSER_KIOSK_ALL :
							$switchToSebGUI = true;
							break;
						case ilSebPlugin::BROWSER_KIOSK_SEB :
							$switchToSebGUI = $is_seb;
					}
					if ($switchToSebGUI) {
						$this->setSebGUI();
					}
					else {
						$this->setUserGUI();
					}
				}
			}
		}
	}
}
?>
