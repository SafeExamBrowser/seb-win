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
        'lock_role' => array(
                'type' => 'integer',
                'length' => 1,
                'notnull' => false
        ),
        'role_id' => array(
                'type' => 'integer',
                'length' => 3,
                'notnull' => false
        ),
        'kiosk' => array(
                'type' => 'integer',
                'length' => 1,
                'notnull' => false
        )
);

$ilDB->createTable("ui_uihk_seb_conf", $fields, true, false);

$q = 'INSERT INTO ui_uihk_seb_conf (req_header, seb_key, url_salt,lock_role,role_id,kiosk) VALUES (%s,%s,%s,%s,%s,%s)';
$types = array("text", "text", "integer", "integer", "integer","integer");
$data = array("X-SafeExamBrowser-RequestHash","",0,1,4,1);

$ilDB->manipulateF($q,$types,$data);
?>
