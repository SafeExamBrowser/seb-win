<?php

/* Copyright (c) 1998-2010 ILIAS open source, Extended GPL, see docs/LICENSE */

include_once("./Services/UIComponent/classes/class.ilUIHookPluginGUI.php");
include_once("./Services/JSON/classes/class.ilJsonUtil.php");
/**
 * User interface hook class
 *
 * @author Alex Killing <alex.killing@gmx.de>
 * @version $Id$
 * @ingroup ServicesUIComponent
 */
class ilSEBUIHookGUI extends ilUIHookPluginGUI {
	function checkSeb() {
		
	}
	
	function getSebObject() {
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
		if ($rbacreview->isAssigned($ilUser->getId(),2)) {
			//$gr = $rbacreview->getGlobalRoles();
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
		global $ilUser, $rbacreview, $styleDefinition;
		if ($rbacreview->isAssigned($ilUser->getId(),2)) {			
			return;
		}
		if ($a_comp == "Services/Init" && $a_part == "init_style") {
			$styleDefinition->setCurrentSkin("seb");
			$styleDefinition->setCurrentStyle("seb");
		}
	}
}
?>
