<?php

/* Copyright (c) 1998-2010 ILIAS open source, Extended GPL, see docs/LICENSE */

include_once("./Services/UIComponent/classes/class.ilUIHookPluginGUI.php");
//include_once("./Services/JSON/classes/class.ilJsonUtil.php");

/**
 * User interface hook class
 *
 * @author Stefan Schneider <schneider@hrz.uni-marburg.de>
 * @version $Id$
 * @ingroup ServicesUIComponent
 */
class ilSEBUIHookGUI extends ilUIHookPluginGUI {
	
	const NOTHING_TO_DETECT = 0;
	const NOT_A_SEB_REQUEST = 1;
	const SEB_REQUEST = 2;
	
	const ROLES_NONE = 0; // not used
	const ALL_ROLES_EXPECT_ADMIN = 1; // not used
	
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
		//print($this->getFullUrl());
		$q = "SELECT * FROM ui_uihk_seb_conf";
		$ret = $ilDB->query($q);
		$rec = $ilDB->fetchAssoc($ret);
		$url_salt = $rec["url_salt"];
		$req_header = $rec["req_header"];
		$seb_key = $rec["seb_key"];
		$role_id = $rec["role_id"];
		$lock_role = $rec["lock_role"];
		$kiosk = $rec["kiosk"];
		
		$ret = array("role_id" => $role_id, "lock_role" => $lock_role, "kiosk" => $kiosk);
		
		if ($url_salt) {
			$url = strtolower($this->getFullUrl());
			$ctx = hash_init('sha256');
			hash_update($ctx, $url);
			hash_update($ctx, $seb_key);
			$seb_key = hash_final($ctx);
		}				
				
		// if no seb_key or request header is configured there is nothing to be detected
		if ($req_header == "") {
			$ret["request"] = self::NOTHING_TO_DETECT; // nothing to detect
			return $ret; 
		}
		if ($seb_key == "") {
			$ret["request"] = self::NOTHING_TO_DETECT; // nothing to detect
			return $ret;
		}
		
		$server_req_header = $_SERVER[$req_header];
		//print $server_req_header . "<br />";
		//print $seb_key . "<br />";
		
		// print $server_req_header . "<br/>" . $seb_key;
		// ILIAS want to detect a valid SEB with a custom req_header and seb_key
		// if no req_header exists in the current request: not a seb request
		if (!$server_req_header || $server_req_header == "") {			
			$ret["request"] = self::NOT_A_SEB_REQUEST; // not a seb request
			return $ret;
		}
		
		// if the value of the req_header is not the the stored or hashed seb key: // not a seb request
		if ($server_req_header != $seb_key) {
			$ret["request"] = self::NOT_A_SEB_REQUEST; // not a seb request
			return $ret;
		}
		else {
			$ret["request"] = self::SEB_REQUEST; // seb request
			return $ret;
		}
	}
	
	function getSebObject() { // obsolet?
		global $ilUser;
		$pl = $this->getPluginObject();
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
		return $ret;
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
		global $ilUser, $rbacreview, $styleDefinition, $ilAuth;
		/*
		if (($a_part == "sub_tabs" || $a_part == "tabs") && $_GET["baseClass"] == "ilrepositorygui") {
			
		}*/
		if ($a_comp == "Services/Init" && $a_part == "init_style") {			
			$req = $this->detectSeb();
			//print_r($req);
			
			if ($req["request"] == self::NOTHING_TO_DETECT) {
				self::$_modifyGUI = 0;
				return;
			}
			if ($req["request"] == self::NOT_A_SEB_REQUEST) {
				if ($req["lock_role"] && !$rbacreview->isAssigned($ilUser->getId(),2)) {					
					if ($rbacreview->isAssigned($ilUser->getId(),$req["role_id"])) {
						ilSession::setClosingContext(ilSession::SESSION_CLOSE_LOGIN);
						$ilAuth->logout();
						session_unset();
						session_destroy();
						$script = "login.php?target=".$_GET["target"]."&client_id=".$_COOKIE["ilClientId"];
						ilUtil::redirect($script);
						return;
					}
				}
			}
			if ($req["request"] == self::SEB_REQUEST && $req["kiosk"]) {					
				if (!$rbacreview->isAssigned($ilUser->getId(),2)) { // maybe admins want to test the kiosk mode?
					// with seb request the mapped user role, anonymous and for the login site ($ilUser->getId() = 0) set seb skin					
					if (!$ilUser->getId() || $ilUser->getId() == ANONYMOUS_USER_ID || $rbacreview->isAssigned($ilUser->getId(),$req["role_id"])) {
						self::$_modifyGUI = 1;
						$styleDefinition->setCurrentSkin("seb");
						$styleDefinition->setCurrentStyle("seb");
					}
				}
			}
		}
	}
}
?>
