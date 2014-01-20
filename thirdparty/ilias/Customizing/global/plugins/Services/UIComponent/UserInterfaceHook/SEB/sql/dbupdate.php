<#1>
<?php
//include_once("./Services/JSON/classes/class.ilJsonUtil.php");

$data = array(
	'req_header' => 'HTTP_X_SAFEEXAMBROWSER_REQUESTHASH',
	'seb_key' => 0,
	'url_salt' => 0,
	'role_deny' => 1,
	'browser_access' => 1,
	'role_kiosk' => 1,
	'browser_kiosk' => 1
);

$fields = array(
		'config_json' => array(
			'type' => 'text',
			'length' => '1000',
			'fixed' => false,
			'notnull' => false
		)
);

$ilDB->createTable("ui_uihk_seb_conf", $fields, true, false);
$q = 'INSERT INTO ui_uihk_seb_conf (config_json) VALUES (%s)';
$types = array("text");
$data = array(json_encode($data));
$ilDB->manipulateF($q,$types,$data);
?>
