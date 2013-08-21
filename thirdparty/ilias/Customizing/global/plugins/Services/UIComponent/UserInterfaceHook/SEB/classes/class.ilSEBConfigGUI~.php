<?php

include_once("./Services/Component/classes/class.ilPluginConfigGUI.php");
 
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
		$lock_role = $rec['lock_role'];
		$role_id = $rec['role_id'];
		$kiosk = $rec['kiosk'];
		
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
		
		// global role binding
		$gr = $rbacreview->getGlobalRoles();
		
		foreach ($gr as $rid) {
			//if ($role_id != 2 && $role_id != 5 && $role_id != 14) {
			if ($rid != 2 && $rid != 14) { // no admin no anomymous
				$roles[$rid] = ilObject::_lookupTitle($rid);
			}
		} 
		
		$roles_sel = new ilSelectInputGUI($pl->txt("roles"), "roles");
		$roles_sel->setRequired(false);
		$roles_sel->setOptions($roles);
		$roles_sel->setValue($role_id);
		$form->addItem($roles_sel);
		
		// role access restriction (user with specified role are allowed to access ILIAS only with a valid SEB request)  
		$lock_role_cb = new ilCheckboxInputGUI($pl->txt("lock_role"), "lock_role");
		$lock_role_cb->setChecked($lock_role);
		$form->addItem($lock_role_cb);
		
		// kiosk (user with specified role are switched to the kiosk view)  
		$kiosk_cb = new ilCheckboxInputGUI($pl->txt("kiosk"), "kiosk");
		$kiosk_cb->setChecked($kiosk);
		$form->addItem($kiosk_cb);
		
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
			$role_id = ($form->getInput("roles") != "") ? $form->getInput("roles") : 0;
			$lock_role = ($form->getInput("lock_role") != "") ? ((int) $form->getInput("lock_role")) : 0;
			$kiosk = ($form->getInput("kiosk") != "") ? ((int) $form->getInput("kiosk")) : 0;
						
			// saving to db						
			$q = "UPDATE ui_uihk_seb_conf SET req_header = %s, seb_key = %s, url_salt = %s, role_id = %s, lock_role = %s, kiosk = %s";			
			$types = array("text","text","integer","integer","integer","integer");
			$data = array($req_header,$seb_key,$url_salt,$role_id,$lock_role,$kiosk);
			
			$ret = $ilDB->manipulateF($q,$types,$data);
			if ($ret != 1) {
				ilUtil::sendFailure($lng->txt("save_failure"), true);
			} 	
			else 	{	
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
