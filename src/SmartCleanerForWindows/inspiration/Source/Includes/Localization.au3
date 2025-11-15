#include-once

#Region AutoIt3Wrapper Directives Section
;===============================================================================================================
; Tidy Settings
;===============================================================================================================
#AutoIt3Wrapper_Run_Tidy=Y                                         ;~ (Y/N) Run Tidy before compilation. Default=N
#AutoIt3Wrapper_Tidy_Stop_OnError=Y                                 ;~ (Y/N) Continue when only Warnings. Default=Y

#EndRegion AutoIt3Wrapper Directives Section


; #INDEX# =======================================================================================================================
; Title .........: Localization
; AutoIt Version : 3.3.15.0
; Language ......: English
; Description ...: Localization Functions.
; Author(s) .....: Derick Payne (Rizonesoft)
; ===============================================================================================================================

; #CONSTANTS# ===================================================================================================================
Global Const $LNG_COUNTABOUT = 23
Global Const $LNG_COUNTDONATE = 10
Global Const $LNG_COUNTFILE = 25
Global Const $LNG_COUNTLOGGING = 99
Global Const $LNG_COUNTMESSAGES = 50
Global Const $LNG_COUNTUPDATE = 12
Global Const $LNG_COUNTVERSIONING = 4
Global Const $LNG_COUNTCUSTOM = 102
Global Const $LNG_COUNTMENUS = 102
Global Const $LNG_COUNTMESSAGES2 = 202
Global Const $LNG_COUNTPREFERENCES = 102
; ===============================================================================================================================

; #VARIABLES# ===================================================================================================================
If Not IsDeclared("g_sLanguageFile") Then Global $g_sLanguageFile
If Not IsDeclared("g_sCompanyName") Then Global $g_sCompanyName
If Not IsDeclared("g_sProgShortName") Then Global $g_sProgShortName
If Not IsDeclared("g_sProgShortName_X64") Then Global $g_sProgShortName_X64
If Not IsDeclared("g_sProgName") Then Global $g_sProgName
If Not IsDeclared("g_aLangAbout") Then Global $g_aLangAbout[$LNG_COUNTABOUT]
If Not IsDeclared("g_aLangDonate") Then Global $g_aLangDonate[$LNG_COUNTDONATE]
If Not IsDeclared("g_aLangFile") Then Global $g_aLangFile[$LNG_COUNTFILE]
If Not IsDeclared("g_aLangLogging") Then Global $g_aLangLogging[$LNG_COUNTLOGGING]
If Not IsDeclared("g_aLangMessages") Then Global $g_aLangMessages[$LNG_COUNTMESSAGES]
If Not IsDeclared("g_aLangUpdate") Then Global $g_aLangUpdate[$LNG_COUNTUPDATE]
If Not IsDeclared("g_aLangVersioning") Then Global $g_aLangVersioning[$LNG_COUNTVERSIONING]
If Not IsDeclared("g_aLangCustom") Then Global $g_aLangCustom[$LNG_COUNTCUSTOM]
If Not IsDeclared("g_aLangMenus") Then Global $g_aLangMenus[$LNG_COUNTMENUS]
If Not IsDeclared("g_aLangMessages2") Then Global $g_aLangMessages2[$LNG_COUNTMESSAGES2]
If Not IsDeclared("g_aLangPreferences") Then Global $g_aLangPreferences[$LNG_COUNTMESSAGES2]
; ===============================================================================================================================

; Cache for replaced variables
Global $g_aLocalizationCache[1][3]  ; [key][section, original, replaced]
Global $g_iLocalizationCacheSize = 0
Global $g_sLocalizationCacheKey = ""
; #CURRENT# =====================================================================================================================
; ===============================================================================================================================


Func _Localization_About()

;~ Check if the language strings is already loaded. Because we do not want to load the language strings twice.
	If StringLen($g_aLangAbout[0]) > 0 Then
		Return
	EndIf

	$g_aLangAbout[0]  = _Localization_Load("About", "Window_Title", "About %{Program.Name}")
	$g_aLangAbout[1]  = _Localization_Load("About", "Label_Version", "Version")
	$g_aLangAbout[2]  = _Localization_Load("About", "Label_AutoIt", "Build with AutoIt version")
	$g_aLangAbout[3]  = _Localization_Load("About", "Label_Copyright", "Copyright")
	$g_aLangAbout[4]  = _Localization_Load("About", "Tip_Title_Donate", "Donate")
	$g_aLangAbout[5]  = _Localization_Load("About", "Tip_Message_Donate", "Would you consider a small gift of $10 to help us\rnkeep the lights on and make quality free software? Click here to donate.")
	$g_aLangAbout[6]  = _Localization_Load("About", "Label_Home", "Homepage")
	$g_aLangAbout[7]  = _Localization_Load("About", "Label_License", "License")
	$g_aLangAbout[8]  = _Localization_Load("About", "Label_Support", "Get Support")
	$g_aLangAbout[9]  = _Localization_Load("About", "Tip_Title_Country", "Made in South Africa")
	$g_aLangAbout[10] = _Localization_Load("About", "Tip_Message_Country", "%{Program.Name} was born in South Africa.")
	$g_aLangAbout[11] = _Localization_Load("About", "Label_Contributors", "Contributors")
	$g_aLangAbout[12] = _Localization_Load("About", "Tip_Facebook", "Like us on Facebook.")
	$g_aLangAbout[13] = _Localization_Load("About", "Tip_GitHub", "%{Program.Name} on GitHub.")
	$g_aLangAbout[14] = _Localization_Load("About", "Button_Ok", "Ok")
	$g_aLangAbout[15] = _Localization_Load("About", "Label_RAM", "(RAM) %d MB FREE OF %d MB (%d%)")
	$g_aLangAbout[16] = _Localization_Load("About", "Label_HDD", "(%s) %.2f GB FREE OF %.2f GB (%d%)")

EndFunc   ;==>_Localization_About


Func _Localization_Donate()

	If StringLen($g_aLangDonate[0]) > 0 Then
		Return
	EndIf

	$g_aLangDonate[0] = _Localization_Load("Donate", "Label_Heading", "%{Program.Name} has been serving you for over %d hours. Now, how about a small donation?")
	$g_aLangDonate[1] = _Localization_Load("Donate", "Label_Message", "Click on the PayPal button below, choose an amount, and send us the donation. Your donation will be used to improve our software and keep everything free on Rizonesoft. A $20 donation will keep us going for at least a month.")
	$g_aLangDonate[2] = _Localization_Load("Donate", "Label_Donate", "Would you consider a small gift of $10 to help us improve %{Program.Name} and keep the lights on?")

EndFunc   ;==>_Localization_Donate


Func _Localization_File()

	If StringLen($g_aLangFile[0]) > 0 Then
		Return
	EndIf

	$g_aLangFile[0] = _Localization_Load("File", "Backup_Saving", "Saving '%s'.")
	$g_aLangFile[1] = _Localization_Load("File", "Backup_Success", "The directory was successfully saved to '%s'.")
	$g_aLangFile[2] = _Localization_Load("File", "Backup_Continue", "We will now continue with removing it.")
	$g_aLangFile[3] = _Localization_Load("File", "Backup_Error_01", "'%s' could not be saved.")
	$g_aLangFile[4] = _Localization_Load("File", "Backup_Error_02", "To avoid data loss, this directory will not be removed.")
	$g_aLangFile[5] = _Localization_Load("File", "CleanDir_Clearing", "Clearing '%s'.")
	$g_aLangFile[6] = _Localization_Load("File", "CleanDir_Error_01", "Directory Path")
	$g_aLangFile[7] = _Localization_Load("File", "CleanDir_Error_02", "Nothing Here '%s'")
	$g_aLangFile[8] = _Localization_Load("File", "CleanDir_Error_03", "'%s' could not be removed.")
	$g_aLangFile[9] = _Localization_Load("File", "CleanDir_Attributes", "Setting attributes for '%s'.")
	$g_aLangFile[10] = _Localization_Load("File", "CleanDir_Success", "Successfully removed '%s'.")
	$g_aLangFile[11] = _Localization_Load("File", "Delete_Removing", "Removing '%s'.")
	$g_aLangFile[12] = _Localization_Load("File", "Delete_Success", "The file was successfully deleted.")
	$g_aLangFile[13] = _Localization_Load("File", "Delete_Error", "An error occurred whilst deleting the file.")
	$g_aLangFile[14] = _Localization_Load("File", "OpenText_Opening", "Opening '%s'")
	$g_aLangFile[15] = _Localization_Load("File", "OpenText_Success", "Showing '%s' file.")
	$g_aLangFile[16] = _Localization_Load("File", "OpenText_Error", "Could not find the '%s' file.")

EndFunc   ;==>_Localization_File


Func _Localization_Logging()

	If StringLen($g_aLangLogging[0]) > 0 Then
		Return
	EndIf

	$g_aLangLogging[0] = _Localization_Load("Logging", "Finished", "Finished")
	$g_aLangLogging[1] = _Localization_Load("Logging", "success", "success")
	$g_aLangLogging[2] = _Localization_Load("Logging", "Response_Received", "Response Received")
	$g_aLangLogging[3] = _Localization_Load("Logging", "Successfully", "Successfully")
	$g_aLangLogging[4] = _Localization_Load("Logging", "OK", "OK!")
	$g_aLangLogging[5] = _Localization_Load("Logging", "Registration_succeeded", "Registration succeeded")
	$g_aLangLogging[6] = _Localization_Load("Logging", "Initiated", "Initiated")
	$g_aLangLogging[7] = _Localization_Load("Logging", "Error", "error")
	$g_aLangLogging[8] = _Localization_Load("Logging", "Failed", "failed")
	$g_aLangLogging[9] = _Localization_Load("Logging", "Access_Denied", "Access is denied")
	$g_aLangLogging[10] = _Localization_Load("Logging", "No_Operation_Performed", "No operation can be performed")
	$g_aLangLogging[11] = _Localization_Load("Logging", "VERSION", "VERSION")
	$g_aLangLogging[12] = _Localization_Load("Logging", "File_Not_Found", "Could not find the '%s' log File")
	$g_aLangLogging[13] = _Localization_Load("Logging", "Showing_File", "Showing '%s' log File.")
	$g_aLangLogging[14] = _Localization_Load("Logging", "Showing_Error", "Something went wrong: Process ID: %s")
	$g_aLangLogging[15] = _Localization_Load("Logging", "Opening_File", "Opening the log File")
	$g_aLangLogging[16] = _Localization_Load("Logging", "Opening_Directory", "Opening the log Folder...")
	$g_aLangLogging[17] = _Localization_Load("Logging", "Directory_Not_Found", "Could not find the '%s' log Directory")
	$g_aLangLogging[18] = _Localization_Load("Logging", "Showing_Directory", "Showing '%s' log Directory")
	$g_aLangLogging[19] = _Localization_Load("Logging", "Bool_Yes", "Yes")
	$g_aLangLogging[20] = _Localization_Load("Logging", "Bool_No", "No")
	$g_aLangLogging[21] = _Localization_Load("Logging", "Info_Date", "Date:")
	$g_aLangLogging[22] = _Localization_Load("Logging", "Info_Program", "Program:")
	$g_aLangLogging[23] = _Localization_Load("Logging", "Info_Program_Path", "Program Path:")
	$g_aLangLogging[24] = _Localization_Load("Logging", "Info_Compiled", "Compiled:")
	$g_aLangLogging[25] = _Localization_Load("Logging", "Info_AutoIt_Version", "AutoIt Version:")
	$g_aLangLogging[26] = _Localization_Load("Logging", "Info_AutoIt_64Bit", "AutoIt 64-Bit Version:")
	$g_aLangLogging[27] = _Localization_Load("Logging", "Info_Windows_Version", "Windows Version:")
	$g_aLangLogging[28] = _Localization_Load("Logging", "Info_System_Type", "System Type:")
	$g_aLangLogging[29] = _Localization_Load("Logging", "Info_Memory", "Memory (RAM):")
	$g_aLangLogging[30] = _Localization_Load("Logging", "Info_ProgramFiles", "Program Files Directory:")
	$g_aLangLogging[31] = _Localization_Load("Logging", "Info_Windows_Directory", "Windows Directory:")
	$g_aLangLogging[32] = _Localization_Load("Logging", "Level_Error", "Error:")
	$g_aLangLogging[33] = _Localization_Load("Logging", "Level_Warning", "Warning:")
	$g_aLangLogging[34] = _Localization_Load("Logging", "Level_Success", "Success:")
	$g_aLangLogging[35] = _Localization_Load("Logging", "Level_Finished", "Finished:")
	$g_aLangLogging[36] = _Localization_Load("Logging", "Level_Clean", "Clean:")

EndFunc   ;==>_Localization_Logging


Func _Localization_Messages()

	If StringLen($g_aLangMessages[0]) > 0 Then
		Return
	EndIf

	$g_aLangMessages[0] = _Localization_Load("Messages", "Info_Title", "Please Take Note")
	$g_aLangMessages[1] = _Localization_Load("Messages", "Warning_Title", "Warning!")
	$g_aLangMessages[2] = _Localization_Load("Messages", "Error_Title", "Oops! Something went wrong!")
	$g_aLangMessages[3] = _Localization_Load("Messages", "Error_Oops", "Oops!")
	$g_aLangMessages[4] = _Localization_Load("Messages", "Singleton", "Another occurence of %{Program.Name} is already running.")
	$g_aLangMessages[5] = _Localization_Load("Messages", "Compatible", "%{Program.Name} is not compatable with your version of windows. If you believe this to be an error, please feel free to leave a message at '%s' with all the details.")
	$g_aLangMessages[6] = _Localization_Load("Messages", "Compatible_Bit", "Unfortuantely %{Program.Name} 32 Bit is not compatible with your Windows version. Please download %{Program.Name} 64 Bit from '%s'")
	$g_aLangMessages[7] = _Localization_Load("Messages", "Loading_Initializing", "Initializing %{Program.Name}")
	$g_aLangMessages[8] = _Localization_Load("Messages", "Loading_Localizations", "Loading Localizations")
	$g_aLangMessages[9] = _Localization_Load("Messages", "Loading_Resources", "Setting Resources")
	$g_aLangMessages[10] = _Localization_Load("Messages", "Loading_WorkingDirectories", "Setting Working Directories")
	$g_aLangMessages[11] = _Localization_Load("Messages", "Loading_Configuration", "Loading Configuration")
	$g_aLangMessages[12] = _Localization_Load("Messages", "Loading_Logging", "Initializing Logging")
	$g_aLangMessages[13] = _Localization_Load("Messages", "Loading_Shortcuts", "Setting Keyboard Shortcuts")
	$g_aLangMessages[14] = _Localization_Load("Messages", "Loading_Interface", "Starting Core Interface")
	$g_aLangMessages[15] = _Localization_Load("Messages", "Ping_Check_Connection", "Checking Internet Connection.")
	$g_aLangMessages[16] = _Localization_Load("Messages", "Ping_Response", "Response Received in %s milliseconds.")
	$g_aLangMessages[17] = _Localization_Load("Messages", "Ping_Host_Offline", "Host is Offline.")
	$g_aLangMessages[18] = _Localization_Load("Messages", "Ping_Host_Unreachable", "Host is Unreachable.")
	$g_aLangMessages[19] = _Localization_Load("Messages", "Ping_Host_Bad", "Bad Destination.")
	$g_aLangMessages[20] = _Localization_Load("Messages", "Ping_Host_No_Internet", "No Internet Connection.")
	$g_aLangMessages[21] = _Localization_Load("Messages", "Global_Errors", "Finished with (%02i) %s!")
	$g_aLangMessages[22] = _Localization_Load("Messages", "Global_Errors_Error", "error")
	$g_aLangMessages[23] = _Localization_Load("Messages", "Global_Errors_Errors", "errors")
	$g_aLangMessages[24] = _Localization_Load("Messages", "Global_Finished", "Completed selected task.")
	$g_aLangMessages[25] = _Localization_Load("Messages", "Global_File", "file")
	$g_aLangMessages[26] = _Localization_Load("Messages", "Global_Files", "files")
	$g_aLangMessages[27] = _Localization_Load("Messages", "RegisterDll_Success", "RegSvr32.exe > '%s' registration succeeded.")
	$g_aLangMessages[28] = _Localization_Load("Messages", "RegisterDll_Error_01", "RegSvr32.exe > '%s' To register a module, you must provide a binary name.")
	$g_aLangMessages[29] = _Localization_Load("Messages", "RegisterDll_Error_02", "RegSvr32.exe > '%s' Specified module not found.")
	$g_aLangMessages[30] = _Localization_Load("Messages", "RegisterDll_Error_03", "RegSvr32.exe > '%s' Module loaded but entry-point DllRegisterServer was not found.")
	$g_aLangMessages[31] = _Localization_Load("Messages", "RegisterDll_Error_04", "RegSvr32.exe > '%s' Error number: 0x80070005")
	$g_aLangMessages[32] = _Localization_Load("Messages", "Registry_Write_Error", "Could not write to '%s'")
	$g_aLangMessages[33] = _Localization_Load("Messages", "Registry_Error_01", "Unable to open requested key.")
	$g_aLangMessages[34] = _Localization_Load("Messages", "Registry_Error_02", "Unable to open requested main key.")
	$g_aLangMessages[35] = _Localization_Load("Messages", "Registry_Error_03", "Unable to open requested value.")
	$g_aLangMessages[36] = _Localization_Load("Messages", "Registry_Error_04", "Value type not supported.")
	$g_aLangMessages[37] = _Localization_Load("Messages", "Registry_Error_05", "Unable to delete requested value.")
	$g_aLangMessages[38] = _Localization_Load("Messages", "Registry_Error_06", "Unable to delete requested key/value.")

EndFunc   ;==>_Localization_Messages


Func _Localization_Update()

	If StringLen($g_aLangUpdate[0]) > 0 Then
		Return
	EndIf

	$g_aLangUpdate[0] = _Localization_Load("Update", "Window_Title_Available", "Update available")
	$g_aLangUpdate[1] = _Localization_Load("Update", "Window_Title_Congratulations", "Congratulations")
	$g_aLangUpdate[2] = _Localization_Load("Update", "Window_Title_Error", "Something went wrong!")
	$g_aLangUpdate[3] = _Localization_Load("Update", "Label_Message_Update", "There is a new version of '%{Program.Name}' available.")
	$g_aLangUpdate[4] = _Localization_Load("Update", "Label_Message_Latest", "You are using the latest version of '%{Program.Name}'.")
	$g_aLangUpdate[5] = _Localization_Load("Update", "Label_Message_Error", "Information about the latest version of '%{Program.Name}' could not be retrieved.")
	$g_aLangUpdate[6] = _Localization_Load("Update", "Label_Message_Internet", "Please check your Internet Connection and try again.")
	$g_aLangUpdate[7] = _Localization_Load("Update", "Label_Build_Current", "Current Build:")
	$g_aLangUpdate[8] = _Localization_Load("Update", "Label_Build_Update", "Update Build:")
	$g_aLangUpdate[9] = _Localization_Load("Update", "CheckBox_NoUpdate", "Do not check for updates at startup.")
	$g_aLangUpdate[10] = _Localization_Load("Update", "Button_Update", "Read more")
	$g_aLangUpdate[11] = _Localization_Load("Update", "Button_Close", "Close")

EndFunc   ;==>_Localization_Update


Func _Localization_Versioning()

	If StringLen($g_aLangVersioning[0]) > 0 Then
		Return
	EndIf

	$g_aLangVersioning[0] = _Localization_Load("Versioning", "Administrator", "Administrator")
	$g_aLangVersioning[1] = _Localization_Load("Versioning", "Build", "Build")
	$g_aLangVersioning[2] = _Localization_Load("Versioning", "AutoIt_Version", "using AutoIt version %{AutoIt.Version}")
	$g_aLangVersioning[3] = _Localization_Load("Versioning", "Bit", "Bit")

EndFunc   ;==>_Localization_Versioning


Func _Localization_Custom()

;~ Check if the language strings is already loaded. Because we do not want to load the language strings twice.
	If StringLen($g_aLangCustom[0]) > 0 Then
		Return
	EndIf

	$g_aLangCustom[0] = _Localization_Load("Custom", "Label_Status_Welcome", "Remember! Never fix something that is not broken, you could break stuff.")
	$g_aLangCustom[1] = _Localization_Load("Custom", "Label_Status_Updates", "Checking for Updates")
	$g_aLangCustom[2] = _Localization_Load("Custom", "Checkbox_Repair_01", "Reset Internet Protocols (TCP/IP)")
	$g_aLangCustom[3] = _Localization_Load("Custom", "Checkbox_Repair_02", "Repair Winsock (Reset Catalog)")
	$g_aLangCustom[4] = _Localization_Load("Custom", "Checkbox_Repair_03", "Renew Internet Connections")
	$g_aLangCustom[5] = _Localization_Load("Custom", "Checkbox_Repair_04", "Flush DNS Resolver Cache (Domain Name System)")
	$g_aLangCustom[6] = _Localization_Load("Custom", "Checkbox_Repair_05", "Flush ARP Cache (Address Resolution Protocol)")
	$g_aLangCustom[7] = _Localization_Load("Custom", "Checkbox_Repair_06", "Repair Internet Explorer %s")
	$g_aLangCustom[8] = _Localization_Load("Custom", "Checkbox_Repair_07", "Clear Windows Update History")
	$g_aLangCustom[9] = _Localization_Load("Custom", "Checkbox_Repair_08", "Repair Windows / Automatic Updates")
	$g_aLangCustom[10] = _Localization_Load("Custom", "Checkbox_Repair_09", "Repair SSL / HTTPS / Cryptography")
	$g_aLangCustom[11] = _Localization_Load("Custom", "Checkbox_Repair_10", "Reset Proxy Server Configuration")
	$g_aLangCustom[12] = _Localization_Load("Custom", "Checkbox_Repair_11", "Reset Windows Firewall Configuration")
	$g_aLangCustom[13] = _Localization_Load("Custom", "Checkbox_Repair_12", "Restore the default hosts file")
	$g_aLangCustom[14] = _Localization_Load("Custom", "Checkbox_Repair_13", "Renew Wins Client Registrations")
	$g_aLangCustom[15] = _Localization_Load("Custom", "Checkbox_Repair_14", "Make Network Computers Visible in File Explorer")
	$g_aLangCustom[16] = _Localization_Load("Custom", "Tip_Status_Show", "Show Status")
	$g_aLangCustom[17] = _Localization_Load("Custom", "Tip_Status_Hide", "Hide Status")
	$g_aLangCustom[18] = _Localization_Load("Custom", "Button_Go", "Go!")
	$g_aLangCustom[19] = _Localization_Load("Custom", "Button_Go_Stop", "Stop")
	$g_aLangCustom[20] = _Localization_Load("Custom", "Info_01", "Before you start; it is recommended that you create a System Restore Point to roll back any changes made by Complete Internet Repair. Furthermore, run the build-in Windows Internet Troubleshooters before any repair options.\rn\rnSelect your repair options and press '%s' to start. Do not select something unless your computer has the described problem. Skip any option you do not understand.")
	$g_aLangCustom[21] = _Localization_Load("Custom", "Info_02", "This option rewrites important registry keys that are used by the Internet Protocol (TCP/IP) stack. This has the same result as removing and reinstalling the protocol.")
	$g_aLangCustom[22] = _Localization_Load("Custom", "Info_03", "This can be used to recover from Winsock corruption result in lost of network connectivity. This option should be used with care becuase any pre-installed LSP's will need to be reinstalled.")
	$g_aLangCustom[23] = _Localization_Load("Custom", "Info_04", "Release and renew all Internet (TCP/IP) connections.")
	$g_aLangCustom[24] = _Localization_Load("Custom", "Info_05", "Flush DNS Resolver Cache, refresh all DHCP leases and re-register DNS names.")
	$g_aLangCustom[25] = _Localization_Load("Custom", "Info_06", "ARP (Address Resolution Protocol) Cache is a technique used to store 'mappings' of OSI Model Network Layer addresses (IP addresses) to corresponding OSI Model Data Link addresses (MAC addresses). Due to a variety of possible circumstances, ARP cache can become damaged.\rn\rnSymptoms include: numerous websites fail to load, and interruptions in network or internet connectivity. The Ping command will also fail to work for communicating with two or more remote hosts.")
	$g_aLangCustom[26] = _Localization_Load("Custom", "Info_07", "Re-registers all the concerned dll and ocx files required for a smooth operation of Microsoft Internet Explorer %s.")
	$g_aLangCustom[27] = _Localization_Load("Custom", "Info_08", "This option will clear the Windows Update History. It will do this by emptying the [%s] and [%s] directories.")
	$g_aLangCustom[28] = _Localization_Load("Custom", "Info_09", "This option will try and fix Windows Update / Automatic Updates. Try this when you are unable to download or install updates.")
	$g_aLangCustom[29] = _Localization_Load("Custom", "Info_10", "If you are having trouble connecting to SSL / Secured websites (Ex. Banking) then this option could help.")
	$g_aLangCustom[30] = _Localization_Load("Custom", "Info_11", "Many malware infections create proxy servers and then set Windows to route all web traffic through the virus proxy. For example, an attempt to access Rizonesoft.com, will redirect to a malware site.\rn\rnThis option will attempt to reset all proxy configurations, including persistent WinHTTP proxy configuration.")
	$g_aLangCustom[31] = _Localization_Load("Custom", "Info_12", "Reset the Windows Firewall configuration to its default state.")
	$g_aLangCustom[32] = _Localization_Load("Custom", "Info_13", "Reset the Windows hosts file to its default state.")
	$g_aLangCustom[33] = _Localization_Load("Custom", "Info_14", "It is sometimes necessary that clients or servers on your network need to reregister their NetBIOS names with a Windows Internet Name Service (WINS) server.\rn\rnHere are some situations where renewing WINS client registrations would be necessary: The registration has been lost or deleted in WINS and needs to be refreshed by the client. The registration exists in some WINS servers but not in others. A reregistration is useful here to increment the WINS version Ids, which will help in causing a WINS server replication to occur.")
	$g_aLangCustom[34] = _Localization_Load("Custom", "Info_15", "By default, computers in your local network should be visible when browsing for a network device with File Explorer. With Windows 10 build 1803 some computers are only accessible via their names or IP addresses.")
	$g_aLangCustom[35] = _Localization_Load("Custom", "Label_Sub_Heading_Notice", "Complete Internet Repair functionality now bundled with Complete Windows Repair. Click here to download it now and evolve!!")
	$g_aLangCustom[36] = _Localization_Load("Custom", "Label_All_Complete", "All selected tasks Complete.")
	$g_aLangCustom[37] = _Localization_Load("Custom", "Checkbox_Select_All", "Select All")
	$g_aLangCustom[38] = _Localization_Load("Custom", "Checkbox_Select_None", "Select None")

EndFunc   ;==>_Localization_Custom


Func _Localization_Menus()

	If StringLen($g_aLangMenus[0]) > 0 Then
		Return
	EndIf

	$g_aLangMenus[0] = _Localization_Load("Menus", "File", "&File")
	$g_aLangMenus[1] = _Localization_Load("Menus", "File_Event_Log", "Windows Event &Viewer")
	$g_aLangMenus[2] = _Localization_Load("Menus", "File_Preferences", "&Preferences")
	$g_aLangMenus[3] = _Localization_Load("Menus", "File_Logging", "&Logging")
	$g_aLangMenus[4] = _Localization_Load("Menus", "File_Logging_Open_File", "Open &log File")
	$g_aLangMenus[5] = _Localization_Load("Menus", "File_Logging_Open_Directory", "Open log &Directory")
	$g_aLangMenus[6] = _Localization_Load("Menus", "File_Logging_IP_Reset", "Open &IP reset log")
	$g_aLangMenus[7] = _Localization_Load("Menus", "File_Export", "&Export (information)")
	$g_aLangMenus[8] = _Localization_Load("Menus", "File_Export_IP", "&IP Configuration (all)")
	$g_aLangMenus[9] = _Localization_Load("Menus", "File_Export_LSP", "Winsock &LSPs")
	$g_aLangMenus[10] = _Localization_Load("Menus", "File_Export_ARP", "&ARP Entries (all)")
	$g_aLangMenus[11] = _Localization_Load("Menus", "File_Export_NBIOS", "Net&BIOS Statistics")
	$g_aLangMenus[12] = _Localization_Load("Menus", "File_Reboot", "&Reboot Windows")
	$g_aLangMenus[13] = _Localization_Load("Menus", "File_Close", "&Close\tAlt+F4")
	$g_aLangMenus[14] = _Localization_Load("Menus", "Maintenance", "&Maintenance")
	$g_aLangMenus[15] = _Localization_Load("Menus", "Maintenance_Restore", "&Create a Windows Restore Point")
	$g_aLangMenus[16] = _Localization_Load("Menus", "Troubleshoot", "&Troubleshoot")
	$g_aLangMenus[17] = _Localization_Load("Menus", "Troubleshoot_01", "Network Diagnostics &Web (Internet)")
	$g_aLangMenus[18] = _Localization_Load("Menus", "Troubleshoot_02", "Network Diagnostics Network &Adapter")
	$g_aLangMenus[19] = _Localization_Load("Menus", "Troubleshoot_03", "I&nternet Diagnostic")
	$g_aLangMenus[20] = _Localization_Load("Menus", "Troubleshoot_04", "Network Diagnostics &Inbound")
	$g_aLangMenus[21] = _Localization_Load("Menus", "Troubleshoot_05", "&Home Group Diagnostic")
	$g_aLangMenus[22] = _Localization_Load("Menus", "Troubleshoot_06", "Network Diagnostics &File Share")
	$g_aLangMenus[23] = _Localization_Load("Menus", "Troubleshoot_07", "&BITS Diagnostic")
	$g_aLangMenus[24] = _Localization_Load("Menus", "Troubleshoot_08", "Windows &Update Diagnostic")
	$g_aLangMenus[25] = _Localization_Load("Menus", "Troubleshoot_09", "Internet E&xplorer Diagnostic")
	$g_aLangMenus[26] = _Localization_Load("Menus", "Troubleshoot_10", "Internet Explorer &Security Diagnostic")
	$g_aLangMenus[27] = _Localization_Load("Menus", "Troubleshoot_11", "Internet Sp&eed Test")
	$g_aLangMenus[28] = _Localization_Load("Menus", "Troubleshoot_12", "Get Router &Passwords")
	$g_aLangMenus[29] = _Localization_Load("Menus", "Tools", "T&ools")
	$g_aLangMenus[30] = _Localization_Load("Menus", "Tools_RDP", "&Remote Desktop Connection")
	$g_aLangMenus[31] = _Localization_Load("Menus", "Tools_IE_Properties", "Internet &Explorer properties")
	$g_aLangMenus[32] = _Localization_Load("Menus", "Tools_Install_IP6", "&Install IP6 protocol")
	$g_aLangMenus[33] = _Localization_Load("Menus", "Tools_Uninstall_IP6", "&Uninstall IP6 protocol")
	$g_aLangMenus[34] = _Localization_Load("Menus", "Tools_Repair_WorkView", "Repair &Workgroup Computers view")
	$g_aLangMenus[35] = _Localization_Load("Menus", "Help", "&Help")
	$g_aLangMenus[36] = _Localization_Load("Menus", "Help_Update", "Check for &updates")
	$g_aLangMenus[37] = _Localization_Load("Menus", "Help_Home", "%{Company.Name} &Home")
	$g_aLangMenus[38] = _Localization_Load("Menus", "Help_Downloads", "More &Downloads")
	$g_aLangMenus[39] = _Localization_Load("Menus", "Help_Support", "&Get Support")
	$g_aLangMenus[40] = _Localization_Load("Menus", "Help_Issue", "Create an &issue")
	$g_aLangMenus[41] = _Localization_Load("Menus", "Help_Donate", "Donate to &our Cause")
	$g_aLangMenus[42] = _Localization_Load("Menus", "Help_About", "About %{Program.Name}")

EndFunc   ;==>_Localization_Menus


Func _Localization_Messages2()

	If StringLen($g_aLangMessages2[0]) > 0 Then
		Return
	EndIf

	$g_aLangMessages2[0] = _Localization_Load("Messages2", "Select_Something_Title", "Select Something")
	$g_aLangMessages2[1] = _Localization_Load("Messages2", "Select_Something_Message", "How about selecting something first?")
	$g_aLangMessages2[2] = _Localization_Load("Messages2", "Stopping", "Stopping, Please wait!")
	$g_aLangMessages2[3] = _Localization_Load("Messages2", "Boot_01", "A system reboot may be required before the settings will take effect.")
	$g_aLangMessages2[4] = _Localization_Load("Messages2", "Boot_02", "To Reboot or not to Reboot?")
	$g_aLangMessages2[5] = _Localization_Load("Messages2", "Boot_03", "You might need to reboot your computer before the settings will take effect. Would you like to reboot your computer now? Save your stuff, hold your breath and press Yes to reboot your computer. Or press No to ignore me.")
	$g_aLangMessages2[6] = _Localization_Load("Messages2", "Boot_04", "^ You do not want to reboot your computer?")
	$g_aLangMessages2[7] = _Localization_Load("Messages2", "Boot_05", "^ Can't argue with that. It is your computer after all.")
	$g_aLangMessages2[8] = _Localization_Load("Messages2", "Boot_06", "Click on '%s' and then '%s' to Reboot your computer later.")
	$g_aLangMessages2[9] = _Localization_Load("Messages2", "Boot_07", "Rebooting Windows...")
	$g_aLangMessages2[10] = _Localization_Load("Messages2", "MsgBox_Boot_Title", "Reboot!")
	$g_aLangMessages2[11] = _Localization_Load("Messages2", "MsgBox_boot_Message", "Your computer will reboot in %s seconds. This should give you enough time to save your stuff. Press Ok to reboot your computer now.")
	$g_aLangMessages2[12] = _Localization_Load("Messages2", "Boot_Restarting", "Your computer is restarting.")
	$g_aLangMessages2[13] = _Localization_Load("Messages2", "Boot_Canceled", "Reboot Canceled.")
	$g_aLangMessages2[14] = _Localization_Load("Messages2", "Reset_IP_01", "Resetting all IP configurations.")
	$g_aLangMessages2[15] = _Localization_Load("Messages2", "Reset_IP_02", "TCP/IP Reset log located @ [%s]")
	$g_aLangMessages2[16] = _Localization_Load("Messages2", "Reset_IP_03", "Resetting IP version 4 configurations.")
	$g_aLangMessages2[17] = _Localization_Load("Messages2", "Reset_IP_04", "Resetting IP version 6 configurations.")
	$g_aLangMessages2[18] = _Localization_Load("Messages2", "Winsock_01", "Attempting to reset Winsock catalog.")
	$g_aLangMessages2[19] = _Localization_Load("Messages2", "Winsock_02", "It is recommended that you install Windows XP Service Pack 2 or later.")
	$g_aLangMessages2[20] = _Localization_Load("Messages2", "Winsock_03", "Finished resetting Winsock catalog.")
	$g_aLangMessages2[21] = _Localization_Load("Messages2", "Winsock_04", "Resetting Winsock using Method 1.")
	$g_aLangMessages2[22] = _Localization_Load("Messages2", "Winsock_05", "Resetting Winsock using Method 2.")
	$g_aLangMessages2[23] = _Localization_Load("Messages2", "Renew_IP_01", "Releasing and Renewing TCP/IP connections.")
	$g_aLangMessages2[24] = _Localization_Load("Messages2", "Renew_IP_02", "Releasing TCP/IP connections.")
	$g_aLangMessages2[25] = _Localization_Load("Messages2", "Renew_IP_03", "Renewing TCP/IP connections.")
	$g_aLangMessages2[26] = _Localization_Load("Messages2", "Renew_IP_04", "Resetting Winsock.")
	$g_aLangMessages2[27] = _Localization_Load("Messages2", "Renew_IP_05", "TCP/IP renewed")
	$g_aLangMessages2[28] = _Localization_Load("Messages2", "DNS_01", "Refreshing DNS Resolver Cache.")
	$g_aLangMessages2[29] = _Localization_Load("Messages2", "DNS_02", "Flushing DNS")
	$g_aLangMessages2[30] = _Localization_Load("Messages2", "DNS_03", "Registering DNS")
	$g_aLangMessages2[31] = _Localization_Load("Messages2", "DNS_04", "DNS Refreshed")
	$g_aLangMessages2[32] = _Localization_Load("Messages2", "ARP_01", "Flushing ARP (Address Resolution Protocol) Cache.")
	$g_aLangMessages2[33] = _Localization_Load("Messages2", "ARP_02", "ARP Cache Refreshed")
	$g_aLangMessages2[34] = _Localization_Load("Messages2", "Repair_IE_01", "Repairing Internet Explorer version %s.")
	$g_aLangMessages2[35] = _Localization_Load("Messages2", "Repair_IE_02", "Closing Internet Explorer. Save your work before you press OK")
	$g_aLangMessages2[36] = _Localization_Load("Messages2", "Repair_IE_03", "Repairing 'Open in new tab/window not working'.")
	$g_aLangMessages2[37] = _Localization_Load("Messages2", "Repair_IE_04", "Repairing 'Add-Ons-Manager menu entry is present but nothing happens'.")
	$g_aLangMessages2[38] = _Localization_Load("Messages2", "Repair_IE_05", "Repairing Simple HTML Mail API.")
	$g_aLangMessages2[39] = _Localization_Load("Messages2", "Repair_IE_06", "Repairing Group policy snap-in.")
	$g_aLangMessages2[40] = _Localization_Load("Messages2", "Repair_IE_07", "Repairing Smart Screen.")
	$g_aLangMessages2[41] = _Localization_Load("Messages2", "Repair_IE_08", "Repairing IEAK Branding.")
	$g_aLangMessages2[42] = _Localization_Load("Messages2", "Repair_IE_09", "Repairing Development Tools.")
	$g_aLangMessages2[43] = _Localization_Load("Messages2", "Repair_IE_10", "Repairing 'IE8 closes immediately on launch'.")
	$g_aLangMessages2[44] = _Localization_Load("Messages2", "Repair_IE_11", "Repairing License Manager.")
	$g_aLangMessages2[45] = _Localization_Load("Messages2", "Repair_IE_12", "Repairing Javascript links don't work (Robin Walker) .NET hub file.")
	$g_aLangMessages2[46] = _Localization_Load("Messages2", "Repair_IE_13", "Repairing VS Debugger.")
	$g_aLangMessages2[47] = _Localization_Load("Messages2", "Repair_IE_14", "Repairing Printing problems, open in new window.")
	$g_aLangMessages2[48] = _Localization_Load("Messages2", "Repair_IE_15", "Repairing 'Find on this page is blank'.")
	$g_aLangMessages2[49] = _Localization_Load("Messages2", "Repair_IE_16", "Repairing Process debug manager.")
	$g_aLangMessages2[50] = _Localization_Load("Messages2", "Repair_IE_17", "Repairing VML Renderer.")
	$g_aLangMessages2[51] = _Localization_Load("Messages2", "Repair_IE_18", "Fixing 'New tabs page cannot display content because it cannot access the controls'.")
	$g_aLangMessages2[52] = _Localization_Load("Messages2", "Repair_IE_19", "This is a result of a bug in shdocvw.dll.")
	$g_aLangMessages2[53] = _Localization_Load("Messages2", "Repair_IE_20", "Repairing Outlook Express.")
	$g_aLangMessages2[54] = _Localization_Load("Messages2", "Repair_IE_21", "Internet Explorer should function correctly now.")
	$g_aLangMessages2[55] = _Localization_Load("Messages2", "Clear_Update_01", "Clearing File Stores (Update History).")
	$g_aLangMessages2[56] = _Localization_Load("Messages2", "Clear_Update_02", "Update History Cleared.")
	$g_aLangMessages2[57] = _Localization_Load("Messages2", "Repair_Update_01", "Repairing Windows Update / Automatic Updates.")
	$g_aLangMessages2[58] = _Localization_Load("Messages2", "Repair_Update_02", "Stopping the Nero Update Service.")
	$g_aLangMessages2[59] = _Localization_Load("Messages2", "Repair_Update_03", "Stopping the BITS Service.")
	$g_aLangMessages2[60] = _Localization_Load("Messages2", "Repair_Update_04", "Stopping the Automatic Updates Service.")
	$g_aLangMessages2[61] = _Localization_Load("Messages2", "Repair_Update_05", "Setting BITS Security Descriptor.")
	$g_aLangMessages2[62] = _Localization_Load("Messages2", "Repair_Update_06", "Setting Automatic Updates Service Security Descriptor.")
	$g_aLangMessages2[63] = _Localization_Load("Messages2", "Repair_Update_07", "Configuring the Automatic Updates Service.")
	$g_aLangMessages2[64] = _Localization_Load("Messages2", "Repair_Update_08", "Configuring BITS.")
	$g_aLangMessages2[65] = _Localization_Load("Messages2", "Repair_Update_09", "Registering Windows Updates Dlls.")
	$g_aLangMessages2[66] = _Localization_Load("Messages2", "Repair_Update_10", "Restarting the Automatic Updates Service.")
	$g_aLangMessages2[67] = _Localization_Load("Messages2", "Repair_Update_11", "Restarting the BITS Service.")
	$g_aLangMessages2[68] = _Localization_Load("Messages2", "Repair_Update_12", "Restarting the Nero Update Service.")
	$g_aLangMessages2[69] = _Localization_Load("Messages2", "Repair_Update_13", "Clean transactional metadata on next Transactional Resource Manager mount.")
	$g_aLangMessages2[70] = _Localization_Load("Messages2", "Repair_Update_14", "Clearing the BITS queue.")
	$g_aLangMessages2[71] = _Localization_Load("Messages2", "Repair_Update_15", "Initiating Windows Updates detection right away.")
	$g_aLangMessages2[72] = _Localization_Load("Messages2", "Repair_Update_16", "Windows Update repaired.")
	$g_aLangMessages2[73] = _Localization_Load("Messages2", "SSL_Crypt_01", "Repairing SSL / HTTPS / Cryptography service.")
	$g_aLangMessages2[74] = _Localization_Load("Messages2", "SSL_Crypt_02", "Stopping the Cryptographic Service.")
	$g_aLangMessages2[75] = _Localization_Load("Messages2", "SSL_Crypt_03", "Configuring the cryptographic service.")
	$g_aLangMessages2[76] = _Localization_Load("Messages2", "SSL_Crypt_04", "Clearing '%s'.")
	$g_aLangMessages2[77] = _Localization_Load("Messages2", "SSL_Crypt_05", "'%s' Cleared.")
	$g_aLangMessages2[78] = _Localization_Load("Messages2", "SSL_Crypt_06", "Registering SSL / HTTPS / Cryptography DLLs.")
	$g_aLangMessages2[79] = _Localization_Load("Messages2", "SSL_Crypt_07", "SSL / HTTPS / Cryptography DLLs Registered.")
	$g_aLangMessages2[80] = _Localization_Load("Messages2", "SSL_Crypt_08", "Restarting the cryptographic service.")
	$g_aLangMessages2[81] = _Localization_Load("Messages2", "SSL_Crypt_09", "Online banking should work now.")
	$g_aLangMessages2[82] = _Localization_Load("Messages2", "Reset_Proxy_01", "Resetting proxy settings.")
	$g_aLangMessages2[83] = _Localization_Load("Messages2", "Reset_Proxy_02", "Setting proxy to direct access.")
	$g_aLangMessages2[84] = _Localization_Load("Messages2", "Reset_Proxy_03", "Proxy settings reset to defaults.")
	$g_aLangMessages2[85] = _Localization_Load("Messages2", "Reset_Firewall_01", "Resetting Windows Firewall configuraton.")
	$g_aLangMessages2[86] = _Localization_Load("Messages2", "Reset_Firewall_02", "Firewall settings reset to defaults.")
	$g_aLangMessages2[87] = _Localization_Load("Messages2", "Restore_HOSTS_01", "Restoring the default Windows HOSTS file.")
	$g_aLangMessages2[88] = _Localization_Load("Messages2", "Restore_HOSTS_02", "An error occurred whilst writing the hosts file.")
	$g_aLangMessages2[89] = _Localization_Load("Messages2", "Restore_HOSTS_03", "Writing data to the HOSTS file.")
	$g_aLangMessages2[90] = _Localization_Load("Messages2", "Restore_HOSTS_04", "HOSTS file created successfully.")
	$g_aLangMessages2[91] = _Localization_Load("Messages2", "Restore_HOSTS_05", "HOSTS file restored.")
	$g_aLangMessages2[92] = _Localization_Load("Messages2", "Renew_Wins_01", "Renewing Wins Client Registrations.")
	$g_aLangMessages2[93] = _Localization_Load("Messages2", "Renew_Wins_02", "Wins Client Registrations Renewed.")
	$g_aLangMessages2[94] = _Localization_Load("Messages2", "Make_Computers_Visible_01", "Making Network Computers Visible.")
	$g_aLangMessages2[95] = _Localization_Load("Messages2", "Make_Computers_Visible_02", "Configuring the Function Discovery Resource Publication Service.")
	$g_aLangMessages2[96] = _Localization_Load("Messages2", "Make_Computers_Visible_03", "Network Computers should now be visible in File Explorer.")
	$g_aLangMessages2[97] = _Localization_Load("Messages2", "Open_Events_01", "Opening Windows Event Viewer.")
	$g_aLangMessages2[98] = _Localization_Load("Messages2", "Open_Events_02", "Event Viewer should be open now.")
	$g_aLangMessages2[99] = _Localization_Load("Messages2", "Export_IP_01", "Exporting Windows IP Configuration.")
	$g_aLangMessages2[100] = _Localization_Load("Messages2", "Export_IP_02", "Windows IP Configuration Saved to '%s'")
	$g_aLangMessages2[101] = _Localization_Load("Messages2", "Export_IP_03", "Could not save IP Configuration.")
	$g_aLangMessages2[102] = _Localization_Load("Messages2", "Export_LSPs_01", "Exporting Installed Winsock LSPs.")
	$g_aLangMessages2[103] = _Localization_Load("Messages2", "Export_LSPs_02", "Winsock LSPs list Saved to '%s'")
	$g_aLangMessages2[104] = _Localization_Load("Messages2", "Export_LSPs_03", "Could not save LSP list.")
	$g_aLangMessages2[105] = _Localization_Load("Messages2", "Export_ARP_01", "Exporting ARP entries.")
	$g_aLangMessages2[106] = _Localization_Load("Messages2", "Export_ARP_02", "ARP entries Saved to '%s'")
	$g_aLangMessages2[107] = _Localization_Load("Messages2", "Export_ARP_03", "Could not save ARP entries.")
	$g_aLangMessages2[108] = _Localization_Load("Messages2", "Export_NBIOS_01", "Exporting NetBIOS Statistics.")
	$g_aLangMessages2[109] = _Localization_Load("Messages2", "Export_NBIOS_02", "NetBIOS Statistics Saved to '%s'")
	$g_aLangMessages2[110] = _Localization_Load("Messages2", "Export_NBIOS_03", "Could not save NetBIOS Statistics.")
	$g_aLangMessages2[111] = _Localization_Load("Messages2", "Tools_Open_RDP_01", "Opening Remote Desktop Connection.")
	$g_aLangMessages2[112] = _Localization_Load("Messages2", "Tools_Open_RDP_02", "Could not open Remote Desktop Connection.")
	$g_aLangMessages2[113] = _Localization_Load("Messages2", "Tools_Open_RDP_03", "Remote Desktop Connection should be Open.")
	$g_aLangMessages2[114] = _Localization_Load("Messages2", "Tools_Open_IE_Properties_01", "Opening Internet Explorer Properties.")
	$g_aLangMessages2[115] = _Localization_Load("Messages2", "Tools_Open_IE_Properties_02", "Could not open Internet Explorer Properties.")
	$g_aLangMessages2[116] = _Localization_Load("Messages2", "Tools_Open_IE_Properties_03", "You should see Internet Explorer Properties.")
	$g_aLangMessages2[117] = _Localization_Load("Messages2", "Tools_Install_IP6", "Installing the TCP/IP v6 protocol.")
	$g_aLangMessages2[118] = _Localization_Load("Messages2", "Tools_Uninstall_IP6", "Uninstalling the TCP/IP v6 protocol.")
	$g_aLangMessages2[119] = _Localization_Load("Messages2", "Tools_Repair_WorkGroup", "Repairing Workgroup Computers view.")

EndFunc   ;==>_Localization_Messages2


Func _Localization_Preferences()

;~ Check if the language strings is already loaded. Because we do not want to load the language strings twice.
	If StringLen($g_aLangPreferences[0]) > 0 Then
		Return
	EndIf

	$g_aLangPreferences[0] = _Localization_Load("Preferences", "Window_Title", "Preferences")
	$g_aLangPreferences[1] = _Localization_Load("Preferences", "Tab_General", "General")
	$g_aLangPreferences[2] = _Localization_Load("Preferences", "Tab_Cache", "Cache")
	$g_aLangPreferences[3] = _Localization_Load("Preferences", "Tab_Performance", "Performance")
	$g_aLangPreferences[4] = _Localization_Load("Preferences", "Tab_Language", "Language")
	$g_aLangPreferences[5] = _Localization_Load("Preferences", "Group_Redundancy", "Redundancy")
	$g_aLangPreferences[6] = _Localization_Load("Preferences", "Group_Logging", "Logging")
	$g_aLangPreferences[7] = _Localization_Load("Preferences", "Group_Priority", "Priority")
	$g_aLangPreferences[8] = _Localization_Load("Preferences", "Group_Memory", "Memory")
	$g_aLangPreferences[9] = _Localization_Load("Preferences", "Group_Language", "Language")
	$g_aLangPreferences[10] = _Localization_Load("Preferences", "Checkbox_Backup_Folders", "Backup Folders Before Removing")
	$g_aLangPreferences[11] = _Localization_Load("Preferences", "Checkbox_Export_IP", "Export IP Configuration before resetting.")
	$g_aLangPreferences[12] = _Localization_Load("Preferences", "Checkbox_Enable_Logging", " Enable logging")
	$g_aLangPreferences[13] = _Localization_Load("Preferences", "Label_Log_Exceed", "Log size must not exceed :")
	$g_aLangPreferences[14] = _Localization_Load("Preferences", "Label_Logging_Size", "Logging Size: %s KB")
	$g_aLangPreferences[15] = _Localization_Load("Preferences", "Button_Logging_Clear", "Clear Logging")
	$g_aLangPreferences[16] = _Localization_Load("Preferences", "Label_SetPriority", "Set process priority:")
	$g_aLangPreferences[17] = _Localization_Load("Preferences", "Checkbox_SaveRealtime", "Save priority above high (not recommended).")
	$g_aLangPreferences[18] = _Localization_Load("Preferences", "Checkbox_ReduceMemory", "Reduce memory on low memory systems.")
	$g_aLangPreferences[19] = _Localization_Load("Preferences", "Label_Language_Message", "Select the language you prefer and press the %s button to continue. (Restart Required)")
	$g_aLangPreferences[20] = _Localization_Load("Preferences", "Button_Save", "Save")
	$g_aLangPreferences[21] = _Localization_Load("Preferences", "Button_Cancel", "Cancel")
	$g_aLangPreferences[22] = _Localization_Load("Preferences", "Label_Updated", "Preferences Updated")
	$g_aLangPreferences[23] = _Localization_Load("Preferences", "Label_Logging_Cleared", "Logging cleared")
	$g_aLangPreferences[24] = _Localization_Load("Preferences", "MsgBox_Language_Title", "Language Changed")
	$g_aLangPreferences[25] = _Localization_Load("Preferences", "MsgBox_Language_Message", "The selected language has changed. Complete Internet Repair should be restarted for the chosen language to take effect.")
	$g_aLangPreferences[26] = _Localization_Load("Preferences", "MsgBox_Closing_Title", "Closing %{Program.Name}")
	$g_aLangPreferences[27] = _Localization_Load("Preferences", "MsgBox_Closing_Message", "Would you like to close %{Program.Name} now?")

EndFunc   ;==>_Localization_Preferences


Func _Localization_Load($sSection, $sKey, $sDefault)
	Local $sCheckSpace = ""
	If StringCompare(StringLeft($sKey, 8), "Checkbox") = 0 Then
		$sCheckSpace = Chr(32)
	EndIf

	; Generate cache key
	Local $sCacheKey = $sSection & "|" & $sKey

	; Check cache first
	For $i = 0 To $g_iLocalizationCacheSize - 1
		If $g_aLocalizationCache[$i][0] = $sCacheKey Then
			Return $sCheckSpace & $g_aLocalizationCache[$i][2]
		EndIf
	Next

	; Not in cache, load from file
	Local $sText = IniRead($g_sLanguageFile, $sSection, $sKey, $sDefault)
	Local $sReplaced = _Localization_ReplaceVar($sText)

	; Add to cache
	_Localization_AddToCache($sCacheKey, $sText, $sReplaced)

	Return $sCheckSpace & $sReplaced
EndFunc   ;==>_Localization_Load


Func _Localization_AddToCache($sCacheKey, $sOriginal, $sReplaced)
	; Grow cache array if needed
	If $g_iLocalizationCacheSize >= UBound($g_aLocalizationCache) Then
		ReDim $g_aLocalizationCache[$g_iLocalizationCacheSize + 50][3]
	EndIf

	; Add to cache
	$g_aLocalizationCache[$g_iLocalizationCacheSize][0] = $sCacheKey
	$g_aLocalizationCache[$g_iLocalizationCacheSize][1] = $sOriginal
	$g_aLocalizationCache[$g_iLocalizationCacheSize][2] = $sReplaced
	$g_iLocalizationCacheSize += 1
EndFunc   ;==>_Localization_AddToCache


Func _Localization_ReplaceVar($sText)
	$sText = String($sText)

	; Do all replacements in one pass
	$sText = StringReplace($sText, "%{Company.Name}", $g_sCompanyName)
	$sText = StringReplace($sText, "%{Program.Name.Short}", $g_sProgShortName)
	$sText = StringReplace($sText, "%{Program.Name.Short.X64}", $g_sProgShortName_X64)
	$sText = StringReplace($sText, "%{Program.Name}", $g_sProgName)
	$sText = StringReplace($sText, "%{AutoIt.Version}", @AutoItVersion)
	$sText = StringReplace($sText, "%{Windows.Version}", _GetWindowsVersion(1))
	$sText = StringReplace($sText, "\rn", @CRLF)
	$sText = StringReplace($sText, "\t", @TAB)

	Return $sText
EndFunc   ;==>_Localization_ReplaceVar
