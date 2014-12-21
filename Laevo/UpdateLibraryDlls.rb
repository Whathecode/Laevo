require 'fileutils'
require 'nokogiri'

# Change working directory to the Laevo project folder.
Dir.chdir(ARGV[0].to_s())

# Get ABC and FCL project paths from ProjectReferences.txt.
doc = Nokogiri::XML(open("..\\..\\ProjectReferences.txt"))
abc = doc.xpath("//nameSpace:PropertyGroup/nameSpace:ABC-Toolkit", {"nameSpace" => "http://schemas.microsoft.com/developer/msbuild/2003"}).text
fcl = doc.xpath("//nameSpace:PropertyGroup/nameSpace:Framework-Class-Library-Extension", {"nameSpace" => "http://schemas.microsoft.com/developer/msbuild/2003"}).text

# Copy Framework Class Library Extension DLLs.
fcl_library = '..\\..\\Libraries\\Framework Class Library Extension\\'
fcl_dlls = [
	'Whathecode.Interop',
	'Whathecode.Microsoft',
	'Whathecode.PresentationFramework',
	'Whathecode.PresentationFramework.Aspects',
	'Whathecode.System',
	'Whathecode.System.Aspects'
	]
fcl_dlls.each do |d|
	FileUtils.cp(
		fcl + '\\' + d + '\\bin\\Release\\' + d + '.dll',
		fcl_library + d + '.dll')
end

# Copy ABC Toolkit DLLs.
abc_toolkit = '..\\..\\Libraries\\ABC Toolkit\\'
abc_dlls = [
	'ABC',
	'ABC.Applications',
	'ABC.Interruptions',
	'ABC.PInvoke',
	'ABC.Windows'
	]
abc_dlls.each do |d|
	FileUtils.cp(
		abc + '\\' + d + '\\bin\\Release\\' + d + '.dll',
		abc_toolkit + d + '.dll')
end
abc_fcl_dlls = [
	'Whathecode.Interop',
	'Whathecode.System'
	]
abc_fcl_dlls.each do |d|
	FileUtils.cp(
		fcl + '\\' + d + '\\bin\\Release\\' + d + '.dll',
		abc_toolkit + d + '.dll')
end