all: Example.exe Utils.dll UtilsTest.exe GenGazeToSourceDump.exe

Example.exe:
	gmcs Example.cs -out:build/Example.exe

Utils.dll:
	gmcs utils/*.cs -target:library -out:build/Utils.dll

UtilsTest.exe: Utils.dll
	gmcs UtilsTest.cs -r:build/Utils.dll -out:build/UtilsTest.exe

GenGazeToSourceDump.exe: Utils.dll
	gmcs GenGazeToSourceDump.cs -r:build/Utils.dll -out:build/GenGazeToSourceDump.exe
SimpleGraph.exe: SimpleGraph.cs
	mcs SimpleGraph.cs utils/Config.cs utils/GazeReader.cs utils/SrcMLCodeReader.cs
