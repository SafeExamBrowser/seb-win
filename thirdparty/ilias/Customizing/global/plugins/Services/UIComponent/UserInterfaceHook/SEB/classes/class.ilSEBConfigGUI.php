<?php

include_once("./Services/Component/classes/class.ilPluginConfigGUI.php");
include_once("class.ilSEBPlugin.php");
/**
 * Example configuration user interface class
 *
 * @author Stefan Schneider <schneider@hrz.uni-marburg.de>
 * @version $Id$
 *
 */
class ilSEBConfigGUI extends ilPluginConfigGUI {
	/**
	* Handles all commmands, default is "configure"
	*/
	function performCommand($cmd) {

		switch ($cmd)
		{
			case "configure":
			case "save":
				$this->$cmd();
				break;

		}
	}

	/**
	 * Configure screen
	 */
	function configure() {
		global $tpl;
		$form = $this->initConfigurationForm();
		$tpl->setContent($form->getHTML());
	}
	
	//
	// From here on, this is just an example implementation using
	// a standard form (without saving anything)
	//
	
	/**
	 * Init configuration form.
	 *
	 * @return object form object
	 */
	public function initConfigurationForm() {
		global $lng, $ilCtrl, $ilDB, $rbacreview;
		$q = "SELECT * FROM ui_uihk_seb_conf";
		$ret = $ilDB->query($q);
		$rec = $ilDB->fetchAssoc($ret);
		$req_header = ($rec['req_header'] != "") ?  $rec['req_header'] : "";
		$seb_key = ($rec['seb_key'] != "") ? $rec['seb_key'] : "";
		$url_salt = $rec['url_salt'];
		$role_deny = $rec['role_deny'];
		$browser_access = $rec['browser_access'];
		$role_kiosk = $rec['role_kiosk'];
		$browser_kiosk = $rec['browser_kiosk'];
		$roles = array(0=>"none",1=>"all except Admin");
		
		$pl = $this->getPluginObject();
	
		include_once("Services/Form/classes/class.ilPropertyFormGUI.php");
		$form = new ilPropertyFormGUI();
		
		// SEB detection
		$req_header_txt = new ilTextInputGUI($pl->txt("req_header"), "req_header");
		$req_header_txt->setRequired(true);
		$req_header_txt->setSize(50);
		$req_header_txt->setValue($req_header);
		$form->addItem($req_header_txt);
		
		// SEB key
		$seb_key_txt = new ilTextInputGUI($pl->txt("seb_key"), "seb_key");
		$seb_key_txt->setRequired(true);
		$seb_key_txt->setSize(150);
		$seb_key_txt->setValue($seb_key);
		$form->addItem($seb_key_txt);
		
		// key is salted with current url
		$url_salt_cb = new ilCheckboxInputGUI($pl->txt("url_salt"), "url_salt");
		$url_salt_cb->setChecked($url_salt);
		$form->addItem($url_salt_cb);
		
		// global role access deny
		$gr = $rbacreview->getGlobalRoles();
		
		foreach ($gr as $rid) {
			//if ($role_id != 2 && $role_id != 5 && $role_id != 14) {
			if ($rid != 2 && $rid != 14) { // no admin no anomymous
				$roles[$rid] = ilObject::_lookupTitle($rid);
			}
		} 
		
		$role_deny_sel = new ilSelectInputGUI($pl->txt("role_deny"), "role_deny");
		$role_deny_sel->setRequired(false);
		$role_deny_sel->setOptions($roles);
		$role_deny_sel->setValue($role_deny);
		$form->addItem($role_deny_sel);
		
		$browser_access_sel = new ilSelectInputGUI($pl->txt("browser_access"), "browser_access");
		$browser_access_sel->setRequired(false);
		$browser_access_sel->setOptions(array(0=>"none",1=>"seb"));
		$browser_access_sel->setValue($browser_access);
		$form->addItem($browser_access_sel);
		
		$role_kiosk_sel = new ilSelectInputGUI($pl->txt("role_kiosk"), "role_kiosk");
		$role_kiosk_sel->setRequired(false);
		$role_kiosk_sel->setOptions($roles);
		$role_kiosk_sel->setValue($role_kiosk);
		$form->addItem($role_kiosk_sel);
		 
		$browser_kiosk_sel = new ilSelectInputGUI($pl->txt("browser_kiosk"), "browser_kiosk");
		$browser_kiosk_sel->setRequired(false);
		$browser_kiosk_sel->setOptions(array(0=>"all",1=>"seb"));
		$browser_kiosk_sel->setValue($browser_kiosk);
		$form->addItem($browser_kiosk_sel);
		
		$form->addCommandButton("save", $lng->txt("save"));
	                
		$form->setTitle($pl->txt("config"));
		$form->setFormAction($ilCtrl->getFormAction($this));
		
		return $form;
	}
	
	/**
	 * Save form input (currently does not save anything to db)
	 *
	 */
	public function save() {
		global $tpl, $lng, $ilCtrl, $ilDB;
		$pl = $this->getPluginObject();
		$form = $this->initConfigurationForm();
		if ($form->checkInput()) {
			// ToDo validate
			$req_header = ($form->getInput("req_header") != "") ? strtoupper(str_replace("-","_",$form->getInput("req_header"))) : "";
			
			if (!preg_match("/^HTTP\_/", $req_header)) {
				$req_header = "HTTP_".$req_header;				
			}
			
			
								
			$seb_key = ($form->getInput("seb_key") != "") ? $form->getInput("seb_key") : "";
			$url_salt = ($form->getInput("url_salt") != "") ? ((int) $form->getInput("url_salt")) : 0;
			$role_deny = $form->getInput("role_deny");
			$browser_access = $form->getInput("browser_access");
			$role_kiosk = $form->getInput("role_kiosk");
			$browser_kiosk = $form->getInput("browser_kiosk");
			// saving to db						
			$q = "UPDATE ui_uihk_seb_conf SET req_header = %s, seb_key = %s, url_salt = %s, role_deny = %s, browser_access = %s, role_kiosk = %s, browser_kiosk = %s";			
			$types = array("text","text","integer","integer","integer","integer","integer");
			$data = array($req_header,$seb_key,$url_salt,$role_deny,$browser_access,$role_kiosk,$browser_kiosk);
			
			$ret = $ilDB->manipulateF($q,$types,$data);
			if ($ret != 1) {
				ilUtil::sendFailure($lng->txt("save_failure"), true);
			} 	
			else 	{	
				// store apc
				if (ilSEBPlugin::_isAPCInstalled()) {										
					$SEB_CONFIG_CACHE = array(
						"req_header" => $req_header,
						"seb_key" => $seb_key,
						"url_salt" => (int)$url_salt,
						"role_deny" => (int)$role_deny,
						"browser_access" => (int)$browser_access,
						"role_kiosk" => (int)$role_kiosk,
						"browser_kiosk" => (int)$browser_kiosk
					);
					apc_store("SEB_CONFIG_CACHE",$SEB_CONFIG_CACHE);				  
				}
				ilUtil::sendSuccess($lng->txt("save_success"), true);
			}
			
			
			$ilCtrl->redirect($this, "configure");
		}
		else {
			$form->setValuesByPost();
			$tpl->setContent($form->getHtml());
		}
	}
}
?>
