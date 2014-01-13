require 'fileutils'
require 'nokogiri'

# Change working directory to the Laevo solution folder.
Dir.chdir(ARGV[0].to_s())

# Get ABC and FCL project paths from ProjectReferences.txt.
doc = Nokogiri::XML(open("..\\ProjectReferences.txt"))
abc = doc.xpath("//nameSpace:PropertyGroup/nameSpace:ABC-Toolkit", {"nameSpace" => "http://schemas.microsoft.com/developer/msbuild/2003"}).text
fcl = doc.xpath("//nameSpace:PropertyGroup/nameSpace:Framework-Class-Library-Extension", {"nameSpace" => "http://schemas.microsoft.com/developer/msbuild/2003"}).text

# Copy Framework Class Library Extension DLLs.
fcl_library = '..\\Libraries\\Framework Class Library Extension\\'
fcl_dlls = [
	'Whathecode.System',
	'Whathecode.System.Aspects',
	'Whathecode.PresentationFramework',
	'Whathecode.PresentationFramework.Aspects'
	]
fcl_dlls.each do |d|
	FileUtils.cp(
		fcl + '\\' + d + '\\bin\\Release\\' + d + '.dll',
		fcl_library + d + '.dll')
end

# Copy ABC Toolkit DLLs.
abc_toolkit = '..\\Libraries\\ABC Toolkit\\'
abc_dlls = [
	'ABC.Windows',
	'ABC.PInvoke',
	'ABC.Interruptions',
	'ABC.Applications',
	'ABC.Common'
	]
abc_dlls.each do |d|
	FileUtils.cp(
		abc + '\\' + d + '\\bin\\Release\\' + d + '.dll',
		abc_toolkit + d + '.dll')
end