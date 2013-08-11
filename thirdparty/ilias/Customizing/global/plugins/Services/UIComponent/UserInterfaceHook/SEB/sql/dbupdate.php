<#1>
<?php
$fields = array(
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
        )
);
 
$ilDB->createTable("ui_uihk_seb_conf", $fields);
?>
<#2>
<?php
$q = 'INSERT INTO ui_uihk_seb_conf (seb_key, url_salt) VALUES (%s,%s)';
$types = array("text", "integer");
$data = array("", 0);
$ilDB->manipulateF($q,$types,$data);
?>
