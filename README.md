# Project Setup
## Basic Setup
In order to have Laevo up and running and ready for development the following steps are necessary to perform:
 - Install Visual Studio 2013 (or later)
 - Install [NuGet](http://www.nuget.org/) for Visual Studio
 - Laevo uses [PostSharp](http://www.postsharp.net/) which NuGet should normally install for you

## Important Libraries
Laevo realies heavily on two libraries:
 - [__Framework Class Library Extension__ (FCLE)](https://github.com/Whathecode/Framework-Class-Library-Extension): used for general purposes, and to abstract away from Win32 code.
 - [__Activity-Based Computing Toolkit__ (ABC)](https://github.com/StevenHouben/ABC): architecture to create activity-centric systems, including functionality like virtual desktops.

Two solution files are provided:
 - __Laevo.sln__ can be used to only load Laevo and reference precompiled libraries from the "Libraries" folder.
 - __Laevo With Project References.sln__ includes references to project sources from the _"Framework Class Library Extension"_ and _"Activity-Based Computing Toolkit"_ libraries.

When using __Laevo With Project References.sln__ there are two run configurations available, _Debug_ and _Release_. Using _Debug_, project references are used simplifying development and debugging. Using _Release_, just like _Laevo.sln_, precompiled libaries from the "Libraries" folder are references. However, in _Release_, after the build completes the "UpdateLibraryDlls.rb" script is ran (using Ruby), which copies the newly compiled DLLs from FCLE and ABC to the "Libraries" folder, thus updating the precompiled libraries.

In order to use __Laevo With Project References.sln__ some additional setup is required:
 - Download the dependent libraries. You can choose where to place them, but placing them side-by-side with the Laevo project simplifies setup:
   - [Laevo](https://bitbucket.org/Whathecode/laevo)
   - [Framework-Class-Library-Extension](https://github.com/Whathecode/Framework-Class-Library-Extension)
   - [ABC](https://github.com/StevenHouben/ABC)
 - When choosing a custom location for the libraries, specifying the paths to the libraries in:
   - Laevo With Project References.sln
   - ProjectReferences.txt
 - Install [Ruby](https://www.ruby-lang.org/)
   - Install [nokogiri library](http://nokogiri.org/) for Ruby

# Copyright
Copyright (c) 2012 Steven Jeuris: http://whathecode.wordpress.com/
This library is free software; you can redistribute it and/or modify it
under the terms of the GNU GENERAL PUBLIC LICENSE V3 or later, as
published by the Free Software Foundation. Check 
http://www.gnu.org/licenses/gpl.html for details.

This program uses several other open source libraries. Their copyright notices are added in the relevant Libraries folders.

Some icons used are from http://iconza.com/