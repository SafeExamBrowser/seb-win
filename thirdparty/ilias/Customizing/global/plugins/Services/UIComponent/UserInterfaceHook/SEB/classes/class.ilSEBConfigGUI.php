<?php

include_once("./Services/Component/classes/class.ilPluginConfigGUI.php");
 
/**
 * Example configuration user interface class
 *
 * @author Alex Killing <alex.killing@gmx.de>
 * @version $Id$
 *
 */
class ilSEBConfigGUI extends ilPluginConfigGUI {
	/**
	* Handles all commmands, default is "configure"
	*/
	function performCommand($cmd)
	{

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
		$seb_key = ($rec['seb_key'] != "") ? base64_decode($rec['seb_key']) : "";
		$url_salt = $rec['url_salt'];
		$pl = $this->getPluginObject();
	
		include_once("Services/Form/classes/class.ilPropertyFormGUI.php");
		$form = new ilPropertyFormGUI();
		$gr = $rbacreview->getGlobalRoles();
		$roles = array();
		foreach ($gr as $role_id) {
			//if ($role_id != 2 && $role_id != 5 && $role_id != 14) {
			if ($role_id != 2) {
				$roles[$role_id] = ilObject::_lookupTitle($role_id);
			}
		} 
		
		$dropbox1 = new ilSelectInputGUI($pl->txt("lock_role"), "lock_role");
		$dropbox1->setRequired(false);
		$dropbox1->setOptions($roles);
		$form->addItem($dropbox1);
		
		$text1 = new ilTextInputGUI($pl->txt("req_header"), "req_header");
		$text1->setRequired(false);
		$text1->setSize(100);
		$text1->setValue($seb_key);
		$form->addItem($text1);
		
		// key is salted with current url
		$cb1 = new ilCheckboxInputGUI($pl->txt("url_salt"), "url_salt");
		$cb1->setChecked($url_salt);
		$form->addItem($cb1);
	
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
			$req = ($form->getInput("req_header") != "") ? base64_encode($form->getInput("req_header")) : "";
			$salt = ($form->getInput("url_salt") != "") ? ((int) $form->getInput("url_salt")) : 0;			
			// saving to db						
			$q = "UPDATE ui_uihk_seb_conf SET seb_key = ".$ilDB->quote($req, "text") . ", url_salt = " . $salt;                   
			$ret = $ilDB->manipulate($q);
			if ($ret != 1) {
				ilUtil::sendFailure($lng->txt("save_failure"), true);
			} 	
			else 	{	
				ilUtil::sendSuccess($lng->txt("save_success"), true);
			}			
			$ilCtrl->redirect($this, "configure");
		}
		else
		{
			$form->setValuesByPost();
			$tpl->setContent($form->getHtml());
		}
	}

}
?>
