﻿IF NOT EXISTS
(SELECT 1
FROM TS_CONFIG_SETTING_TYPE
WHERE CST_CODE = 'API')
INSERT INTO TS_CONFIG_SETTING_TYPE VALUES('API','API CONFIG');

IF NOT EXISTS
(SELECT 1
FROM TS_CONFIG_SETTING_TYPE
WHERE CST_CODE = 'APP')
INSERT INTO TS_CONFIG_SETTING_TYPE VALUES('APP','APPPLICATION_CONFIG');

IF NOT EXISTS
(SELECT 1
FROM TM_APP_SETTING_CONFIG_VERSION
join TS_CONFIG_SETTING_TYPE on ASV_CST_ID = CST_ID where CST_CODE = 'API')
INSERT INTO TM_APP_SETTING_CONFIG_VERSION
SELECT 1.0,CST_ID FROM TS_CONFIG_SETTING_TYPE WHERE CST_CODE = 'API';

IF NOT EXISTS
(SELECT 1
FROM TM_APP_SETTING_CONFIG_VERSION
join TS_CONFIG_SETTING_TYPE on ASV_CST_ID = CST_ID where CST_CODE = 'APP')
INSERT INTO TM_APP_SETTING_CONFIG_VERSION
SELECT 1.0,CST_ID FROM TS_CONFIG_SETTING_TYPE WHERE CST_CODE = 'APP';


DECLARE @APIID INT;
DECLARE @APPID INT;

SELECT @APIID = ASV_ID FROM TM_APP_SETTING_CONFIG_VERSION
INNER JOIN TS_CONFIG_SETTING_TYPE ON CST_ID = ASV_CST_ID
 WHERE CST_CODE = 'API';

 SELECT @APPID = ASV_ID FROM TM_APP_SETTING_CONFIG_VERSION
INNER JOIN TS_CONFIG_SETTING_TYPE ON CST_ID = ASV_CST_ID
 WHERE CST_CODE = 'APP';


 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_FIREBASE_ENABLED')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_FIREBASE_ENABLED','FALSE','',0);

 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_FIREBASE_APP_NAME')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_FIREBASE_APP_NAME','domain.extension','',1);

 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_PERFORMANCE_ENABLED')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_PERFORMANCE_ENABLED','FALSE','Performance - Switch on [TRUE] or off [FALSE] the Performance',0);

 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_PERFORMANCE_DATABASE')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_PERFORMANCE_DATABASE','defaultConnection','Performance - Choose the Database connection string where to store the records',0);

 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_MEMCACHED_SALSA')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_MEMCACHED_SALSA','xxx','MemCacheD - Salsa code to isolate the cache records form other applications or environments',1);
 
 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_MEMCACHED_MAX_VALIDITY')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_MEMCACHED_MAX_VALIDITY','2592000','MemCacheD - Maximum validity in number of seconds that MemCacheD can handle (30 days = 2592000) ',0);

 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_FIREBASE_ENABLED')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_MEMCACHED_MAX_SIZE','128','MemCacheD - Set the max size in MB before splitting a string record in sub-cache entries',0);

 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_MEMCACHED_ENABLED')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_MEMCACHED_ENABLED','FALSE','MemCacheD - Switch on [TRUE] or off [FALSE] the MemCacheD',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_ENABLED')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_ENABLED','FALSE','EMAIL - MAIL - Switch on [TRUE] or off [FALSE] the Email service',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_MAIL_NOREPLY')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_MAIL_NOREPLY','','EMAIL - MAIL - NoReply email address',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_MAIL_SENDER')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_MAIL_SENDER','','EMAIL - MAIL - Sender email address',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_SMTP_SERVER')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_SMTP_SERVER','','EMAIL - SMTP - Server IP address',1);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_SMTP_PORT')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_SMTP_PORT','','EMAIL - SMTP - Port number',0);

 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_SMTP_AUTHENTICATION')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_SMTP_AUTHENTICATION','FALSE','EMAIL - SMTP - Switch on [TRUE] or off [FALSE] the authentication',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_SMTP_USERNAME')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_SMTP_USERNAME','','EMAIL - SMTP - Set the Username if authentication is required',1);
 
IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_SMTP_PASSWORD')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_SMTP_PASSWORD','','EMAIL - SMTP - Set the Password if authentication is required',1);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_SMTP_SSL')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_SMTP_SSL','FALSE','EMAIL - SMTP - Switch on [TRUE] or off [FALSE] the SSL',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_EMAIL_DATETIME_MASK')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_EMAIL_DATETIME_MASK','dd/MM/yyyy - HH:mm:ss','EMAIL - TEMPLATE - Datetime Mask',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_RECAPTCHA_PRIVATE_KEY')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_RECAPTCHA_PRIVATE_KEY','FALSE','ReCAPTCHA - Private Key ',1);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_RECAPTCHA_URL')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_RECAPTCHA_URL','https://www.google.com/recaptcha/api/siteverify?secret={0}&amp;response={1}','ReCAPTCHA - URL ',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_RECAPTCHA_ENABLED')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_RECAPTCHA_ENABLED','FALSE','ReCAPTCHA - Switch on [TRUE] or off [FALSE] the ReCaptcha',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_AD_DOMAIN')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_AD_DOMAIN','CSOCORK','Active Directory - Domain',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_AD_PATH')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_AD_PATH','','Active Directory - Path ',0);

 IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_AD_USERNAME')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_AD_USERNAME','','Active Directory - Username',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_AD_PASSWORD')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_AD_PASSWORD','','Active Directory - Password',1);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_AD_CUSTOM_PROPERTIES')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_AD_CUSTOM_PROPERTIES','Manager,Division,Title,Department,Directorate,HeadOfDivision,ExternalAccess','Active Directory - Custom Properties (comas separated, case sensitive)',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_MASK_PARAMETERS')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_MASK_PARAMETERS','FileContent,userPrincipal','API - JSONRPC - Mask parameters (comas separated, case insensitive)',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_MAINTENANCE')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_MAINTENANCE','FALSE','API - Maintenance flag [TRUE, FALSE]',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_AUTHENTICATION_TYPE')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_AUTHENTICATION_TYPE','WINDOWS','API - Windows Authentication [ANONYMOUS, WINDOWS, ANY]',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_STATELESS')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_STATELESS','TRUE','API - Stateless [TRUE, FALSE]',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_SUCCESS')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_SUCCESS','success','API - Success response (case sensitive)',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_SESSION_COOKIE')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_SESSION_COOKIE','session','API - Session Cookie (case sensitive)',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_FIREBASE_CREDENTIAL')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_FIREBASE_CREDENTIAL','','Firebase key',1);


IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_AD_BLACKLIST_OUS')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_AD_BLACKLIST_OUS','','List of OU\'s to exclude from AD queries',1);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'API_DATETIME_FORMAT')
INSERT INTO TS_API_SETTING VALUES(@APIID,'API_DATETIME_FORMAT','','List of allowed datetime masks',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'SANITIZER_REMOVE_ALLOWED_TAGS')
INSERT INTO TS_API_SETTING VALUES(@APIID,'SANITIZER_REMOVE_ALLOWED_TAGS','','List of allowed tags to remove',0);


IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'SANITIZER_REMOVE_ALLOWED_ATTRIBUTES')
INSERT INTO TS_API_SETTING VALUES(@APIID,'SANITIZER_REMOVE_ALLOWED_ATTRIBUTES','','List of allowed attributes to remove',0);


IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'SANITIZER_REMOVE_ALLOWED_CSSCLASSESS')
INSERT INTO TS_API_SETTING VALUES(@APIID,'SANITIZER_REMOVE_ALLOWED_CSSCLASSESS','','List of css classes to remove',0);


IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'SANITIZER_REMOVE_ALLOWED_CSSPROPERTIES')
INSERT INTO TS_API_SETTING VALUES(@APIID,'SANITIZER_REMOVE_ALLOWED_CSSPROPERTIES','','List of css properties to remove',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'SANITIZER_REMOVE_ALLOWED_ATRULES')
INSERT INTO TS_API_SETTING VALUES(@APIID,'SANITIZER_REMOVE_ALLOWED_ATRULES','','List of \'at rules\' to remove',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'SANITIZER_REMOVE_ALLOWED_SCHEMES')
INSERT INTO TS_API_SETTING VALUES(@APIID,'SANITIZER_REMOVE_ALLOWED_SCHEMES','','List of schemes to remove',0);

IF NOT EXISTS
(SELECT 1
FROM TS_API_SETTING
where API_KEY = 'SANITIZER_REMOVE_URI_ATTRIBUTES')
INSERT INTO TS_API_SETTING VALUES(@APIID,'SANITIZER_REMOVE_URI_ATTRIBUTES','','List of URI attributes to remove',0);
