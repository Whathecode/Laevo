require 'xml'
require 'fileutils'

Dir.chdir(ARGV[0].to_s())
parser = XML::Parser.file('ProjectReferences.txt')
doc = parser.parse
namespace = 'msbuild:http://schemas.microsoft.com/developer/msbuild/2003'

# Change working directory to the Laevo project folder, since the paths might be relative paths from there.
Dir.chdir('Laevo')

fcl = doc.find_first('//msbuild:Framework-Class-Library-Extension', namespace).first.to_s()
abc = doc.find_first('//msbuild:ABC-Toolkit', namespace).first.to_s()

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
	'ABC.Applications'
	]
abc_dlls.each do |d|
	FileUtils.cp(
		abc + '\\' + d + '\\bin\\Release\\' + d + '.dll',
		abc_toolkit + d + '.dll')
end