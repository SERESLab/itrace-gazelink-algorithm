all: Example.exe Utils.dll UtilsTest.exe

Example.exe:
	gmcs Example.cs -out:build/Example.exe

Utils.dll:
	gmcs utils/*.cs -target:library -out:build/Utils.dll

UtilsTest.exe:
	gmcs UtilsTest.cs -r:build/Utils.dll -out:build/UtilsTest.exe
