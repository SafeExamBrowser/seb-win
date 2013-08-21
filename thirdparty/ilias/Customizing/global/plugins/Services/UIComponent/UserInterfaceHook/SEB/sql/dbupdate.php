<#1>
<?php

$fields = array(
	'req_header' => array(
                'type' => 'text',
                'length' => 50,
                'fixed' => false,
                'notnull' => false
        ),
        'seb_key' => array(
                'type' => 'text',
                'length' => 500,
                'fixed' => false,
                'notnull' => false
        ),
        'url_salt' => array(
                'type' => 'integer',
                'length' => 1,
                'notnull' => false
        ),
        'role_deny' => array(
                'type' => 'integer',
                'length' => 3,
                'notnull' => false
        ),
        'browser_access' => array(
                'type' => 'integer',
                'length' => 1,
                'notnull' => false
        ),
        'role_kiosk' => array(
                'type' => 'integer',
                'length' => 3,
                'notnull' => false
        ),
        'browser_kiosk' => array(
                'type' => 'integer',
                'length' => 1,
                'notnull' => false
        )
);

$ilDB->createTable("ui_uihk_seb_conf", $fields, true, false);

$q = 'INSERT INTO ui_uihk_seb_conf (req_header, seb_key, url_salt,role_deny, browser_access, role_kiosk, browser_kiosk) VALUES (%s,%s,%s,%s,%s,%s,%s)';
$types = array("text", "text", "integer", "integer", "integer","integer","integer");
$data = array("HTTP_X_SAFEEXAMBROWSER_REQUESTHASH","",0,1,1,1,1);

$ilDB->manipulateF($q,$types,$data);
?>
