require 'fileutils'
require 'nokogiri'

# Change working directory to the Laevo project folder.
Dir.chdir(ARGV[0].to_s())

# Get ABC and FCL project paths from ProjectReferences.txt.
doc = Nokogiri::XML(open("..\\..\\ProjectReferences.txt"))
abc = doc.xpath("//nameSpace:PropertyGroup/nameSpace:ABC-Toolkit", {"nameSpace" => "http://schemas.microsoft.com/developer/msbuild/2003"}).text
fcl = doc.xpath("//nameSpace:PropertyGroup/nameSpace:Framework-Class-Library-Extension", {"nameSpace" => "http://schemas.microsoft.com/developer/msbuild/2003"}).text
timeline = doc.xpath("//nameSpace:PropertyGroup/nameSpace:TimeLine", {"nameSpace" => "http://schemas.microsoft.com/developer/msbuild/2003"}).text

def copy_dll(project_path, dll_name, target_path)
	FileUtils.cp(
		project_path + '\\bin\\Release\\' + dll_name + '.dll',
		target_path + '.dll')
end

# Copy Framework Class Library Extension DLLs.
fcl_library = '..\\..\\Libraries\\Framework Class Library Extension\\'
fcl_dlls = [
	'Whathecode.Interop',
	'Whathecode.Microsoft',
	'Whathecode.PresentationFramework',
	'Whathecode.PresentationFramework.Aspects',
	'Whathecode.System',
	'Whathecode.System.Aspects',
	'Whathecode.System.Management'
	]

fcl_dlls.each do |dll_name|
	copy_dll(fcl + '\\' + dll_name, dll_name, fcl_library + dll_name)
end

# Copy ABC Toolkit DLLs.
abc_toolkit = '..\\..\\Libraries\\ABC Toolkit\\'
abc_dlls = [
	'ABC',
	'ABC.PInvoke'
	]

abc_dlls.each do |dll_name|
	copy_dll(abc + '\\' + dll_name, dll_name, abc_toolkit + dll_name)
end

# Copy DLLs that ABC Toolkit uses.
abc_fcl_dlls = [
	'Whathecode.Interop',
	'Whathecode.System',
	'Microsoft.WindowsAPICodePack',
	'Microsoft.WindowsAPICodePack.Shell'
	]
abc_fcl_dlls.each do |dll_name|
	copy_dll(abc + '\\ABC', dll_name, abc_toolkit + dll_name)
end

#Copy TimeLine DLL.
timeline_library = '..\\..\\Libraries\\TimeLine\\'
timeline_dlls = [
	'Whathecode.TimeLine'
	]

timeline_dlls.each do |dll_name|
	copy_dll(timeline + '\\' + dll_name, dll_name, timeline_library + dll_name)
end

# Copy ABC Plug-in manager into appdata folder.

# Ruby by default gives a path to "C:\Users\UserName\AppData\Roaming\".
# In order to get to "AppData\Local" we have to move one folder back.
laevo_appdata = ENV['APPDATA'] + '\\..\\Local\\Laevo\\PluginManager\\'

# Copy  all contents from the Relaease directory.
plugin_manager =  abc + '\\ABC.PluginManager\\bin\\Release\\.'

FileUtils.cp_r plugin_manager, laevo_appdata