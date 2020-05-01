/*
	DO NOT EDIT THIS FILE! THIS FILE WAS GENERATED AUTOMATICALLY BY CONAN-UE4CLI VERSION ${VERSION}.
	THIS BOILERPLATE CODE IS INTENDED FOR USE WITH UNREAL ENGINE VERSION 4.19.
*/
using System;
using System.IO;
using UnrealBuildTool;
using System.Diagnostics;

//For Tools.DotNETCommon.JsonObject and Tools.DotNETCommon.FileReference
using Tools.DotNETCommon;

public class ${MODULE} : ModuleRules
{
	//Returns the identifier string for the given target, which includes its platform, architecture (if specified), and debug CRT status
	private string TargetIdentifier(ReadOnlyTargetRules target)
	{
		//Append the target's architecture to its platform name if an architecture was specified
		string id = (target.Architecture != null && target.Architecture.Length > 0) ?
			String.Format("{0}-{1}", target.Platform.ToString(), target.Architecture) :
			target.Platform.ToString();
		
		//Append a debug suffix for Windows debug targets that actually use the debug CRT
		bool isDebug = (target.Configuration == UnrealTargetConfiguration.Debug || target.Configuration == UnrealTargetConfiguration.DebugGame);
		if (isDebug && target.bDebugBuildsActuallyUseDebugCRT) {
			id += "-Debug";
		}
		
		return id;
	}
	
	//Determines if a target's platform is a Windows target platform
	private bool IsWindows(ReadOnlyTargetRules target) {
		return (target.Platform == UnrealTargetPlatform.Win32 || Target.Platform == UnrealTargetPlatform.Win64);
	}
	
	//Returns the version string for the Unreal Engine being used to build this module
	private string GetEngineVersion()
	{
		//The EngineDirectory attribute wasn't added to the ModuleRules class until 4.20, so we just hardcode 4.19 here
		return "4.19";
	}
	
	//Processes the JSON data produced by Conan that describes our dependencies
	private void ProcessDependencies(string depsJson, ReadOnlyTargetRules target)
	{
		//We need to ensure libraries end with ".lib" under Windows
		string libSuffix = ((this.IsWindows(target)) ? ".lib" : "");
		
		//Attempt to parse the JSON file
		JsonObject deps = JsonObject.Read(new FileReference(depsJson));
		
		//Process the list of dependencies
		foreach (JsonObject dep in deps.GetObjectArrayField("dependencies"))
		{
			//Add the header and library paths for the dependency package
			PublicIncludePaths.AddRange(dep.GetStringArrayField("include_paths"));
			PublicLibraryPaths.AddRange(dep.GetStringArrayField("lib_paths"));
			
			//Add the preprocessor definitions from the dependency package
			PublicDefinitions.AddRange(dep.GetStringArrayField("defines"));
			
			//Link against the libraries from the package
			string[] libs = dep.GetStringArrayField("libs");
			foreach (string lib in libs)
			{
				string libFull = lib + ((libSuffix.Length == 0 || lib.EndsWith(libSuffix)) ? "" : libSuffix);
				PublicAdditionalLibraries.Add(libFull);
			}
		}
	}
	
	//Determines if we have precomputed dependency data for the specified target and Engine version, and processes it if we do
	private bool ProcessPrecomputedData(ReadOnlyTargetRules target, string engineVersion)
	{
		//Resolve the paths to the files and directories that will exist if we have precomputed data for the target
		string targetDir = Path.Combine(ModuleDirectory, "precomputed", engineVersion, this.TargetIdentifier(target));
		string flagsFile = Path.Combine(targetDir, "flags.json");
		string includeDir = Path.Combine(targetDir, "include");
		string libDir = Path.Combine(targetDir, "lib");
		
		//If any of the required files or directories do not exist then we do not have precomputed data
		if (!File.Exists(flagsFile) || !Directory.Exists(includeDir) || !Directory.Exists(libDir)) {
			return false;
		}
		
		//Add the precomputed include directory to our search paths
		PublicIncludePaths.Add(includeDir);
		
		//Link against all static library files in the lib directory
		string libExtension = ((this.IsWindows(target)) ? ".lib" : ".a");
		string[] libs = Directory.GetFiles(libDir, "*" + libExtension);
		foreach(string lib in libs) {
			PublicAdditionalLibraries.Add(lib);
		}
		
		//Attempt to parse the JSON file containing any additional flags and system libraries
		JsonObject flags = JsonObject.Read(new FileReference(flagsFile));
		
		//Add any preprocessor definitions specified by the JSON file
		PublicDefinitions.AddRange(flags.GetStringArrayField("defines"));
		
		//Link against any system libraries specified by the JSON file, ensuring we add the file extension under Windows
		string[] systemLibs = flags.GetStringArrayField("system_libs");
		foreach (string lib in systemLibs)
		{
			string libFull = lib + ((this.IsWindows(target)) ? libExtension : "");
			PublicAdditionalLibraries.Add(libFull);
		}
		
		return true;
	}
	
	public ${MODULE}(ReadOnlyTargetRules Target) : base(Target)
	{
		Type = ModuleType.External;
		
		//Determine if we have precomputed dependency data for the target that is being built
		string engineVersion = this.GetEngineVersion();
		if (this.ProcessPrecomputedData(Target, engineVersion) == false)
		{
			//No precomputed data detected, install third-party dependencies using Conan
			Process.Start(new ProcessStartInfo
			{
				FileName = "conan",
				Arguments = "install . --profile=ue" + engineVersion + "-" + this.TargetIdentifier(Target),
				WorkingDirectory = ModuleDirectory,
				UseShellExecute = false
			})
			.WaitForExit();
			
			//Link against our Conan-installed dependencies
			this.ProcessDependencies(Path.Combine(ModuleDirectory, "conanbuildinfo.json"), Target);
		}
	}
}
